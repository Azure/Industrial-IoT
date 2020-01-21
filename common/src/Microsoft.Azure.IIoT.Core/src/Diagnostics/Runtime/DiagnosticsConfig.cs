// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Metric logger configuration
    /// </summary>
    public class DiagnosticsConfig : ConfigBase, IDiagnosticsConfig {

        private const string kInstrumentationKey = PcsVariable.PCS_APPINSIGHTS_INSTRUMENTATIONKEY;

        /// <inheritdoc/>
        public string InstrumentationKey => GetStringOrDefault(kInstrumentationKey);

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public DiagnosticsConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
