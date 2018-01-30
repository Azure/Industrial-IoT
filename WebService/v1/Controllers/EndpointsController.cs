// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Controllers {
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Exceptions;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Auth;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Filters;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Models;
    using System;
    using System.Linq;
    using System.Net.Mime;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint controller
    /// </summary>
    [Route(ServiceInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policy.BrowseOpcServer)]
    public class EndpointsController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="endpointServices"></param>
        public EndpointsController(IOpcUaEndpointServices endpointServices) {
            _endpoints = endpointServices;
        }

        /// <summary>
        /// Returns the endpoint with the specified identifier.
        /// </summary>
        /// <param name="id">Endpoint identifier</param>
        /// <returns>Endpoint</returns>
        [HttpGet("{id}")]
        public async Task<ServerEndpointApiModel> GetAsync(string id) {
            var result = await _endpoints.GetAsync(id);

            // TODO: Redact username/token/certificates based on policy/permission

            return new ServerEndpointApiModel(result);
        }

        /// <summary>
        /// Get all registered endpoints in paged form.
        /// </summary>
        /// <returns>
        /// List of endpoints and continuation token to use for next request 
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        public async Task<ServerRegistrationListApiModel> ListAsync() {
            string continuationToken = null;
            if (Request.Headers.ContainsKey(CONTINUATION_TOKEN_NAME)) {
                continuationToken = Request.Headers[CONTINUATION_TOKEN_NAME].FirstOrDefault();
            }
            var result = await _endpoints.ListAsync(continuationToken);

            // TODO: Redact username/token/certificates based on policy/permission

            return new ServerRegistrationListApiModel(result);
        }

        /// <summary>
        /// Register new endpoint on the server. 
        /// </summary>
        /// <param name="request">Server registration request</param>
        /// <returns>Server registration response</returns>
        [HttpPost]
        [Authorize(Policy = Policy.AddOpcServer)]
        public async Task<ServerRegistrationResponseApiModel> RegisterAsync(
            [FromBody]ServerRegistrationRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // TODO: if token type is not "none", but user/token not, take from current claims

            var result = await _endpoints.RegisterAsync(request.ToServiceModel());
            return new ServerRegistrationResponseApiModel(result);
        }

        /// <summary>
        /// Update existing server endpoint. Note that Id field in request 
        /// must not be null and endpoint registration must exist.
        /// </summary>
        /// <param name="registration">Existing registration to patch</param>
        [HttpPatch]
        [Authorize(Policy = Policy.AddOpcServer)]
        public async Task PatchAsync(
            [FromBody] ServerRegistrationApiModel registration) {
            if (registration == null) {
                throw new ArgumentNullException(nameof(registration));
            }

            // TODO: permissions

            await _endpoints.PatchAsync(registration.ToServiceModel());
        }

        /// <summary>
        /// Delete endpoint by endpoint identifier.  
        /// </summary>
        /// <param name="id">The identifier of the endpoint</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = Policy.AddOpcServer)]
        public async Task DeleteAsync(string id) {

            // TODO: permissions

            await _endpoints.DeleteAsync(id);
        }

        /// <summary>
        /// Downloads a file with the public server certificate for the endpoint. 
        /// This allows a user to inspect the certificate to see if it can be trusted. 
        /// If the user trusts the certificate the endpoint can be patched with the
        /// IsTrusted property set to true.
        /// </summary>
        /// <param name="id">Endpoint identifier</param>
        /// <returns>Public certificate of the server</returns>
        [HttpGet("{id}/certificate/server")]
        [Authorize(Policy = Policy.AddOpcServer)]
        public async Task<ActionResult> GetServerCertificateAsync(string id) {

            // TODO: permissions

            var endpoint = await _endpoints.GetAsync(id);
            if (endpoint == null) {
                throw new ResourceNotFoundException($"Endpoint {id} not found.");
            }
            var certificate = endpoint.ServerCertificate;
            if (certificate == null) {
                throw new ResourceNotFoundException($"Certificate not available in {id}");
            }
            return File(certificate.GetRawCertData(),
                MediaTypeNames.Application.Octet, certificate.FriendlyName + ".der");
        }

        /// <summary>
        /// Downloads a file with the public client certificate for the endpoint.
        /// This certificate must be imported into the server trust list.
        /// </summary>
        /// <param name="id">Endpoint identifier</param>
        /// <returns>Public certificate of the client</returns>
        [HttpGet("{id}/certificate/client")]
        [Authorize(Policy = Policy.AddOpcServer)]
        public async Task<ActionResult> GetClientCertificateAsync(string id) {

            // TODO: permissions

            var endpoint = await _endpoints.GetAsync(id);
            if (endpoint == null) {
                throw new ResourceNotFoundException($"Endpoint {id} not found.");
            }
            var certificate = endpoint.ClientCertificate;
            if (certificate == null) {
                throw new ResourceNotFoundException($"Certificate not available in {id}");
            }
            return File(certificate.GetRawCertData(),
                MediaTypeNames.Application.Octet, certificate.FriendlyName + ".der");
        }

        private const string CONTINUATION_TOKEN_NAME = "x-ms-continuation";
        private readonly IOpcUaEndpointServices _endpoints;
    }
}