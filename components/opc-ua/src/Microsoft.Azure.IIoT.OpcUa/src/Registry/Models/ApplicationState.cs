// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// State of the application
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ApplicationState {

        /// <summary>
        /// Newly registered
        /// </summary>
        New,

        /// <summary>
        /// Approved and ready to use
        /// </summary>
        Approved,

        /// <summary>
        /// Rejected
        /// </summary>
        Rejected
    }
}

