// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Config {

    public interface IOpcPlcConfig {

        /// <summary>
        /// Semicolon separated URLs to load published_nodes.json from OPC-PLCs
        /// </summary>
        string Urls { get; }
    }
}
