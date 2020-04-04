// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection {
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.AspNetCore.Serializers;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using System.Collections.Generic;
    using System;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// Mvc setup extensions
    /// </summary>
    public static class MvcBuilderEx {

        /// <summary>
        /// Add MessagePack serializer
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMvcBuilder AddSerializers(this IMvcBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            // Add newtonsoft json serializer to the front (default)
            builder = builder.AddJsonSerializer();

            // Add all other serializers
            builder.Services.AddTransient<IConfigureOptions<MvcOptions>>(services =>
                new ConfigureNamedOptions<MvcOptions>(Options.DefaultName, option => {
                    var serializers = services.GetService<IEnumerable<ISerializer>>();
                    if (serializers == null) {
                        return;
                    }
                    option.OutputFormatters.RemoveType<SerializerOutputFormatter>();
                    option.InputFormatters.RemoveType<SerializerInputFormatter>();
                    foreach (var serializer in serializers) {
                        if (serializer is NewtonSoftJsonSerializer) {
                            continue;  // skip
                        }
                        option.OutputFormatters.Add(new SerializerOutputFormatter(serializer));
                        option.InputFormatters.Add(new SerializerInputFormatter(serializer));
                    }
                }));
            return builder;
        }


        /// <summary>
        /// Add json serializer
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        internal static IMvcBuilder AddJsonSerializer(this IMvcBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder = builder.AddNewtonsoftJson();

            // Configure json serializer settings transiently to pick up all converters
            builder.Services.AddTransient<IConfigureOptions<MvcNewtonsoftJsonOptions>>(services =>
                new ConfigureNamedOptions<MvcNewtonsoftJsonOptions>(Options.DefaultName, options => {
                    var provider = services.GetService<IJsonSerializerSettingsProvider>();
                    var settings = provider?.Settings;
                    if (settings == null) {
                        return;
                    }

                    options.SerializerSettings.Formatting = settings.Formatting;
                    options.SerializerSettings.NullValueHandling = settings.NullValueHandling;
                    options.SerializerSettings.DefaultValueHandling = settings.DefaultValueHandling;
                    options.SerializerSettings.ContractResolver = settings.ContractResolver;
                    options.SerializerSettings.DateFormatHandling = settings.DateFormatHandling;
                    options.SerializerSettings.MaxDepth = settings.MaxDepth;
                    options.SerializerSettings.Context = settings.Context;

                    var set = new HashSet<JsonConverter>(options.SerializerSettings.Converters);
                    if (!set.IsProperSupersetOf(settings.Converters)) {
                        options.SerializerSettings.Converters =
                            set.MergeWith(settings.Converters).ToList();
                    }
                }));
            return builder;
        }
    }
}
