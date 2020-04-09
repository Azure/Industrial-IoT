// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection {
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Azure.SignalR;

    /// <summary>
    /// SignalR setup extensions
    /// </summary>
    public static class SignalRBuilderEx {

        /// <summary>
        /// Add azure signalr if possible
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ISignalRServerBuilder AddAzureSignalRService(this ISignalRServerBuilder builder,
            ISignalRServiceConfig config = null) {
            if (config == null) {
                config = builder.Services.BuildServiceProvider().GetService<ISignalRServiceConfig>();
            }
            if (string.IsNullOrEmpty(config?.SignalRConnString)) {
                // not using signalr service
                return builder;
            }
            builder.AddAzureSignalR().Services.Configure<ServiceOptions>(options => {
                options.ConnectionString = config.SignalRConnString;
            });
            return builder;
        }
    }
}
