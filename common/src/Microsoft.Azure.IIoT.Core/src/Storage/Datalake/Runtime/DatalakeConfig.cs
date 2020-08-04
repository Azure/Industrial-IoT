// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Datalake.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Datalake file storage configuration
    /// </summary>
    public class DatalakeConfig : ConfigBase, IDatalakeConfig  {

        /// <summary>
        /// Configuration keys
        /// </summary>
        private const string kDatalakeAccountNameKey = "Datalake:AccountName";
        private const string kDatalakeEndpointSuffixKey = "Datalake:EndpointSuffix";
        private const string kDatalakeAccountKeyKey = "Datalake:AccountKey";

        /// <summary> Name </summary>
        public string AccountName =>
            GetStringOrDefault(kDatalakeAccountNameKey,
            () => GetStringOrDefault(PcsVariable.PCS_ADLSG2_ACCOUNT,
                () => null));
        /// <summary> Suffix </summary>
        public string EndpointSuffix =>
            GetStringOrDefault(kDatalakeEndpointSuffixKey,
            () => GetStringOrDefault(PcsVariable.PCS_ADLSG2_ENDPOINTSUFFIX,
                () => "dfs.core.windows.net"));
        /// <summary> Key </summary>
        public string AccountKey =>
            GetStringOrDefault(kDatalakeAccountKeyKey,
            () => GetStringOrDefault(PcsVariable.PCS_ADLSG2_ACCOUNT_KEY,
                () => null));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public DatalakeConfig(IConfiguration configuration) :
            base(configuration) {
        }

    }
}
