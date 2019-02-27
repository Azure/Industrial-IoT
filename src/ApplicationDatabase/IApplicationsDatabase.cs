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
    /// <summary>
    /// Application Database interface.
    /// </summary>
    public interface IApplicationsDatabase
    {
        /// <summary>
        /// Performs setup tasks. Used to create the database if it doesn't exist.
        /// </summary>
        Task Initialize();
        /// <summary>
        /// Register a new application.
        /// If the applicationId is not empty an Update is performed.
        /// </summary>
        /// <param name="application">The application record</param>
        Task<Application> RegisterApplicationAsync(Application application);
        /// <summary>
        /// Get the application by applicationId
        /// </summary>
        /// <param name="id">The applicationId</param>
        /// <returns>The application</returns>
        Task<Application> GetApplicationAsync(string id);
        /// <summary>
        /// Update an application.
        /// </summary>
        /// <param name="id">The applicationId</param>
        /// <param name="application">The application</param>
        /// <returns>The updated application</returns>
        Task<Application> UpdateApplicationAsync(string id, Application application);
        /// <summary>
        /// Approve or reject a new application.
        /// Application is in approved or rejected state after this call.
        /// </summary>
        /// <param name="id">The applicationId</param>
        /// <param name="approved">true if approved, false if rejected</param>
        /// <param name="force">Ignore state check</param>
        Task<Application> ApproveApplicationAsync(string id, bool approved, bool force);
        /// <summary>
        /// Unregister an application.
        /// After unregistering, the application is in deleted state but is
        /// not yet physically deleted, to maintain the history.
        /// All approved or accepted certificate requests of the application
        /// are also set to deleted state.
        /// The function is called Unregister instead of Delete to avoid confusion with
        /// a similar OPC UA GDS server function.
        /// </summary>
        /// <param name="id">The application Id</param>
        Task<Application> UnregisterApplicationAsync(string id);
        /// <summary>
        /// Physically remove the application form the database. Must be in deleted state.
        /// </summary>
        /// <param name="id">The applicationId</param>
        /// <param name="force">Force the application to be deleted, even when not in deleted state</param>
        /// <returns></returns>
        Task DeleteApplicationAsync(string id, bool force);
        /// <summary>
        /// List all applications with a ApplicationUri.
        /// </summary>
        /// <param name="uri">The ApplicationUri</param>
        /// <returns>The applications</returns>
        Task<IList<Application>> ListApplicationAsync(string uri);
        /// <summary>
        /// Query for Applications sorted by ID.
        /// This query implements the search parameters required for the
        /// OPC UA GDS server QueryServers/QueryApplications API.
        /// </summary>
        /// <param name="startingRecordId">the id of the first record to return</param>
        /// <param name="maxRecordsToReturn">max number of records the query should return</param>
        /// <param name="applicationName">Search string for Application Name</param>
        /// <param name="applicationUri">Search string for Application Uri</param>
        /// <param name="applicationType">Search flags for Application Type</param>
        /// <param name="productUri">Search string for Product Uri</param>
        /// <param name="serverCapabilities">Search array for server caps</param>
        /// <param name="applicationState">Filter for a application state. Default: Approved</param>
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
        /// <summary>
        /// Pageable query for applications with various search parameters.
        /// </summary>
        /// <param name="applicationName">Search string for Application Name</param>
        /// <param name="applicationUri">Search string for Application Uri</param>
        /// <param name="applicationType">Search flags for Application Type</param>
        /// <param name="productUri">Search string for Product Uri</param>
        /// <param name="serverCapabilities">Search array for server caps</param>
        /// <param name="applicationState">Filter for a application state. Default: Approved</param>
        /// <param name="nextPageLink">Next page link string</param>
        /// <param name="pageSize">Max number of applications to return</param>
        /// <returns></returns>
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
