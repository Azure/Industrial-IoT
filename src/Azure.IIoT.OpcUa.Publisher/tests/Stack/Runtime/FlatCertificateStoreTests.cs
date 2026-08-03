// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Moq;
    using Opc.Ua;
    using System;
    using Xunit;

    public class FlatCertificateStoreTests
    {
        [Theory]
        [InlineData("FlatDirectory:C:\\certs", true)]
        [InlineData("flatdirectory:C:\\certs", true)]
        [InlineData("FLATDIRECTORY:C:\\certs", true)]
        [InlineData("Directory:C:\\certs", false)]
        [InlineData("", false)]
        public void SupportsStorePathMatchesFlatDirectoryPrefixCaseInsensitively(
            string storePath, bool expected)
        {
            var storeType = new FlatCertificateStore();

            var supported = storeType.SupportsStorePath(storePath);

            Assert.Equal(expected, supported);
        }

        [Fact]
        public void SupportsStorePathReturnsFalseForNull()
        {
            var storeType = new FlatCertificateStore();

            var supported = storeType.SupportsStorePath(null!);

            Assert.False(supported);
        }

        [Fact]
        public void CreateStoreReturnsFlatDirectoryStoreWithoutOpeningLocation()
        {
            var storeType = new FlatCertificateStore();

            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);

            Assert.Equal(FlatCertificateStore.StoreTypeName, store.StoreType);
            Assert.Equal(FlatCertificateStore.StoreTypeName + ":", FlatCertificateStore.StoreTypePrefix);
        }

        [Fact]
        public void FlatDirectoryStoreRejectsLocationsWithoutExactPrefixBeforeOpening()
        {
            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);

            var exception = Assert.Throws<ArgumentException>(() =>
                store.Open("flatdirectory:C:\\certs"));

            Assert.Equal("location", exception.ParamName);
        }

        [Fact]
        public void FlatDirectoryStoreRejectsNullOrEmptyLocationBeforeOpening()
        {
            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);

            Assert.Throws<ArgumentNullException>(() => store.Open(null!));
            Assert.Throws<ArgumentException>(() => store.Open(string.Empty));
        }
    }
}
