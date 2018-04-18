// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin Browse services
    /// </summary>
    public interface IOpcUaTwinBrowseServices {

        /// <summary>
        /// Browse nodes on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BrowseResultModel> NodeBrowseAsync(string twinId,
            BrowseRequestModel request);
    }
}