// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.KeyVault.Clients {
    using Microsoft.Azure.IIoT.Auth.Clients.Default;
    using Autofac;

    /// <summary>
    /// Keyvault client and authentication support
    /// </summary>
    public class KeyVaultClientModule : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<KeyVaultKeyHandleSerializer>()
                .AsImplementedInterfaces();
            builder.RegisterType<KeyVaultServiceClient>()
                .AsImplementedInterfaces();
            builder.RegisterModule<KeyVaultAuthentication>();
            base.Load(builder);
        }
    }
}
