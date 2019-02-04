// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Types;


namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault
{

    public interface IApplicationsDatabase
    {
        Task Initialize();
        Task<Application> RegisterApplicationAsync(Application application);
        Task<Application> GetApplicationAsync(string id);
        Task<Application> UpdateApplicationAsync(string id, Application application);
        Task<Application> ApproveApplicationAsync(string id, bool approved, bool force);
        Task<Application> UnregisterApplicationAsync(string id);
        Task DeleteApplicationAsync(string id, bool force);
        Task<IList<Application>> ListApplicationAsync(string uri);
        Task<QueryApplicationsByIdResponseModel> QueryApplicationsByIdAsync(
            uint startingRecordId,
            uint maxRecordsToReturn,
            string applicationName,
            string applicationUri,
            uint applicationType,
            string productUri,
            IList<string> serverCapabilities,
            QueryApplicationState? applicationState = null
            );
        Task<QueryApplicationsResponseModel> QueryApplicationsAsync(
            string applicationName,
            string applicationUri,
            uint applicationType,
            string productUri,
            IList<string> serverCapabilities,
            QueryApplicationState? applicationState = null,
            string nextPageLink = null,
            int? pageSize = null);
    }
}
