// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Managed service identity configuration
    /// </summary>
    public class MsiClientConfig : ConfigBase, IOAuthClientConfig {

        /// <summary>
        /// Client configuration
        /// </summary>
        private const string kAuth_AppIdKey = "Msi:AppId";
        private const string kAuth_TenantIdKey = "Msi:TenantId";

        /// <summary>Scheme</summary>
        public string Scheme => AuthScheme.Msi;
        /// <summary>Application id</summary>
        public string AppId => GetStringOrDefault(kAuth_AppIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_MSI_APPID,
                () => null))?.Trim();
        /// <summary>Optional tenant</summary>
        public string TenantId => GetStringOrDefault(kAuth_TenantIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_MSI_TENANT))?.Trim();
        /// <summary>Authority url</summary>
        public string InstanceUrl => null;
        /// <summary>App secret</summary>
        public string AppSecret => null;
        /// <summary>Audience</summary>
        public string Audience => null;
        /// <summary>Resource</summary>
        public string Resource => Http.Resource.KeyVault;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public MsiClientConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
