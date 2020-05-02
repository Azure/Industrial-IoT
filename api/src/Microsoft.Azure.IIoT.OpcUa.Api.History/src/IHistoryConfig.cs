// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History {

    /// <summary>
    /// Configuration for service
    /// </summary>
    public interface IHistoryConfig {

        /// <summary>
        /// Opc History service url
        /// </summary>
        string OpcUaHistoryServiceUrl { get; }
    }
}
