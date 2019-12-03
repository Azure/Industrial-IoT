// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.SignalR {
    using Microsoft.Azure.IIoT.Http.SignalR.Services;
    using Autofac;

    /// <summary>
    /// Subscriber for signalr service
    /// </summary>
    public sealed class SignalRClientModule : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<SignalRClientHost>()
                .AutoActivate()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            base.Load(builder);
        }
    }
}