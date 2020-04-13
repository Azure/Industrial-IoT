// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin {

    /// <summary>
    /// Configuration for service
    /// </summary>
    public interface ITwinConfig {

        /// <summary>
        /// Opc twin service url
        /// </summary>
        string OpcUaTwinServiceUrl { get; }
    }
}
