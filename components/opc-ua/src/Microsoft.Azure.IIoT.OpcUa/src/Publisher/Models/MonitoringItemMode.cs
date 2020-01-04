// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Monitoring modes
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MonitoringItemMode {

        /// <summary>
        /// Disabled
        /// </summary>
        Disabled,

        /// <summary>
        /// Sampling
        /// </summary>
        Sampling,

        /// <summary>
        /// Reporting
        /// </summary>
        Reporting
    }
}