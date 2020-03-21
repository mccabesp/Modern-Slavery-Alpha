﻿using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using ModernSlavery.Core.Interfaces;
using ModernSlavery.Database.Classes;
using ModernSlavery.SharedKernel.Interfaces;

namespace ModernSlavery.Database
{
    public class DatabaseDependencyModule : IDependencyModule
    {
        public void Bind(ContainerBuilder builder, IServiceCollection services)
        {
            builder.RegisterType<DatabaseContext>().As<IDbContext>().InstancePerLifetimeScope();

            builder.RegisterType<SqlRepository>().As<IDataRepository>().InstancePerLifetimeScope();
        }
    }
}
