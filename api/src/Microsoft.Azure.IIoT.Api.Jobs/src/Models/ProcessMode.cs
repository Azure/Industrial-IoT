// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Processing mode for processing engine
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProcessMode {

        /// <summary>
        /// Active processing
        /// </summary>
        Active,

        /// <summary>
        /// Passive
        /// </summary>
        Passive
    }
}