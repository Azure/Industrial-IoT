// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Specifies the security mode for OPC UA endpoint connections.
    /// Determines how messages are protected during transmission between
    /// the Publisher and OPC UA servers. Proper security mode selection
    /// is crucial for protecting sensitive data and credentials.
    /// </summary>
    [DataContract]
    [Flags]
    public enum SecurityMode
    {
        /// <summary>
        /// Selects the highest security level available from the server.
        /// WARNING: May select None if no secure endpoints are available.
        /// Not recommended for production - use explicit security modes
        /// to enforce security requirements.
        /// </summary>
        [EnumMember(Value = "Best")]
        Best,

        /// <summary>
        /// Messages are signed but not encrypted.
        /// Ensures message integrity and authenticity.
        /// Protects against tampering but not eavesdropping.
        /// Use when data confidentiality is not required.
        /// </summary>
        [EnumMember(Value = "Sign")]
        Sign,

        /// <summary>
        /// Messages are both signed and encrypted.
        /// Provides maximum security with full message protection.
        /// Ensures confidentiality, integrity, and authenticity.
        /// Recommended when transmitting sensitive data or credentials.
        /// </summary>
        [EnumMember(Value = "SignAndEncrypt")]
        SignAndEncrypt,

        /// <summary>
        /// No message security applied.
        /// WARNING: Transmits all data in clear text.
        /// Should only be used in secure networks or for testing.
        /// Not recommended for production environments.
        /// </summary>
        [EnumMember(Value = "None")]
        None,

        /// <summary>
        /// Requires either Sign or SignAndEncrypt mode.
        /// Ensures some level of message protection.
        /// Default when UseSecurity is true.
        /// Recommended minimum security for production use.
        /// </summary>
        [EnumMember(Value = "NotNone")]
        NotNone,
    }
}
