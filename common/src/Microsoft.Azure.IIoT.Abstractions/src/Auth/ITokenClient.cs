// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {

    /// <summary>
    /// Injectable identity provider specific token acquisition
    /// client implementation - multiple per process and
    /// encapsulating the identity platform and flow used.
    /// </summary>
    public interface ITokenClient : ITokenAcquisition {
    }
}
