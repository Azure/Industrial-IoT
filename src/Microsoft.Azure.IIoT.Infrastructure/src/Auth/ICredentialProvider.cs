// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Auth {
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
    using Microsoft.Rest;
    using System.Threading.Tasks;

    public interface ICredentialProvider {

        /// <summary>
        /// Returns credentials for a particular
        /// environment
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        Task<AzureCredentials> GetAzureCredentialsAsync(
            AzureEnvironment environment);

        /// <summary>
        /// Get token credentials for a resource
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        Task<TokenCredentials> GetTokenCredentialsAsync(
            string resource);
    }
}
