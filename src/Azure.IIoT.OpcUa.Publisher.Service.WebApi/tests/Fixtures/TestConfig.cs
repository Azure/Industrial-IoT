// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi
{
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk;
    using System;

    /// <inheritdoc/>
    public class TestConfig : IServiceApiConfig
    {
        /// <summary>
        /// Create test configuration
        /// </summary>
        /// <param name="baseAddress"></param>
        public TestConfig(Uri baseAddress)
        {
            ServiceUrl = baseAddress.ToString().TrimEnd('/');
        }

        /// <inheritdoc/>
        public string ServiceUrl { get; }
    }
}
