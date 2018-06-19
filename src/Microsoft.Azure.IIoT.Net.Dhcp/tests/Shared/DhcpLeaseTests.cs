// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp.Shared {
    using Xunit;
    using System.Collections.Generic;
    using Microsoft.Azure.IIoT.Net;
    using System.Net.NetworkInformation;
    using System.Net;
    using AutoFixture;
    using System.Linq;

    public class DhcpLeaseTests {

        [Fact]
        void TestIdEquals1() {
            var fix = new Fixture();
            var address = new IPAddress(fix.CreateMany<byte>(4).ToArray());
            var mac = fix.Create<PhysicalAddress>();

            var id1 = new DhcpLease.Id(address.Copy(), mac.Copy());
            var id2 = new DhcpLease.Id(address.Copy(), mac.Copy());

            var lookup = new Dictionary<DhcpLease.Id, int> {
                { id1, 1 }
            };
            var set = new HashSet<DhcpLease.Id> { id1, id2 };

            Assert.Equal(id1, id2);
            Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
            Assert.Equal(1, lookup[id2]);
            Assert.Single(set);
            Assert.Equal(id2, set.Single());
            Assert.Equal(id1, set.Single());
        }

        [Fact]
        void TestIdEquals2() {
            var fix = new Fixture();
            var address = new IPAddress(fix.CreateMany<byte>(4).ToArray());

            var id1 = new DhcpLease.Id(address.Copy(), null);
            var id2 = new DhcpLease.Id(address.Copy(), null);

            var lookup = new Dictionary<DhcpLease.Id, int> {
                { id1, 1 }
            };
            var set = new HashSet<DhcpLease.Id> { id1, id2 };

            Assert.Equal(id1, id2);
            Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
            Assert.Equal(1, lookup[id2]);
            Assert.Single(set);
            Assert.Equal(id2, set.Single());
            Assert.Equal(id1, set.Single());
        }

        [Fact]
        void TestIdNotEquals1() {
            var fix = new Fixture();
            var address = new IPAddress(fix.CreateMany<byte>(4).ToArray());
            var mac = fix.Create<PhysicalAddress>();

            var id1 = new DhcpLease.Id(address.Copy(), mac.Copy());
            var id2 = new DhcpLease.Id(address.Copy(), null);

            var lookup = new Dictionary<DhcpLease.Id, int> {
                { id1, 1 }
            };
            var set = new HashSet<DhcpLease.Id> { id1, id2 };

            Assert.NotEqual(id1, id2);
            Assert.NotEqual(id1.GetHashCode(), id2.GetHashCode());
            Assert.False(lookup.TryGetValue(id2, out var test));
            Assert.Equal(2, set.Count);
        }

        [Fact]
        void TestIdNotEquals2() {
            var fix = new Fixture();
            var address = new IPAddress(fix.CreateMany<byte>(4).ToArray());
            var mac1 = fix.Create<PhysicalAddress>();
            var mac2 = fix.Create<PhysicalAddress>();

            var id1 = new DhcpLease.Id(address.Copy(), mac1);
            var id2 = new DhcpLease.Id(address.Copy(), mac2);

            var lookup = new Dictionary<DhcpLease.Id, int> {
                { id1, 1 }
            };
            var set = new HashSet<DhcpLease.Id> { id1, id2 };

            Assert.NotEqual(id1, id2);
            Assert.NotEqual(id1.GetHashCode(), id2.GetHashCode());
            Assert.False(lookup.TryGetValue(id2, out var test));
            Assert.Equal(2, set.Count);
        }

        [Fact]
        void TestIdNotEquals3() {
            var fix = new Fixture();
            var address1 = new IPAddress(fix.CreateMany<byte>(4).ToArray());
            var address2 = new IPAddress(fix.CreateMany<byte>(4).ToArray());
            var mac = fix.Create<PhysicalAddress>();

            var id1 = new DhcpLease.Id(address1.Copy(), mac.Copy());
            var id2 = new DhcpLease.Id(address2.Copy(), mac.Copy());

            var lookup = new Dictionary<DhcpLease.Id, int> {
                { id1, 1 }
            };
            var set = new HashSet<DhcpLease.Id> { id1, id2 };

            Assert.NotEqual(id1, id2);
            Assert.NotEqual(id1.GetHashCode(), id2.GetHashCode());
            Assert.False(lookup.TryGetValue(id2, out var test));
            Assert.Equal(2, set.Count);
        }

        [Fact]
        void TestLeaseIdEquals() {
            var fix = new Fixture();
            var address = new IPAddress(fix.CreateMany<byte>(4).ToArray());
            var mac = fix.Create<PhysicalAddress>();

            var lease1 = new DhcpLease(address.Copy(), mac.Copy());
            var lease2 = new DhcpLease(address.Copy(), mac.Copy());

            var lookup = new Dictionary<DhcpLease.Id, DhcpLease> {
                { lease1.Identifier, lease1 }
            };

            Assert.Equal(lease1.Identifier, lease2.Identifier);
            Assert.Equal(lease1, lookup[lease2.Identifier]);
        }
    }
}
