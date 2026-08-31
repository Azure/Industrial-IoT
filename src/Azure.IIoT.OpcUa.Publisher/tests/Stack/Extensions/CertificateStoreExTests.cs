// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    using AzureStack = Azure.IIoT.OpcUa.Publisher.Stack;
    using Opc.Ua;
    using Opc.Ua.Security.Certificates;
    using System;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="CertificateStoreEx"/> pure configuration helpers.
    /// </summary>
    public sealed class CertificateStoreExTests
    {
        // ── ApplyLocalConfig(CertificateTrustList, CertificateStore?) ─────────

        [Fact]
        public void ApplyLocalConfig_TrustList_NullStore_DoesNothing()
        {
            var tl = new CertificateTrustList
            {
                StorePath = "original/path",
                StoreType = "X509Store"
            };

            tl.ApplyLocalConfig((AzureStack.CertificateStore?)null);

            Assert.Equal("original/path", tl.StorePath);
            Assert.Equal("X509Store", tl.StoreType);
        }

        [Fact]
        public void ApplyLocalConfig_TrustList_NullTrustList_ThrowsArgumentNull()
        {
            CertificateTrustList? tl = null;
            var store = new AzureStack.CertificateStore { StorePath = "new/path" };

            Assert.Throws<ArgumentNullException>(() =>
                CertificateStoreEx.ApplyLocalConfig(tl!, store));
        }

        [Fact]
        public void ApplyLocalConfig_TrustList_DifferentStorePath_UpdatesPathAndType()
        {
            var tl = new CertificateTrustList
            {
                StorePath = "old/path",
                StoreType = "OldType"
            };
            var store = new AzureStack.CertificateStore
            {
                StorePath = "new/path",
                StoreType = "NewType"
            };

            tl.ApplyLocalConfig(store);

            Assert.Equal("new/path", tl.StorePath);
            Assert.Equal("NewType", tl.StoreType);
        }

        [Fact]
        public void ApplyLocalConfig_TrustList_SameStorePath_DoesNotUpdate()
        {
            var tl = new CertificateTrustList
            {
                StorePath = "same/path",
                StoreType = "OrigType"
            };
            var store = new AzureStack.CertificateStore
            {
                StorePath = "same/path",
                StoreType = "NewType"
            };

            tl.ApplyLocalConfig(store);

            // Same path → condition false → StoreType not updated
            Assert.Equal("OrigType", tl.StoreType);
        }

        // ── ApplyLocalConfig(List<CertificateIdentifier>, CertificateInfo?) ───

        [Fact]
        public void ApplyLocalConfig_ListIdentifiers_NullList_ThrowsArgumentNull()
        {
            List<CertificateIdentifier>? list = null;
            var info = new AzureStack.CertificateInfo { StorePath = "path" };

            Assert.Throws<ArgumentNullException>(() =>
                CertificateStoreEx.ApplyLocalConfig(list!, info));
        }

        [Fact]
        public void ApplyLocalConfig_ListIdentifiers_NullStore_DoesNothing()
        {
            var list = new List<CertificateIdentifier>
            {
                new CertificateIdentifier { StorePath = "orig", StoreType = "X509" }
            };

            CertificateStoreEx.ApplyLocalConfig(list, (AzureStack.CertificateInfo?)null);

            Assert.Equal("orig", list[0].StorePath);
        }

        [Fact]
        public void ApplyLocalConfig_ListIdentifiers_DifferentPath_UpdatesAll()
        {
            var list = new List<CertificateIdentifier>
            {
                new CertificateIdentifier { StorePath = "old", StoreType = "OldType" },
                new CertificateIdentifier { StorePath = "old", StoreType = "OldType" }
            };
            var info = new AzureStack.CertificateInfo { StorePath = "new", StoreType = "NewType" };

            CertificateStoreEx.ApplyLocalConfig(list, info);

            Assert.All(list, c => Assert.Equal("new", c.StorePath));
            Assert.All(list, c => Assert.Equal("NewType", c.StoreType));
        }

        [Fact]
        public void ApplyLocalConfig_ListIdentifiers_SamePath_NoUpdate()
        {
            var list = new List<CertificateIdentifier>
            {
                new CertificateIdentifier { StorePath = "same", StoreType = "OrigType" }
            };
            var info = new AzureStack.CertificateInfo { StorePath = "same", StoreType = "NewType" };

            CertificateStoreEx.ApplyLocalConfig(list, info);

            Assert.Equal("OrigType", list[0].StoreType);
        }

        [Fact]
        public void ApplyLocalConfig_EmptyList_DoesNothing()
        {
            var list = new List<CertificateIdentifier>();
            var info = new AzureStack.CertificateInfo { StorePath = "new" };

            var ex = Record.Exception(() => CertificateStoreEx.ApplyLocalConfig(list, info));
            Assert.Null(ex);
        }

        // ── ApplyLocalConfig(CertificateStoreIdentifier, CertificateStore?) ──

        [Fact]
        public void ApplyLocalConfig_StoreIdentifier_NullIdentifier_ThrowsArgumentNull()
        {
            CertificateStoreIdentifier? id = null;
            var store = new AzureStack.CertificateStore { StorePath = "path" };

            Assert.Throws<ArgumentNullException>(() =>
                CertificateStoreEx.ApplyLocalConfig(id!, store));
        }

        [Fact]
        public void ApplyLocalConfig_StoreIdentifier_NullStore_DoesNothing()
        {
            var id = new CertificateStoreIdentifier
            {
                StorePath = "orig",
                StoreType = "X509"
            };

            CertificateStoreEx.ApplyLocalConfig(id, (AzureStack.CertificateStore?)null);

            Assert.Equal("orig", id.StorePath);
        }

        [Fact]
        public void ApplyLocalConfig_StoreIdentifier_DifferentPath_Updates()
        {
            var id = new CertificateStoreIdentifier
            {
                StorePath = "old",
                StoreType = "OldType"
            };
            var store = new AzureStack.CertificateStore { StorePath = "new", StoreType = "NewType" };

            CertificateStoreEx.ApplyLocalConfig(id, store);

            Assert.Equal("new", id.StorePath);
            Assert.Equal("NewType", id.StoreType);
        }

        [Fact]
        public void ApplyLocalConfig_StoreIdentifier_SamePath_NoUpdate()
        {
            var id = new CertificateStoreIdentifier
            {
                StorePath = "same",
                StoreType = "OrigType"
            };
            var store = new AzureStack.CertificateStore { StorePath = "same", StoreType = "NewType" };

            CertificateStoreEx.ApplyLocalConfig(id, store);

            Assert.Equal("OrigType", id.StoreType);
        }

        // ── ApplyLocalConfig(ArrayOf<CertificateIdentifier>, CertificateInfo?) ─

        [Fact]
        public void ApplyLocalConfig_ArrayOfIdentifiers_NullStore_DoesNothing()
        {
            ArrayOf<CertificateIdentifier> arr = new CertificateIdentifier[]
            {
                new CertificateIdentifier { StorePath = "orig", StoreType = "X509" }
            };

            CertificateStoreEx.ApplyLocalConfig(arr, (AzureStack.CertificateInfo?)null);

            var item = Assert.Single(arr.ToArray()!);
            Assert.Equal("orig", item.StorePath);
        }

        [Fact]
        public void ApplyLocalConfig_ArrayOfIdentifiers_DifferentPath_UpdatesAll()
        {
            ArrayOf<CertificateIdentifier> arr = new CertificateIdentifier[]
            {
                new CertificateIdentifier { StorePath = "old", StoreType = "OldType" },
                new CertificateIdentifier { StorePath = "old", StoreType = "OldType" }
            };
            var info = new AzureStack.CertificateInfo { StorePath = "new", StoreType = "NewType" };

            CertificateStoreEx.ApplyLocalConfig(arr, info);

            foreach (var item in arr)
            {
                Assert.Equal("new", item.StorePath);
                Assert.Equal("NewType", item.StoreType);
            }
        }

        [Fact]
        public void ApplyLocalConfig_ArrayOfIdentifiers_EmptyArray_DoesNothing()
        {
            var arr = ArrayOf<CertificateIdentifier>.Empty;
            var info = new AzureStack.CertificateInfo { StorePath = "new" };

            var ex = Record.Exception(() => CertificateStoreEx.ApplyLocalConfig(arr, info));
            Assert.Null(ex);
        }
    }
}
