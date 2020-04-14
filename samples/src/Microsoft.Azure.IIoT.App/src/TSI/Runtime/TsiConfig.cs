// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.TSI.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// TSI configuration
    /// </summary>
    public class TsiConfig : ConfigBase, ITsiConfig {

        private const string kTsi_DataAccessFQDN = "Tsi:DataAccessFQDN";

        /// <summary>DataAccessFQDN</summary>
        public string DataAccessFQDN => GetStringOrDefault(kTsi_DataAccessFQDN,
            () => GetStringOrDefault(PcsVariable.PCS_TSI_URL))?.Trim();

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public TsiConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
