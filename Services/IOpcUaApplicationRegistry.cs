// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Application registry
    /// </summary>
    public interface IOpcUaApplicationRegistry {

        /// <summary>
        /// Register application from discovery url.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationResultModel> RegisterAsync(
            ServerRegistrationRequestModel request);

        /// <summary>
        /// Register application using the application info as
        /// template.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationResultModel> RegisterAsync(
            ApplicationRegistrationRequestModel request);

        /// <summary>
        /// Read full application model for specified 
        /// application (server/client) which includes all 
        /// endpoints if there are any.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId);

        /// <summary>
        /// Update an existing application, e.g. server
        /// certificate, or additional capabilities.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task UpdateApplicationAsync(
            ApplicationRegistrationUpdateModel request);

        /// <summary>
        /// List all applications or continue find query.
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        Task<ApplicationInfoListModel> ListApplicationsAsync(
            string continuation);

        /// <summary>
        /// Find applications for the specified information 
        /// criterias.  The returned continuation if any must 
        /// be passed to ListApplicationsAsync.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel query);

        /// <summary>
        /// Unregister application and all associated endpoints.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        Task UnregisterApplicationAsync(string applicationId);
    }
}