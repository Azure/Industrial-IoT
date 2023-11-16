// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    /// <summary>
    /// Provide api key
    /// </summary>
    public interface IApiKeyProvider
    {
        /// <summary>
        /// Api key
        /// </summary>
        string? ApiKey { get; }
    }
}
