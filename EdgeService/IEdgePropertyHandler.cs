// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService {
    using Microsoft.Azure.Devices.Shared;
    using System.Threading.Tasks;

    /// <summary>
    /// Route to the right service
    /// </summary>
    public interface IEdgePropertyHandler {

        /// <summary>
        /// Process desired and return reported
        /// </summary>
        /// <param name="desired"></param>
        /// <returns></returns>
        Task<TwinCollection> ProcessSettings(TwinCollection desired);
    }
}