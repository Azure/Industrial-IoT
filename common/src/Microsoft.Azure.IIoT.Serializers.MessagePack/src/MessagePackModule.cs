// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers {
    using Microsoft.Azure.IIoT.Serializers.MessagePack;
    using Autofac;

    /// <summary>
    /// All pluggable serializers
    /// </summary>
    public class MessagePackModule : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<MessagePackSerializer>()
                .AsImplementedInterfaces();
            builder.RegisterType<MessagePackResolvers>()
                .AsImplementedInterfaces();

            base.Load(builder);
        }
    }
}