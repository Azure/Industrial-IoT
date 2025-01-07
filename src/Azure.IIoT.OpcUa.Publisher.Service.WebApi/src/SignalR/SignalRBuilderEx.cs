// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection
{
    using Microsoft.Extensions.Options;
    using Microsoft.AspNetCore.SignalR;
    using Furly.Extensions.Serializers;
    using MessagePack.Resolvers;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// SignalR setup extensions
    /// </summary>
    public static class SignalRBuilderEx
    {
        /// <summary>
        /// Add json serializer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/>
        /// is <c>null</c>.</exception>
        public static T AddNewtonsoftJson<T>(this T builder) where T : ISignalRBuilder
        {
            builder = builder.AddNewtonsoftJsonProtocol();

            // Configure json serializer settings transiently to pick up all converters
            builder.Services.AddTransient<IConfigureOptions<NewtonsoftJsonHubProtocolOptions>>(services =>
                new ConfigureNamedOptions<NewtonsoftJsonHubProtocolOptions>(Options.DefaultName, options =>
                {
                    var provider = services.GetService<INewtonsoftSerializerSettingsProvider>();
                    var settings = provider?.Settings;
                    if (settings == null)
                    {
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
                    if (!set.IsProperSupersetOf(settings.Converters))
                    {
                        options.PayloadSerializerSettings.Converters =
                            [.. set.MergeWith(settings.Converters)];
                    }
                }));
            return builder;
        }

        /// <summary>
        /// Add message pack serializer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/>
        /// is <c>null</c>.</exception>
        public static T AddMessagePack<T>(this T builder) where T : ISignalRBuilder
        {
            builder = builder.AddMessagePackProtocol();

            // Configure json serializer settings transiently to pick up all converters
            builder.Services.AddTransient<IConfigureOptions<MessagePackHubProtocolOptions>>(services =>
                new ConfigureNamedOptions<MessagePackHubProtocolOptions>(Options.DefaultName, options =>
                {
                    var provider = services.GetService<IMessagePackSerializerOptionsProvider>();
                    var resolvers = provider?.Resolvers;
                    if (resolvers != null)
                    {
                        options.SerializerOptions = options.SerializerOptions.WithResolver(
                            CompositeResolver.Create(resolvers.ToArray()));
                    }
                }));
            return builder;
        }
    }
}
