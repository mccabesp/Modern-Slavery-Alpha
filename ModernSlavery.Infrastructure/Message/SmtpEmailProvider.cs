﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModernSlavery.Core.Interfaces;
using ModernSlavery.Core.Models;
using ModernSlavery.Extensions;
using ModernSlavery.SharedKernel;
using ModernSlavery.SharedKernel.Options;

namespace ModernSlavery.Infrastructure.Message
{

    public class SmtpEmailProvider : BaseEmailProvider
    {

        public SmtpEmailProvider(IEmailTemplateRepository emailTemplateRepo,
            SmtpEmailOptions smtpEmailOptions,
            GlobalOptions globalOptions,
            ILogger<SmtpEmailProvider> logger,
            [KeyFilter(Filenames.EmailSendLog)]ILogRecordLogger emailSendLog)
            : base(globalOptions,emailTemplateRepo, logger, emailSendLog)
        {
            Options = smtpEmailOptions ?? throw new ArgumentNullException(nameof(smtpEmailOptions));
            //TODO ensure smtp config is present (when enabled)
        }

        public SmtpEmailOptions Options { get; }

        public override bool Enabled => Options.Enabled != false;

        public override async Task<SendEmailResult> SendEmailAsync<TModel>(string emailAddress, string templateId, TModel model, bool test)
        {
            // convert the model's public properties to a dictionary
            Dictionary<string, object> mergeParameters = model.GetPropertiesDictionary();

            // prefix subject with environment name
            mergeParameters["Environment"] = GlobalOptions.IsProduction() ? "" : $"[{GlobalOptions.Environment}] ";

            // get template
            EmailTemplateInfo emailTemplateInfo = EmailTemplateRepo.GetByTemplateId(templateId);
            string htmlContent = System.IO.File.ReadAllText(emailTemplateInfo.FilePath);

            // parse html
            var parser = new HtmlParser();
            IHtmlDocument document = parser.ParseDocument(htmlContent);

            // remove the meta data comments from the document
            IComment templateMetaData = document.Descendents<IComment>().FirstOrDefault();
            if (templateMetaData == null)
            {
                new NullReferenceException(nameof(templateMetaData));
            }

            templateMetaData.Remove();

            string messageSubject = emailTemplateInfo.EmailSubject;
            string messageHtml = document.ToHtml();
            string messageText = document.Text();

            // merge the template parameters
            foreach ((string name, object value) in mergeParameters)
            {
                messageSubject = messageSubject.Replace($"(({name}))", value.ToString());
                messageHtml = messageHtml.Replace($"(({name}))", value.ToString());
            }

            string smtpServer = string.IsNullOrWhiteSpace(Options.Server) ? Options.Server2 : Options.Server;
            int smtpServerPort = Options.Port<=0 ? Options.Port2: Options.Port;
            string smtpUsername = string.IsNullOrWhiteSpace(Options.Username) ? Options.Username2 : Options.Username;
            string smtpPassword = string.IsNullOrWhiteSpace(Options.Password) ? Options.Password2 : Options.Password;

            await Email.QuickSendAsync(
                messageSubject,
                Options.SenderEmail,
                Options.SenderName,
                Options.ReplyEmail,
                emailAddress,
                messageHtml,
                smtpServer,
                smtpUsername,
                smtpPassword,
                smtpServerPort,
                test: test);

            return new SendEmailResult {
                Status = "sent",
                Server = $"{smtpServer}:{smtpServerPort}",
                ServerUsername = smtpUsername,
                EmailAddress = emailAddress,
                EmailSubject = messageSubject,
                EmailMessagePlainText = messageText
            };
        }

    }

}
