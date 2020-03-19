﻿using ModernSlavery.SharedKernel.Options;

namespace ModernSlavery.WebUI.Shared.Options
{
    [Options("DistributedCache")]
    public class DistributedCacheOptions:IOptions
    {
        public string Type { get; set; }
        public string AzureConnectionString { get; set; }

    }
}