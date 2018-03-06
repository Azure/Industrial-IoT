// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Server registry
    /// </summary>
    public interface IOpcUaServerRegistry {

        /// <summary>
        /// List all servers.
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        Task<ServerInfoListModel> ListServerInfosAsync(
            string continuation);

        /// <summary>
        /// Read full server model for specified server
        /// which includes all endpoints.
        /// </summary>
        /// <param name="serverId"></param>
        /// <returns></returns>
        Task<ServerModel> GetServerAsync(string serverId);

        /// <summary>
        /// Find full server model for specified server
        /// information criterias. Application Uri is mandatory.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        Task<ServerModel> FindServerAsync(ServerInfoModel info);
    }
}