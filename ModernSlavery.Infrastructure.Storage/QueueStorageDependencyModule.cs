﻿using System;
using System.Linq;
using Autofac;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using ModernSlavery.Core.Interfaces;
using ModernSlavery.Infrastructure.Storage.Classes;
using ModernSlavery.SharedKernel;
using ModernSlavery.SharedKernel.Interfaces;
using ModernSlavery.SharedKernel.Options;

namespace ModernSlavery.Infrastructure.Storage
{
    public class QueueStorageDependencyModule: IDependencyModule
    {
        private readonly StorageOptions _options;
        public QueueStorageDependencyModule(StorageOptions options)
        {
            _options = options;
        }

        public void Bind(ContainerBuilder builder)
        {
            // Register queues
            builder.RegisterAzureQueue(_options.AzureConnectionString, QueueNames.SendEmail);
            builder.RegisterAzureQueue(_options.AzureConnectionString, QueueNames.SendNotifyEmail);
            builder.RegisterAzureQueue(_options.AzureConnectionString, QueueNames.ExecuteWebJob);
        }
    }
}