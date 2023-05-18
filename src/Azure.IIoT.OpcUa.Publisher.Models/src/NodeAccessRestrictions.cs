// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Flags that can be read or written in the
    /// AccessRestrictions attribute.
    /// </summary>
    [Flags]
    [DataContract]
    public enum NodeAccessRestrictions
    {
        /// <summary>
        /// No restrictions
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0x0,

        /// <summary>
        /// The Client can only access the Node when using a
        /// SecureChannel which digitally signs all messages.
        /// </summary>
        [EnumMember(Value = "SigningRequired")]
        SigningRequired = 0x1,

        /// <summary>
        /// The Client can only access the Node when using a
        /// SecureChannel which encrypts all messages.
        /// </summary>
        [EnumMember(Value = "EncryptionRequired")]
        EncryptionRequired = 0x2,

        /// <summary>
        /// The Client cannot access the Node when using
        /// SessionlessInvoke Service invocation.
        /// </summary>
        [EnumMember(Value = "SessionRequired")]
        SessionRequired = 0x4
    }
}
