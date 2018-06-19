// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Security assessment of the endpoint or application
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SecurityAssessment {

        /// <summary>
        /// Unknown security level
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Low or no security
        /// </summary>
        Low = 1,

        /// <summary>
        /// Good security
        /// </summary>
        Medium = 2,

        /// <summary>
        /// High level of security
        /// </summary>
        High = 3,
    }
}
