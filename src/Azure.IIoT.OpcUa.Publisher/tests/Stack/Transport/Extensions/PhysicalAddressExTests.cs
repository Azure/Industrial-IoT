// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Transport.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport;
    using System.Net.NetworkInformation;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="PhysicalAddressEx.Copy"/>.
    /// </summary>
    public sealed class PhysicalAddressExTests
    {
        [Fact]
        public void Copy_ReturnsDifferentInstance()
        {
            var original = new PhysicalAddress([0x00, 0x11, 0x22, 0x33, 0x44, 0x55]);

            var copy = original.Copy();

            Assert.NotSame(original, copy);
        }

        [Fact]
        public void Copy_PreservesAddressBytes()
        {
            var original = new PhysicalAddress([0x00, 0x11, 0x22, 0x33, 0x44, 0x55]);

            var copy = original.Copy();

            Assert.Equal(original.GetAddressBytes(), copy.GetAddressBytes());
        }

        [Fact]
        public void Copy_EmptyAddress_ReturnsEmptyAddress()
        {
            var original = new PhysicalAddress([]);

            var copy = original.Copy();

            Assert.Empty(copy.GetAddressBytes());
        }

        [Fact]
        public void Copy_MutatingOriginalBytesDoesNotAffectCopy()
        {
            var bytes = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
            var original = new PhysicalAddress(bytes);
            var copy = original.Copy();

            // Mutate the original bytes array (not the address internals, but
            // we verify copy has its own byte array).
            bytes[0] = 0x00;

            // The copy was created from a snapshot, so its bytes are unchanged.
            Assert.Equal(0xAA, copy.GetAddressBytes()[0]);
        }

        [Fact]
        public void Copy_SingleByteAddress_IsPreserved()
        {
            var original = new PhysicalAddress([0xFE]);

            var copy = original.Copy();

            Assert.Equal(original.GetAddressBytes(), copy.GetAddressBytes());
        }
    }
}
