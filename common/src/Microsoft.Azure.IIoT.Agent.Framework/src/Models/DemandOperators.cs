// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Demand operator
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DemandOperators {

        /// <summary>
        /// Equals
        /// </summary>
        Equals,

        /// <summary>
        /// Match
        /// </summary>
        Match,

        /// <summary>
        /// Exists
        /// </summary>
        Exists
    }
}