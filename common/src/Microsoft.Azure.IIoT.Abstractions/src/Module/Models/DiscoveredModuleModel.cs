// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Models {

    /// <summary>
    /// Modules model as returned by module discovery
    /// </summary>
    public class DiscoveredModuleModel {

        /// <summary>
        /// Module Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Image name
        /// </summary>
        public string ImageName { get; set; }

        /// <summary>
        /// Image hash
        /// </summary>
        public string ImageHash { get; set; }

        /// <summary>
        /// Image version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public string Status { get; set; }
    }
}
