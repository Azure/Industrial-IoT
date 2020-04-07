// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Flags for use with the AccessRestrictions attribute.
    /// </summary>
    [Flags]
    [DataContract]
    public enum NodeAccessRestrictions {

        /// <summary>
        /// Requires SecureChannel which digitally signs all messages.
        /// </summary>
        [EnumMember]
        SigningRequired = 0x1,

        /// <summary>
        /// Requires SecureChannel which encrypts all messages.
        /// </summary>
        [EnumMember]
        EncryptionRequired = 0x2,

        /// <summary>
        /// No SessionlessInvoke invocation.
        /// </summary>
        [EnumMember]
        SessionRequired = 0x4
    }
}
