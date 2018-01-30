// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Client {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents internal publisher functionality, i.e. to get the
    /// published nodes from the publisher to augment browse results.
    /// </summary>
    public interface IOpcUaPublisher {

        /// <summary>
        /// Get all published node ids for endpoint - provides for inter
        /// service communication between opc node services and publisher
        /// to enable filtering and tagging of published nodes.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task<IEnumerable<string>> GetPublishedNodeIds(
            ServerEndpointModel endpoint);
    }
}