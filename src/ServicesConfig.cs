// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.Utils;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Runtime
{
    public interface IServicesConfig
    {
        string KeyVaultApiUrl { get; set; }
        int KeyVaultApiTimeout { get; set; }
        string CosmosDBEndpoint { get; set; }
        string CosmosDBToken { get; set; }
    }

    public class ServicesConfig : IServicesConfig
    {
        public string KeyVaultApiUrl { get; set; }
        public int KeyVaultApiTimeout { get; set; }
        public string CosmosDBEndpoint { get; set; }
        public string CosmosDBToken { get; set; }

    }
}
