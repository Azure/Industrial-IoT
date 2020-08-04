// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Utils.Tests {
    using Xunit;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Utils;

    /// <summary>
    /// Extract host from uri and format for simpler logging
    /// </summary>
    public class LoggingTests {

        [Fact]
        public void TestHostsWithPort() {
            Assert.Equal("example.com:50000", LoggingHelper.ExtractHost("opc.tcp://example.com:50000"));
            Assert.Equal("example.com:50000", LoggingHelper.ExtractHost("opc.tcp://example.com:50000/"));
            Assert.Equal("example.com:50000", LoggingHelper.ExtractHost("opc.tcp://example.com:50000_hash"));
        }

        [Fact]
        public void TestHostsWithoutPort() {
            Assert.Equal("example.com", LoggingHelper.ExtractHost("opc.tcp://example.com"));
            Assert.Equal("example.com", LoggingHelper.ExtractHost("opc.tcp://example.com/"));
            Assert.Equal("example.com", LoggingHelper.ExtractHost("opc.tcp://example.com_hash"));
        }

        [Fact]
        public void TestIPsWithPort()
        {
            Assert.Equal("10.1.9.5:50000", LoggingHelper.ExtractHost("opc.tcp://10.1.9.5:50000"));
            Assert.Equal("10.1.9.5:50000", LoggingHelper.ExtractHost("opc.tcp://10.1.9.5:50000/"));
            Assert.Equal("10.1.9.5:50000", LoggingHelper.ExtractHost("opc.tcp://10.1.9.5:50000_hash"));
        }

        [Fact]
        public void TestIPsWithoutPort()
        {
            Assert.Equal("10.1.9.5", LoggingHelper.ExtractHost("opc.tcp://10.1.9.5"));
            Assert.Equal("10.1.9.5", LoggingHelper.ExtractHost("opc.tcp://10.1.9.5/"));
            Assert.Equal("10.1.9.5", LoggingHelper.ExtractHost("opc.tcp://10.1.9.5_hash"));
        }

        [Fact]
        public void EdgeCases()
        {
            Assert.Equal("", LoggingHelper.ExtractHost(""));
            Assert.Equal("", LoggingHelper.ExtractHost("opc.tcp://"));
        }
    }
}