// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Authentication {

    using System.Runtime.InteropServices;
    using Microsoft.Azure.IIoT.Deployment.Deployment;

    /// <summary>
    /// Factory class for instantiating corresponding IAuthenticationManager class.
    /// </summary>
    class AuthenticationManagerFactory {

        /// <summary>
        /// Get instance of IAuthenticationManager based on provided authentication configuration.
        /// </summary>
        /// <param name="authConf"> authentication configuration </param>
        /// <returns></returns>
        public static IAuthenticationManager GetAuthenticationManager(
            AuthenticationConfiguration authConf
        ) {
            if (null != authConf.ClientSecret) {
                return new ClientCredentialsAuthenticationManager(
                    authConf.AzureEnvironment,
                    authConf.TenantId,
                    authConf.ClientId,
                    authConf.ClientSecret
                );
            }
            else {
                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                if (isWindows) {
                    return new InteractiveAuthenticationManager(
                        authConf.AzureEnvironment,
                        authConf.TenantId,
                        authConf.ClientId
                    );
                }
                else {
                    return new CodeFlowAuthenticationManager(
                        authConf.AzureEnvironment,
                        authConf.TenantId,
                        authConf.ClientId
                    );

                }
            }
        }
    }
}
