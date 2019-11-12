// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;

    public class SupervisorInfo {

        /// <summary>
        /// Supervisor models.
        /// </summary>
        public SupervisorApiModel SupervisorModel { get; set; }

        /// <summary>
        /// scan status.
        /// </summary>
        public bool ScanStatus { get; set; }

        /// <summary>
        /// is scan searching.
        /// </summary>
        public bool IsSearching { get; set; }

        /// <summary>
        /// Supervisor has application children.
        /// </summary>
        public bool HasApplication { get; set; }
    }
}
