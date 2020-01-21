// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.BouncyCastle {
    using Org.BouncyCastle.Asn1.Pkcs;
    using System;
    using Xunit;

    /// <summary>
    /// Certificate request info tests
    /// </summary>
    public static class CertificationRequestInfoTests {

        [Fact]
        public static void ArgumentTests() {
            CertificationRequestInfo info = null;
            byte[] buffer = null;
            Assert.Throws<ArgumentNullException>(() => buffer.ToCertificationRequestInfo());
            Assert.Throws<ArgumentNullException>(() => buffer.ToPkcs10CertificationRequest());
            Assert.Throws<ArgumentNullException>(() => info.GetX509Extensions());
            Assert.Throws<ArgumentNullException>(() => info.GetPublicKey());
        }
    }
}
