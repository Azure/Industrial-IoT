// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Management.Auth {
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

    public interface ICredentialProvider {

        /// <summary>
        /// Returns credentials to use for simulation
        /// </summary>
        /// <returns></returns>
        AzureCredentials Credentials { get; }
    }
}