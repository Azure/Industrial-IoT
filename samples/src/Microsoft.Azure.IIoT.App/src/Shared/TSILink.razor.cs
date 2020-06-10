// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Shared {
    using System;

    public partial class TSILink {
        private string TsiLink { get; set; } = null;

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized() {
            CreateTSILink();
        }

        /// <summary>
        /// Create TSI link
        /// </summary>
        private void CreateTSILink() {
            if (!string.IsNullOrEmpty(Configuration.TsiDataAccessFQDN)) {
                var index = Configuration.TsiDataAccessFQDN.IndexOf('.');
                if (index > 0) {
                    TsiLink = "https://insights.timeseries.azure.com/preview?environmentId=" + Configuration.TsiDataAccessFQDN.Substring(0, index) + "&tid=" + Configuration.TenantId;
                }
            }
        }
    }
}