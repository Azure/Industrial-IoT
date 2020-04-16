// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {

    /// <summary>
    /// Injectable token client implementation - multiple
    /// per process and encapsulating the provider.
    /// </summary>
    public interface ITokenClient : ITokenAcquisition {
    }
}
