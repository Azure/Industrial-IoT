// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Runtime
{
    using Microsoft.Extensions.Configuration;
    using global::Azure.IIoT.OpcUa.Services.Sdk.Runtime;

    /// <summary>
    /// Configuration aggregation
    /// </summary>
    public class Config : ApiConfig
    {
        /// <summary>Url</summary>
        public string TsiDataAccessFQDN =>
            GetStringOrDefault(PcsVariable.PCS_TSI_URL)?.Trim();

        /// <summary>TenantId</summary>
        public string TenantId =>
            GetStringOrDefault(PcsVariable.PCS_AUTH_TENANT)?.Trim();

        /// <summary>WorkbookId</summary>
        public string WorkbookId =>
            GetStringOrDefault(PcsVariable.PCS_WORKBOOK_ID)?.Trim();

        /// <summary>SubscriptionId</summary>
        public string SubscriptionId =>
            GetStringOrDefault(PcsVariable.PCS_SUBSCRIPTION_ID)?.Trim();

        /// <summary>ResourceGroup Name</summary>
        public string ResourceGroup =>
            GetStringOrDefault(PcsVariable.PCS_RESOURCE_GROUP)?.Trim();

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration)
        {
        }
    }
}
