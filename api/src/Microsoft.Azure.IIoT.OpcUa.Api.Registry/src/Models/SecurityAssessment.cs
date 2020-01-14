// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Security assessment of the endpoint or application
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SecurityAssessment {

        /// <summary>
        /// Low
        /// </summary>
        Low,

        /// <summary>
        /// Medium
        /// </summary>
        Medium,

        /// <summary>
        /// High
        /// </summary>
        High
    }
}
