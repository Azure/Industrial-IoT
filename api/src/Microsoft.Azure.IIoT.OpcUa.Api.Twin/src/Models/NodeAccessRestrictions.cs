// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;

    /// <summary>
    /// Flags for use with the AccessRestrictions attribute.
    /// </summary>
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NodeAccessRestrictions {

        /// <summary>
        /// Requires SecureChannel which digitally signs all messages.
        /// </summary>
        SigningRequired = 0x1,

        /// <summary>
        /// Requires SecureChannel which encrypts all messages.
        /// </summary>
        EncryptionRequired = 0x2,

        /// <summary>
        /// No SessionlessInvoke invocation.
        /// </summary>
        SessionRequired = 0x4
    }
}
