// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Client's application configuration implementation
    /// </summary>
    public class TestClientServicesConfig : IClientServicesConfig, IDisposable {

        /// <summary>
        /// Pki root
        /// </summary>
        public const string Pki = "pki";

        /// <inheritdoc/>
        public string AppCertStoreType =>
             RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                "X509Store" : "Directory";

        /// <inheritdoc/>
        public string PkiRootPath { get; }
        /// <inheritdoc/>
        public string OwnCertPath => Path.Combine(PkiRootPath, "own");
        /// <inheritdoc/>
        public string TrustedCertPath => Path.Combine(PkiRootPath, "trusted");
        /// <inheritdoc/>
        public string IssuerCertPath => Path.Combine(PkiRootPath, "issuer");
        /// <inheritdoc/>
        public string RejectedCertPath => Path.Combine(PkiRootPath, "rejected");
        /// <inheritdoc/>
        public string OwnCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";
        /// <inheritdoc/>
        public bool AutoAcceptUntrustedCertificates { get; }
        /// <inheritdoc/>
        public TimeSpan? DefaultSessionTimeout => null;
        /// <inheritdoc/>
        public TimeSpan? OperationTimeout => null;

        /// <inheritdoc/>
        public TestClientServicesConfig(bool autoAccept = false) {
            AutoAcceptUntrustedCertificates = autoAccept;
            PkiRootPath = Path.Combine(Directory.GetCurrentDirectory(), Pki,
                Guid.NewGuid().ToByteArray().ToBase16String());
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (Directory.Exists(PkiRootPath)) {
                Try.Op(() => Directory.Delete(PkiRootPath, true));
            }
        }
    }
}
