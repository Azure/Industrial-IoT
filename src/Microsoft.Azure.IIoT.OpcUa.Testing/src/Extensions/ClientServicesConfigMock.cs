// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Mock {
    using System.Runtime.InteropServices;
    /// <summary>
    /// Client's application configuration implementation
    /// </summary>
    public class ClientServicesConfigMock : IClientServicesConfig {

        /// <inheritdoc/>
        public string AppCertStoreType =>
             RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "X509Store" : "Directory";

        /// <inheritdoc/>
        public string PkiRootPath => "pki";

        /// <inheritdoc/>
        public string OwnCertPath => PkiRootPath + "/own";

        /// <inheritdoc/>
        public string TrustedCertPath => PkiRootPath + "/trusted";

        /// <inheritdoc/>
        public string IssuerCertPath => PkiRootPath + "/issuer";

        /// <inheritdoc/>
        public string RejectedCertPath => PkiRootPath + "/rejected";

        /// <inheritdoc/>
        public string OwnCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";

        /// <inheritdoc/>
        public bool AutoAccept => true;
        
        /// <inheritdoc/>          
        public ClientServicesConfigMock() { }
    }
}
