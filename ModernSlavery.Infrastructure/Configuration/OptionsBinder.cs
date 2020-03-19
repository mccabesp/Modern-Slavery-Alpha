﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModernSlavery.Extensions;
using ModernSlavery.SharedKernel;
using ModernSlavery.SharedKernel.Options;

namespace ModernSlavery.Infrastructure.Configuration
{
    public class OptionsBinder
    {
        public readonly IServiceCollection Services;
        public readonly IConfiguration Configuration;

        private Dictionary<Type, object> _bindings = new Dictionary<Type, object>();
        public OptionsBinder(IServiceCollection services, IConfiguration configuration)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private object Bind(Type optionsType, string configSection = null, bool requireAttribute = false)
        {
            if (_bindings.ContainsKey(optionsType)) return _bindings[optionsType];

            var instance = Activator.CreateInstance(optionsType);

            if (string.IsNullOrWhiteSpace(configSection))
            {
                var configSettingAttribute = instance.GetAttribute<OptionsAttribute>();
                configSection = configSettingAttribute?.Key;
            }

            if (string.IsNullOrWhiteSpace(configSection))
            {
                if (requireAttribute) return null;
                Configuration.Bind(instance);
            }
            else
                Configuration.Bind(configSection, instance);

            Services.AddSingleton(instance);

            _bindings[optionsType] = instance;

            return instance;
        }

        //Bind a specific configuration section to a class and register as a singleton dependency
        /// <summary>
        /// Populates a new instance of an IOptions class from settings in a specified configuration section
        /// then registers it as a singleton dependency.
        /// Multiple calls with the same type always returns the first instance registered.
        /// </summary>
        /// <typeparam name="TOptions">The IOptions class to bind the setting to</typeparam>
        /// <param name="configSection">The name of the IConfiguration section where the settings are stored or empty to load the root configuration settings</param>
        /// <returns>An instance of the populated IOptions concrete class</returns>
        /// <returns></returns>
        public TOptions Bind<TOptions>(string configSection=null) where TOptions : class, IOptions
        {
            return (TOptions)Bind(typeof(TOptions), configSection);
        }

        /// <summary>
        /// Populates a new instance of an IOptions class from settings in a specified configuration section
        /// then registers it as a singleton dependency.
        /// Multiple calls with the same type always returns the first instance registered.
        /// </summary>
        /// <typeparam name="TOptions">The IOptions class to bind the setting to</typeparam>
        /// <param name="configSection">The IConfiguration section where the settings are stored</param>
        /// <returns>An instance of the populated IOptions concrete class</returns>
        public TOptions Bind<TOptions>(IConfiguration configSection) where TOptions : class, IOptions
        {
            var optionsType=typeof(TOptions);
            if (_bindings.ContainsKey(optionsType)) return (TOptions)_bindings[optionsType];

            var instance = Activator.CreateInstance<TOptions>();

            configSection.Bind(instance);

            Services.AddSingleton(instance);

            _bindings[optionsType] = instance;

            return instance;
        }

        /// <summary>
        /// Return a previously bound instance of an IOptions class 
        /// </summary>
        /// <typeparam name="TOptions">The IOptions class to bind the setting to</typeparam>
        /// <returns>An instance of the populated IOptions concrete class or null</returns>
        public TOptions Get<TOptions>() where TOptions : class, IOptions
        {
            var optionsType = typeof(TOptions);
            return _bindings.ContainsKey(optionsType)? (TOptions)_bindings[optionsType] : default;
        }

        /// <summary>
        /// Bind all IOptions classes in all assemblies of the current appdomain whos name assembly name starts with the specified prefix
        /// Only classes with ConfigSettingAttribute.Key will be bound.
        /// </summary>
        /// <param name="assemblyPrefix"></param>
        public void BindAssemblies(string assemblyPrefix)
        {
            if (string.IsNullOrWhiteSpace(assemblyPrefix)) throw new ArgumentNullException(nameof(assemblyPrefix));

            var type = typeof(IOptions);

            var optionsTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith(assemblyPrefix, true, default))
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p));

            foreach (var optionsType in optionsTypes)
                Bind(optionsType, requireAttribute: true);
        }

    }
}