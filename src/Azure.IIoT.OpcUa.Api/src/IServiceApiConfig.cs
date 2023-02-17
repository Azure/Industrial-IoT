// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api {

    /// <summary>
    /// Configuration for service api
    /// </summary>
    public interface IServiceApiConfig {

        /// <summary>
        /// Web service url
        /// </summary>
        string ServiceUrl { get; }
    }
}
