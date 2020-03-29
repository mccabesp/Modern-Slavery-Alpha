﻿using System;
using System.Net.Http;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using ModernSlavery.Core.Interfaces;
using ModernSlavery.Core.SharedKernel.Attributes;
using ModernSlavery.Core.SharedKernel.Interfaces;

namespace ModernSlavery.Infrastructure.CompaniesHouse
{
    [AutoRegister]
    public class DependencyModule : IDependencyModule
    {
        private readonly CompaniesHouseOptions _options;

        public DependencyModule(CompaniesHouseOptions options)
        {
            _options = options;
        }

        public void Register(IDependencyBuilder builder)
        {
            //Add a dedicated httpclient for Companies house API with exponential retry policy
            builder.Services.AddHttpClient<ICompaniesHouseAPI, CompaniesHouseAPI>(nameof(ICompaniesHouseAPI),
                    httpClient =>
                    {
                        CompaniesHouseAPI.SetupHttpClient(httpClient, _options.ApiServer, _options.ApiKey);
                    })
                .SetHandlerLifetime(TimeSpan.FromMinutes(10))
                .AddPolicyHandler(CompaniesHouseAPI.GetRetryPolicy());

            builder.Autofac.RegisterType<CompaniesHouseAPI>()
                .As<ICompaniesHouseAPI>()
                .SingleInstance()
                .WithParameter(
                    (p, ctx) => p.ParameterType == typeof(HttpClient),
                    (p, ctx) => ctx.Resolve<IHttpClientFactory>().CreateClient(nameof(ICompaniesHouseAPI)));
        }

        public void Configure(IServiceProvider serviceProvider, IContainer container)
        {
            //TODO: Add configuration here
        }
    }
}