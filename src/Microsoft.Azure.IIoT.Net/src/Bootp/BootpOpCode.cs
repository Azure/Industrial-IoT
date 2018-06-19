// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Bootp {

    /// <summary>
    /// Operation
    /// </summary>
    public enum BootpOpCode : byte {
        BootRequest = 0x01,
        BootReply
    }
}
