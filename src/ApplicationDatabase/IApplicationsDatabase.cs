// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        Task<QueryApplicationsResponseModel> QueryApplicationsAsync(
            uint startingRecordId,
            uint maxRecordsToReturn,
            string applicationName,
            string applicationUri,
            uint applicationType,
            string productUri,
            IList<string> serverCapabilities,
            bool? anyState
            );
        Task<QueryApplicationsPageResponseModel> QueryApplicationsPageAsync(
            string applicationName, 
            string applicationUri, 
            uint applicationType, 
            string productUri, 
            IList<string> serverCapabilities, 
            string nextPageLink, 
            int maxRecordsToReturn,
            bool? anyState);
    }
}
