// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Security mode of endpoint
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SecurityMode {

        /// <summary>
        /// Best
        /// </summary>
        Best,

        /// <summary>
        /// Sign
        /// </summary>
        Sign,

        /// <summary>
        /// Sign and Encrypt
        /// </summary>
        SignAndEncrypt,

        /// <summary>
        /// No security
        /// </summary>
        None
    }
}
