// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp.Shared {
    using AutoFixture;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Net.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using Xunit;

    public class DhcpScopeTests {

        [Fact]
        public void TestRange1() {
            var scope = CreateScope();
            Assert.Equal(253, scope.Available.Count());
            Assert.Empty(scope.Assigned);
            Assert.Equal(IPAddress.Parse("192.168.1.1"), scope.Available.First());
            Assert.DoesNotContain(IPAddress.Parse("192.168.1.100"), scope.Available);
            Assert.Contains(IPAddress.Parse("192.168.1.222"), scope.Available);
        }

        [Fact]
        public void TestRange2() {
            var scope = CreateScope(10, 10);
            Assert.Equal(233, scope.Available.Count());
            Assert.Empty(scope.Assigned);
            Assert.Equal(IPAddress.Parse("192.168.1.11"), scope.Available.First());
            Assert.DoesNotContain(IPAddress.Parse("192.168.1.10"), scope.Available);
            Assert.DoesNotContain(IPAddress.Parse("192.168.1.100"), scope.Available);
            Assert.Contains(IPAddress.Parse("192.168.1.222"), scope.Available);
        }

        [Fact]
        public void TestRange3() {
            var scope = CreateScope(0, 0, new HashSet<IPAddress> {
                IPAddress.Parse("192.168.1.222"),
                IPAddress.Parse("192.168.1.223"),
                IPAddress.Parse("192.168.1.224"),
                IPAddress.Parse("192.168.1.225")
            });
            Assert.Equal(249, scope.Available.Count());
            Assert.Equal(IPAddress.Parse("192.168.1.1"), scope.Available.First());
            Assert.Empty(scope.Assigned);
            Assert.DoesNotContain(IPAddress.Parse("192.168.1.100"), scope.Available);
            Assert.Contains(IPAddress.Parse("192.168.1.221"), scope.Available);
            Assert.DoesNotContain(IPAddress.Parse("192.168.1.222"), scope.Available);
            Assert.DoesNotContain(IPAddress.Parse("192.168.1.223"), scope.Available);
        }

        [Fact]
        public void TestLeaseAllocationThatIsAvailable() {
            var scope = CreateScope();
            var address = IPAddress.Parse("192.168.1.222");
            var mac = new Fixture().Create<PhysicalAddress>();

            // Expected
            var id = new DhcpLease.Id(address, mac);

            var lease = scope.Allocate(mac, address, 555u, TimeSpan.FromSeconds(1));

            Assert.Equal(id, lease.Identifier);
            Assert.Equal(555u, lease.TransactionId);
            Assert.Equal(252, scope.Available.Count());
            Assert.Equal(IPAddress.Parse("192.168.1.1"), scope.Available.First());
            Assert.Single(scope.Assigned);
            Assert.Equal(lease, scope.Assigned.Single());
            Assert.DoesNotContain(IPAddress.Parse("192.168.1.222"), scope.Available);
            Assert.Contains(IPAddress.Parse("192.168.1.221"), scope.Available);
        }

        [Fact]
        public void TestLeaseAllocationWhenRequestedUnavailable() {
            var scope = CreateScope();
            var address = IPAddress.Parse("192.168.8.222"); // Not in assignable
            var mac = new Fixture().Create<PhysicalAddress>();

            // Expected
            var id = new DhcpLease.Id(IPAddress.Parse("192.168.1.1"), mac);

            var lease = scope.Allocate(mac, address, 555u, TimeSpan.FromSeconds(1));

            Assert.Equal(id, lease.Identifier);
            Assert.Equal(555u, lease.TransactionId);
            Assert.Equal(252, scope.Available.Count());
            Assert.Equal(IPAddress.Parse("192.168.1.2"), scope.Available.First());
            Assert.Single(scope.Assigned);
            Assert.Equal(lease, scope.Assigned.Single());
            Assert.DoesNotContain(IPAddress.Parse("192.168.1.1"), scope.Available);
            Assert.Contains(IPAddress.Parse("192.168.1.222"), scope.Available);
        }

        [Fact]
        public void TestLeaseAllocationAny() {
            var scope = CreateScope();
            var mac = new Fixture().Create<PhysicalAddress>();

            // Expected
            var id = new DhcpLease.Id(IPAddress.Parse("192.168.1.1"), mac);

            var lease = scope.Allocate(mac, null, 555u, TimeSpan.FromSeconds(1));

            Assert.Equal(id, lease.Identifier);
            Assert.Equal(555u, lease.TransactionId);
            Assert.Equal(252, scope.Available.Count());
            Assert.Equal(IPAddress.Parse("192.168.1.2"), scope.Available.First());
            Assert.Single(scope.Assigned);
            Assert.Equal(lease, scope.Assigned.Single());
            Assert.DoesNotContain(IPAddress.Parse("192.168.1.1"), scope.Available);
            Assert.Contains(IPAddress.Parse("192.168.1.222"), scope.Available);
        }

        [Fact]
        public void TestDoubleLeaseAllocation() {
            var scope = CreateScope();
            var address = IPAddress.Parse("192.168.1.222");
            var mac = new Fixture().Create<PhysicalAddress>();

            // Expected
            var id = new DhcpLease.Id(address, mac);

            var lease1 = scope.Allocate(mac.Copy(), address.Copy(), 555u, TimeSpan.FromSeconds(1));
            var lease2 = scope.Allocate(mac.Copy(), address.Copy(), 666u, TimeSpan.FromSeconds(2));

            Assert.Equal(id, lease1.Identifier);
            Assert.Equal(id, lease2.Identifier);
            Assert.Equal(555u, lease1.TransactionId);
            Assert.Equal(666u, lease2.TransactionId);
            Assert.Equal(252, scope.Available.Count());
            Assert.Equal(IPAddress.Parse("192.168.1.1"), scope.Available.First());
            Assert.Single(scope.Assigned);
            Assert.Equal(lease2, scope.Assigned.Single());
            Assert.DoesNotContain(IPAddress.Parse("192.168.1.222"), scope.Available);
            Assert.Contains(IPAddress.Parse("192.168.1.221"), scope.Available);
        }

        [Fact]
        public void TestAllocationOfTwoAddresses() {
            var scope = CreateScope();
            var address1 = IPAddress.Parse("192.168.1.222");
            var address2 = IPAddress.Parse("192.168.1.223");
            var mac = new Fixture().Create<PhysicalAddress>();

            // Expected
            var id1 = new DhcpLease.Id(address1, mac);
            var id2 = new DhcpLease.Id(address2, mac);

            var lease1 = scope.Allocate(mac.Copy(), address1.Copy(), 555u, TimeSpan.FromSeconds(1));
            var lease2 = scope.Allocate(mac.Copy(), address2.Copy(), 666u, TimeSpan.FromSeconds(2));

            Assert.Equal(id1, lease1.Identifier);
            Assert.Equal(id2, lease2.Identifier);
            Assert.Equal(555u, lease1.TransactionId);
            Assert.Equal(666u, lease2.TransactionId);
            Assert.Equal(251, scope.Available.Count());
            Assert.Equal(IPAddress.Parse("192.168.1.1"), scope.Available.First());
            Assert.Equal(2, scope.Assigned.Count());
            Assert.Contains(lease1, scope.Assigned);
            Assert.Contains(lease2, scope.Assigned);
            Assert.DoesNotContain(IPAddress.Parse("192.168.1.223"), scope.Available);
            Assert.DoesNotContain(IPAddress.Parse("192.168.1.222"), scope.Available);
            Assert.Contains(IPAddress.Parse("192.168.1.221"), scope.Available);
        }

        [Fact]
        public void TestConfirmAllocated() {
            var scope = CreateScope();
            var address = IPAddress.Parse("192.168.1.222");
            var mac = new Fixture().Create<PhysicalAddress>();

            var lease1 = scope.Allocate(mac.Copy(), address.Copy(), 555u, TimeSpan.FromSeconds(1));
            var lease2 = scope.Commit(mac.Copy(), address.Copy(), 555u, TimeSpan.FromSeconds(1));

            Assert.Equal(lease1.Identifier, lease2.Identifier);
            Assert.False(lease1.Accepted);
            Assert.True(lease2.Accepted);
            Assert.True(scope.Assigned.Single().Accepted);
            Assert.Single(scope.Assigned);
            Assert.Equal(252, scope.Available.Count());
        }

        [Fact]
        public void TestConfirmNotAllocated() {
            var scope = CreateScope();
            var address = IPAddress.Parse("192.168.1.222");
            var mac = new Fixture().Create<PhysicalAddress>();

            var lease = scope.Commit(mac.Copy(), address.Copy(),
                555u, TimeSpan.FromSeconds(1));

            Assert.Null(lease);
            Assert.Empty(scope.Assigned);
            Assert.Contains(IPAddress.Parse("192.168.1.222"), scope.Available);
            Assert.Equal(253, scope.Available.Count());
        }

        [Fact]
        public void TestConfirmAllocatedDifferentXid() {
            var scope = CreateScope();
            var address = IPAddress.Parse("192.168.1.222");
            var mac = new Fixture().Create<PhysicalAddress>();

            var lease1 = scope.Allocate(mac.Copy(), address.Copy(), 666u, TimeSpan.FromSeconds(1));
            var lease2 = scope.Commit(mac.Copy(), address.Copy(), 555u, TimeSpan.FromSeconds(1));

            Assert.Null(lease2);
            Assert.False(lease1.Accepted);
            Assert.False(scope.Assigned.Single().Accepted);
            Assert.False(scope.Assigned.Single().External);
            Assert.Single(scope.Assigned);
            Assert.DoesNotContain(IPAddress.Parse("192.168.1.222"), scope.Available);
            Assert.Equal(252, scope.Available.Count());
        }

        [Fact]
        public void TestReleaseConfirmed() {
            var scope = CreateScope();
            var address = IPAddress.Parse("192.168.1.222");
            var mac = new Fixture().Create<PhysicalAddress>();

            var lease1 = scope.Allocate(mac.Copy(), address.Copy(), 555u, TimeSpan.FromSeconds(1));
            var lease2 = scope.Commit(mac.Copy(), address.Copy(), 555u, TimeSpan.FromSeconds(1));
            scope.Release(mac.Copy(), address.Copy());

            Assert.Empty(scope.Assigned);
            Assert.Contains(IPAddress.Parse("192.168.1.222"), scope.Available);
            Assert.Equal(253, scope.Available.Count());
        }

        [Fact]
        public void TestDeclineAllocated() {
            var scope = CreateScope();
            var address = IPAddress.Parse("192.168.1.222");
            var mac = new Fixture().Create<PhysicalAddress>();

            var lease1 = scope.Allocate(mac.Copy(), address.Copy(), 555u, TimeSpan.FromSeconds(1));
            scope.Shelve(mac.Copy(), address.Copy(), TimeSpan.FromSeconds(1));

            Assert.Single(scope.Assigned);
            Assert.True(scope.Assigned.Single().Accepted);
            Assert.True(scope.Assigned.Single().External);
            Assert.DoesNotContain(IPAddress.Parse("192.168.1.222"), scope.Available);
            Assert.Equal(252, scope.Available.Count());
        }

        /// <summary>
        /// Helper to create scope object
        /// </summary>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <param name="reserved"></param>
        /// <returns></returns>
        private DhcpScope CreateScope(int b = 0, int t = 0,
            HashSet<IPAddress> reserved = null) {
            var fix = new Fixture();
            var address = new IPAddress(fix.CreateMany<byte>(4).ToArray());
            var mac = fix.Create<PhysicalAddress>();

            return new DhcpScope(new NetInterface("test",
                IPAddress.Parse("192.168.1.100"), IPAddress.Parse("255.255.255.0")),
                b, t, new ConsoleLogger("test", LogLevel.Debug), reserved);
        }
    }
}
