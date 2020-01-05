// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Authentication {

    using System;
    using System.Runtime.InteropServices;

    using Microsoft.Azure.Management.ResourceManager.Fluent;

    class AuthenticationManagerFactory {

        public static IAuthenticationManager GetAuthenticationManager(
            AzureEnvironment azureEnvironment,
            Guid tenantId,
            Guid applicationClientId,
            string clientSecret = null
        ) {
            if (null != clientSecret) {
                return new ClientCredentialsAuthenticationManager(
                    azureEnvironment,
                    tenantId,
                    applicationClientId,
                    clientSecret
                );
            }
            else {
                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                if (isWindows) {
                    return new InteractiveAuthenticationManager(
                        azureEnvironment,
                        tenantId,
                        applicationClientId
                    );
                }
                else {
                    return new CodeFlowAuthenticationManager(
                        azureEnvironment,
                        tenantId,
                        applicationClientId
                    );

                }
            }
        }
    }
}
