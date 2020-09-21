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
        private const string kDatalakeAccountConnectionStringKey = "Datalake:ConnectionString";

        /// <summary> Name </summary>
        public string AccountName =>
            GetConnectonStringTokenOrDefault(kDatalakeAccountConnectionStringKey,
                cs => cs.Endpoint,
            () => GetConnectonStringTokenOrDefault(PcsVariable.PCS_ADLSG2_CONNSTRING,
                cs => cs.Endpoint,
            () => GetStringOrDefault(kDatalakeAccountNameKey,
            () => GetStringOrDefault(PcsVariable.PCS_ADLSG2_ACCOUNT,
            () => null))));
        /// <summary> Suffix </summary>
        public string EndpointSuffix =>
            GetConnectonStringTokenOrDefault(kDatalakeAccountConnectionStringKey,
                cs => cs.EndpointSuffix,
            () => GetConnectonStringTokenOrDefault(PcsVariable.PCS_ADLSG2_CONNSTRING,
                cs => cs.EndpointSuffix,
            () => GetStringOrDefault(kDatalakeEndpointSuffixKey,
            () => GetStringOrDefault(PcsVariable.PCS_ADLSG2_ENDPOINTSUFFIX,
            () => "dfs.core.windows.net"))));
        /// <summary> Key </summary>
        public string AccountKey =>
            GetConnectonStringTokenOrDefault(kDatalakeAccountConnectionStringKey,
                cs => cs.SharedAccessKey,
            () => GetConnectonStringTokenOrDefault(PcsVariable.PCS_ADLSG2_CONNSTRING,
                cs => cs.SharedAccessKey,
            () => GetStringOrDefault(kDatalakeAccountKeyKey,
            () => GetStringOrDefault(PcsVariable.PCS_ADLSG2_ACCOUNT_KEY,
            () => null))));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public DatalakeConfig(IConfiguration configuration) :
            base(configuration) {
        }

    }
}
