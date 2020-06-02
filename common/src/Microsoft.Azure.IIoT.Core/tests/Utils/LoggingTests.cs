// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Core.Tests {
    using Xunit;
    using static Microsoft.Azure.IIoT.Utils.Logging;

    /// <summary>
    /// Simple bitmap on top of ulong list
    /// </summary>
    public class LoggingTests {

        [Fact]
        public void TestHostsWithPort() {
            Assert.Equal("example.com:50000", ExtractServerPort("opc.tcp://example.com:50000"));
            Assert.Equal("example.com:50000", ExtractServerPort("opc.tcp://example.com:50000/"));
            Assert.Equal("example.com:50000", ExtractServerPort("opc.tcp://example.com:50000_hash"));
        }

        [Fact]
        public void TestHostsWithoutPort() {
            Assert.Equal("example.com", ExtractServerPort("opc.tcp://example.com"));
            Assert.Equal("example.com", ExtractServerPort("opc.tcp://example.com/"));
            Assert.Equal("example.com", ExtractServerPort("opc.tcp://example.com_hash"));
        }

        [Fact]
        public void TestIPsWithPort()
        {
            Assert.Equal("10.1.9.5:50000", ExtractServerPort("opc.tcp://10.1.9.5:50000"));
            Assert.Equal("10.1.9.5:50000", ExtractServerPort("opc.tcp://10.1.9.5:50000/"));
            Assert.Equal("10.1.9.5:50000", ExtractServerPort("opc.tcp://10.1.9.5:50000_hash"));
        }

        [Fact]
        public void TestIPsWithoutPort()
        {
            Assert.Equal("10.1.9.5", ExtractServerPort("opc.tcp://10.1.9.5"));
            Assert.Equal("10.1.9.5", ExtractServerPort("opc.tcp://10.1.9.5/"));
            Assert.Equal("10.1.9.5", ExtractServerPort("opc.tcp://10.1.9.5_hash"));
        }
    }
}
