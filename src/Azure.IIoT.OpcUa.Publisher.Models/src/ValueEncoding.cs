// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// The OPC UA JSON encoding to use when serializing a value in
    /// a response.
    /// </summary>
    [DataContract]
    public enum ValueEncoding
    {
        /// <summary>
        /// Reversible JSON encoding (default). Preserves all OPC UA
        /// type information.
        /// </summary>
        [EnumMember(Value = "Reversible")]
        Reversible,

        /// <summary>
        /// Non-reversible JSON encoding. Produces a more compact
        /// representation matching the format used by non-reversible
        /// PubSub messages.
        /// </summary>
        [EnumMember(Value = "NonReversible")]
        NonReversible
    }
}
