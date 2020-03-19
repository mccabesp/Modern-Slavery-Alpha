﻿using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using ModernSlavery.Core.Interfaces;
using ModernSlavery.WebUI.Shared.Models;
using ModernSlavery.WebUI.Shared.Options;

namespace ModernSlavery.WebUI.Shared.Interfaces
{
    public interface IWebService
    {
        IErrorViewModelFactory ErrorViewModelFactory { get; }
        FeatureSwitchOptions FeatureSwitchOptions { get; }

        IMapper AutoMapper { get; }
        IHttpCache Cache { get; }
        IHttpSession Session { get; }
        IWebHostEnvironment HostingEnvironment { get; }
        IShortCodesRepository ShortCodesRepository { get; }
        IWebTracker WebTracker { get; }
    }
}
