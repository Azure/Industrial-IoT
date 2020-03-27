﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection {
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Extensions.Options;
    using Microsoft.AspNetCore.SignalR;
    using System.Collections.Generic;
    using System;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// SignalR setup extensions
    /// </summary>
    public static class SignalRBuilderEx {

        /// <summary>
        /// Add json serializer
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static T AddJsonSerializer<T>(this T builder) where T : ISignalRBuilder {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder = builder.AddNewtonsoftJsonProtocol();

            // Configure json serializer settings transiently to pick up all converters
            builder.Services.AddTransient<IConfigureOptions<NewtonsoftJsonHubProtocolOptions>>(services =>
                new ConfigureNamedOptions<NewtonsoftJsonHubProtocolOptions>(Options.DefaultName, options => {
                    var provider = services.GetService<IJsonSerializerSettingsProvider>();
                    var settings = provider?.Settings;
                    if (settings == null) {
                        return;
                    }

                    options.PayloadSerializerSettings.Formatting = settings.Formatting;
                    options.PayloadSerializerSettings.NullValueHandling = settings.NullValueHandling;
                    options.PayloadSerializerSettings.DefaultValueHandling = settings.DefaultValueHandling;
                    options.PayloadSerializerSettings.ContractResolver = settings.ContractResolver;
                    options.PayloadSerializerSettings.DateFormatHandling = settings.DateFormatHandling;
                    options.PayloadSerializerSettings.MaxDepth = settings.MaxDepth;
                    options.PayloadSerializerSettings.Context = settings.Context;

                    var set = new HashSet<JsonConverter>(options.PayloadSerializerSettings.Converters);
                    if (!set.IsProperSupersetOf(settings.Converters)) {
                        options.PayloadSerializerSettings.Converters =
                            set.MergeWith(settings.Converters).ToList();
                    }
                }));
            return builder;
        }

        /// <summary>
        /// Add json serializer
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static T AddMessagePackSerializer<T>(this T builder) where T : ISignalRBuilder {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder = builder.AddMessagePackProtocol();

            // Configure json serializer settings transiently to pick up all converters
            builder.Services.AddTransient<IConfigureOptions<MessagePackHubProtocolOptions>>(services =>
                new ConfigureNamedOptions<MessagePackHubProtocolOptions>(Options.DefaultName, options => {
                    var provider = services.GetService<IMessagePackSerializerOptionsProvider>();
                    var resolvers = provider?.Resolvers;
                    if (resolvers != null) {
                        options.FormatterResolvers = resolvers.ToList();
                    }
                }));
            return builder;
        }
    }
}
