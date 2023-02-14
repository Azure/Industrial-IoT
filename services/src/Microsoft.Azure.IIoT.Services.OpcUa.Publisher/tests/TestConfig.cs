// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Publisher.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using System;

    /// <inheritdoc/>
    public class TestConfig : IPublisherConfig {

        /// <summary>
        /// Create test configuration
        /// </summary>
        /// <param name="baseAddress"></param>
        public TestConfig(Uri baseAddress) {
            OpcUaPublisherServiceUrl = baseAddress.ToString().TrimEnd('/');
        }

        /// <inheritdoc/>
        public string OpcUaPublisherServiceUrl { get; }

        /// <inheritdoc/>
        public string OpcUaTwinServiceResourceId => null;
    }
}
