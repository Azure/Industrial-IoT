// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {

    /// <summary>
    /// Token provider is a single source aggregate per
    /// process and used directly by a service implementation.
    /// </summary>
    public interface ITokenProvider : ITokenAcquisition {
    }
}
