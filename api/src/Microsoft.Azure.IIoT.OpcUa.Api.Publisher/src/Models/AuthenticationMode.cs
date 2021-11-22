// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Enum that defines the endpoint session authentication methods
    /// </summary>
    [DataContract]
    public enum AuthenticationMode {
        /// <summary> Anonymous authentication </summary>
        [EnumMember]
        Anonymous,
        /// <summary> Username/Password authentication </summary>
        [EnumMember]
        UsernamePassword
    }
}
