// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module {

    /// <summary>
    /// Module or device identity
    /// </summary>
    public interface IIdentity {

        /// <summary>
        /// Device id
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// Module id
        /// </summary>
        string ModuleId { get; }

        /// <summary>
        /// Site events
        /// </summary>
        string SiteId { get; }

        /// <summary>
        /// Module's host (gateway) hostname
        /// </summary>
        string Gateway { get; }
    }
}
