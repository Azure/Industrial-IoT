// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Default {
    using Autofac;

    /// <summary>
    /// Injected http client framework module
    /// </summary>
    public sealed class HttpClientModule : Module {

        /// <summary>
        /// Load the module
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder) {

            // Http client services ...
            builder.RegisterType<HttpClient>()
                .AsImplementedInterfaces()
                .IfNotRegistered(typeof(IHttpClient));
            builder.RegisterType<HttpHandlerFactory>()
                .AsImplementedInterfaces()
                .IfNotRegistered(typeof(IHttpHandlerFactory));
            builder.RegisterType<HttpClientFactory>().SingleInstance()
                .AsImplementedInterfaces()
                .IfNotRegistered(typeof(System.Net.Http.IHttpClientFactory));

            base.Load(builder);
        }
    }
}
