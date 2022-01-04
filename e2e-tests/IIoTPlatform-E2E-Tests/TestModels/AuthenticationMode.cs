// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestModels {
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
