// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Security mode of endpoint
    /// </summary>
    [DataContract]
    public enum SecurityMode
    {
        /// <summary>
        /// Use any security mode
        /// </summary>
        [EnumMember(Value = "Best")]
        Best,

        /// <summary>
        /// Sign
        /// </summary>
        [EnumMember(Value = "Sign")]
        Sign,

        /// <summary>
        /// Sign and Encrypt
        /// </summary>
        [EnumMember(Value = "SignAndEncrypt")]
        SignAndEncrypt,

        /// <summary>
        /// No security
        /// </summary>
        [EnumMember(Value = "None")]
        None
    }
}
