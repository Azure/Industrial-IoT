// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.History {
    using Microsoft.Azure.IIoT.OpcUa.Api.History;
    using System;

    /// <inheritdoc/>
    public class TestConfig : IHistoryConfig {

        /// <summary>
        /// Create test configuration
        /// </summary>
        /// <param name="baseAddress"></param>
        public TestConfig(Uri baseAddress) {
            OpcUaHistoryServiceUrl = baseAddress.ToString().TrimEnd('/');
        }

        /// <inheritdoc/>
        public string OpcUaHistoryServiceUrl { get; }
    }
}
