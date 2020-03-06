﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModernSlavery.Core;
using ModernSlavery.Extensions;
using ModernSlavery.Extensions.AspNetCore;
using ModernSlavery.WebUI.Areas.Account.Abstractions;
using ModernSlavery.WebUI.Areas.Account.ViewModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using ModernSlavery.Core.Interfaces;
using ModernSlavery.WebUI.Shared.Classes;
using ModernSlavery.Entities;

namespace ModernSlavery.WebUI.Areas.Account.ViewServices
{

    public class CloseAccountViewService : ICloseAccountViewService
    {

        public CloseAccountViewService(IUserRepository userRepository,
            IRegistrationRepository registrationRepository,
            ILogger<CloseAccountViewService> logger,
            ISendEmailService sendEmailService)
        {
            UserRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            RegistrationRepository = registrationRepository ?? throw new ArgumentNullException(nameof(registrationRepository));
            Logger = logger;
            SendEmailService = sendEmailService;
        }


        private IUserRepository UserRepository { get; }
        private IRegistrationRepository RegistrationRepository { get; }
        private ILogger<CloseAccountViewService> Logger { get; }
        private ISendEmailService SendEmailService { get; }

        public async Task<ModelStateDictionary> CloseAccountAsync(User userToRetire, string currentPassword, User actionByUser)
        {
            var errorState = new ModelStateDictionary();

            // ensure the user has entered their password
            bool checkPasswordResult = await UserRepository.CheckPasswordAsync(userToRetire, currentPassword);
            if (checkPasswordResult == false)
            {
                errorState.AddModelError(nameof(ChangePasswordViewModel.CurrentPassword), "Could not verify your current password");
                return errorState;
            }

            //Save the list of registered organisations
            List<Organisation> userOrgs = userToRetire.UserOrganisations.Select(uo => uo.Organisation).Distinct().ToList();

            // aggregated save
            await UserRepository.BeginTransactionAsync(
                async () => {
                    try
                    {
                        // update retired user registrations 
                        await RegistrationRepository.RemoveRetiredUserRegistrationsAsync(userToRetire, actionByUser);

                        // retire user
                        await UserRepository.RetireUserAsync(userToRetire);

                        // commit
                        UserRepository.CommitTransaction();
                    }
                    catch (Exception ex)
                    {
                        UserRepository.RollbackTransaction();
                        Logger.LogWarning(
                            ex,
                            "Failed to retire user {UserId}. Action by user {ActionByUserId}",
                            userToRetire.UserId,
                            actionByUser.UserId);
                        throw;
                    }
                });

            if (!userToRetire.EmailAddress.StartsWithI(Global.TestPrefix))
            {
                // Create the close account notification to user
                var sendEmails = new List<Task>();
                bool testEmail = !Config.IsProduction();
                sendEmails.Add(SendEmailService.SendAccountClosedNotificationAsync(userToRetire.EmailAddress, testEmail));

                //Create the notification to GEO for each newly orphaned organisation
                userOrgs.Where(org => org.GetIsOrphan())
                    .ForEach(org => sendEmails.Add(SendEmailService.SendGEOOrphanOrganisationNotificationAsync(org.OrganisationName, testEmail)));

                //Send all the notifications in parallel
                await Task.WhenAll(sendEmails);
            }

            return errorState;
        }

    }

}
