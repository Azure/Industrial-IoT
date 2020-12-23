// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {

    /// <summary>
    /// Configuration version
    /// </summary>
    public class ConfigurationVersionModel {

        /// <summary>
        /// Major version
        /// </summary>
        public uint MajorVersion { get; set; }

        /// <summary>
        /// Minor version
        /// </summary>
        public uint MinorVersion { get; set; }
    }
}
