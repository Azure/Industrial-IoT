// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Transport
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport;
    using System.Net.NetworkInformation;
    using Xunit;

    public class NetworkInformationExTests
    {
        [Fact]
        public void TestCopy()
        {
            var addr1 = new PhysicalAddress([0, 1, 2, 3, 4, 5, 6, 7]);
            var addr2 = new PhysicalAddress([0, 1, 2, 3, 4, 5, 6, 7]);
            var addr3 = addr1.Copy();

            Assert.Equal(addr1, addr2);
            Assert.Equal(addr1, addr3);
            Assert.Equal(addr2, addr3);
            Assert.Equal(addr1.GetHashCode(), addr2.GetHashCode());
            Assert.Equal(addr1.GetHashCode(), addr3.GetHashCode());
        }

        [Fact]
        public void TestNotEqual()
        {
            var addr1 = new PhysicalAddress([0, 1, 2, 3, 4, 5, 6, 7]);
            var addr2 = new PhysicalAddress([1, 1, 2, 3, 4, 5, 6, 7]);
            var addr3 = new PhysicalAddress([1, 1, 2, 3, 4, 5, 6]);

            Assert.NotEqual(addr1, addr2);
            Assert.NotEqual(addr1, addr3);
            Assert.NotEqual(addr2, addr3);
            Assert.NotEqual(addr1.GetHashCode(), addr2.GetHashCode());
            Assert.NotEqual(addr1.GetHashCode(), addr3.GetHashCode());
        }
    }
}
