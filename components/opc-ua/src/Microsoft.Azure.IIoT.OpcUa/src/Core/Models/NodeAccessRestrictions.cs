// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System;

    /// <summary>
    /// Flags that can be read or written in the
    /// AccessRestrictions attribute.
    /// </summary>
    [Flags]
    public enum NodeAccessRestrictions {

        /// <summary>
        /// The Client can only access the Node when using a
        /// SecureChannel which digitally signs all messages.
        /// </summary>
        SigningRequired = 0x1,

        /// <summary>
        /// The Client can only access the Node when using a
        /// SecureChannel which encrypts all messages.
        /// </summary>
        EncryptionRequired = 0x2,

        /// <summary>
        /// The Client cannot access the Node when using
        /// SessionlessInvoke Service invocation.
        /// </summary>
        SessionRequired = 0x4
    }
}
