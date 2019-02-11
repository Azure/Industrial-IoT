// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.Auth.Clients;
using Microsoft.Azure.IIoT.Diagnostics;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test
{
    public class ClientConfig : IClientConfig
    {
        /// <summary>
        /// The AAD application id for the client.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// AAD Client / Application secret (optional)
        /// </summary>
        public string AppSecret { get; set; }

        /// <summary>
        /// Tenant id if any (optional)
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Instance or authority (optional)
        /// </summary>
        public string InstanceUrl { get; set; }

        /// <summary>
        /// Audience to talk to.
        /// </summary>
        public string Audience { get; set; }
    }

    public class LogConfig : ILogConfig
    {
        public LogLevel LogLevel => LogLevel.Debug;

        public string ProcessId => "Vault.Test";
    }

}
