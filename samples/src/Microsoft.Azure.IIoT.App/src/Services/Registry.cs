// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Serilog;

    public class Registry {

        /// <summary>
        /// Create registry
        /// </summary>
        /// <param name="registryService"></param>
        /// <param name="logger"></param>
        public Registry(IRegistryServiceApi registryService, ILogger logger) {
            _registryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GetEndpointListAsync
        /// </summary>
        /// <param name="discovererId"></param>
        /// <returns>EndpointInfoApiModel</returns>
        public async Task<PagedResult<EndpointInfo>> GetEndpointListAsync(
            string discovererId, string applicationId, string supervisorId) {

            var pageResult = new PagedResult<EndpointInfo>();

            try {
                var model = new EndpointRegistrationQueryApiModel();
                model.DiscovererId = discovererId == PathAll ? null : discovererId;
                model.ApplicationId = applicationId == PathAll ? null : applicationId;
                model.SupervisorId = supervisorId == PathAll ? null : supervisorId;

                var endpoints = await _registryService.QueryAllEndpointsAsync(model);
                foreach (var ep in endpoints) {
                    // Get non cached version of endpoint
                    var endpoint = ep; // await _registryService.GetEndpointAsync(ep.Registration.Id);
                    pageResult.Results.Add(new EndpointInfo {
                        EndpointModel = endpoint
                    });
                }
            }
            catch (Exception e) {
                _logger.Warning("Can not get endpoint list");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                _logger.Warning(errorMessage);
                pageResult.Error = e.Message;
            }

            pageResult.PageSize = 10;
            pageResult.RowCount = pageResult.Results.Count;
            pageResult.PageCount = (int)Math.Ceiling((decimal)pageResult.RowCount / 10);
            return pageResult;
        }

        /// <summary>
        /// GetDiscovererListAsync
        /// </summary>
        /// <returns>DiscovererInfo</returns>
        public async Task<PagedResult<DiscovererInfo>> GetDiscovererListAsync() {
            var pageResult = new PagedResult<DiscovererInfo>();

            try {
                var discovererModel = new DiscovererQueryApiModel();
                var applicationModel = new ApplicationRegistrationQueryApiModel();
                var discoverers = await _registryService.QueryAllDiscoverersAsync(discovererModel);

                if (discoverers != null && discoverers.Any()) {
                    foreach (var disc in discoverers) {
                        var discoverer = disc; //  await _registryService.GetDiscovererAsync(disc.Id);
                        var info = new DiscovererInfo {
                            DiscovererModel = discoverer,
                            HasApplication = false,
                            ScanStatus = (discoverer.Discovery == DiscoveryMode.Off) || (discoverer.Discovery == null) ? false : true
                        };
                        applicationModel.DiscovererId = discoverer.Id;
                        var applications = await _registryService.QueryApplicationsAsync(applicationModel);
                        if (applications != null) {
                            info.HasApplication = true;
                        }
                        pageResult.Results.Add(info);
                    }
                }
                else {
                    pageResult.Error = "No Discoveres Found";
                }
            }
            catch (Exception e) {
                _logger.Warning("Can not get discoverers as list");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                _logger.Warning(errorMessage);
                pageResult.Error = e.Message;
            }

            pageResult.PageSize = 10;
            pageResult.RowCount = pageResult.Results.Count;
            pageResult.PageCount = (int)Math.Ceiling((decimal)pageResult.RowCount / 10);
            return pageResult;
        }

        /// <summary>
        /// GetApplicationListAsync
        /// </summary>
        /// <returns>ApplicationInfoApiModel</returns>
        public async Task<PagedResult<ApplicationInfoApiModel>> GetApplicationListAsync() {
            var pageResult = new PagedResult<ApplicationInfoApiModel>();

            try {
                var applicationModel = new ApplicationRegistrationQueryApiModel();
                var applications = await _registryService.QueryAllApplicationsAsync(applicationModel);

                if (applications != null) {
                    foreach (var app in applications) {
                        var application = app; // (await _registryService.GetApplicationAsync(app.ApplicationId)).Application;
                        pageResult.Results.Add(app);
                    }
                }
            }
            catch (Exception e) {
                _logger.Warning("Can not get applications list");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                _logger.Warning(errorMessage);
                pageResult.Error = e.Message;
            }

            pageResult.PageSize = 10;
            pageResult.RowCount = pageResult.Results.Count;
            pageResult.PageCount = (int)Math.Ceiling((decimal)pageResult.RowCount / 10);
            return pageResult;
        }

        /// <summary>
        /// SetScanAsync
        /// </summary>
        /// <param name="discoverer"></param>
        /// <returns></returns>
        public async Task<string> SetDiscoveryAsync(DiscovererInfo discoverer) {
            try {
                var discoveryMode = discoverer.ScanStatus ? DiscoveryMode.Fast : DiscoveryMode.Off;
                await _registryService.SetDiscoveryModeAsync(discoverer.DiscovererModel.Id, discoveryMode, discoverer.Patch);
                discoverer.Patch = new DiscoveryConfigApiModel();
            }
            catch (Exception exception) {
                var errorMessageTrace = string.Concat(exception.Message,
                    exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                _logger.Error(errorMessageTrace);
                return errorMessageTrace;
            }
            return null;
        }

        /// <summary>
        /// UpdateDiscovererAsync
        /// </summary>
        /// <param name="discoverer"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public async Task<string> UpdateDiscovererAsync(DiscovererInfo discoverer) {
            try {
                await _registryService.UpdateDiscovererAsync(discoverer.DiscovererModel.Id, new DiscovererUpdateApiModel {
                    DiscoveryConfig = discoverer.Patch
                });
                discoverer.Patch = new DiscoveryConfigApiModel();
            }
            catch (Exception exception) {
                var errorMessageTrace = string.Concat(exception.Message,
                    exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                _logger.Error(errorMessageTrace);
                return errorMessageTrace;
            }
            return null;
        }

        /// <summary>
        /// GetGatewayListAsync
        /// </summary>
        /// <returns>GatewayApiModel</returns>
        public async Task<PagedResult<GatewayApiModel>> GetGatewayListAsync() {
            var pageResult = new PagedResult<GatewayApiModel>();

            try {
                var gatewayModel = new GatewayQueryApiModel();
                var gateways = await _registryService.QueryAllGatewaysAsync(gatewayModel);

                if (gateways != null) {
                    foreach (var gw in gateways) {
                        var gateway = gw; // (await _registryService.GetGatewayAsync(gw.Id)).Gateway;
                        pageResult.Results.Add(gateway);
                    }
                }
            }
            catch (Exception e) {
                _logger.Warning("Can not get gateways list");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                _logger.Warning(errorMessage);
                pageResult.Error = e.Message;
            }

            pageResult.PageSize = 10;
            pageResult.RowCount = pageResult.Results.Count;
            pageResult.PageCount = (int)Math.Ceiling((decimal)pageResult.RowCount / 10);
            return pageResult;
        }

        /// <summary>
        /// GetPublisherListAsync
        /// </summary>
        /// <returns>PublisherApiModel</returns>
        public async Task<PagedResult<PublisherApiModel>> GetPublisherListAsync() {
            var pageResult = new PagedResult<PublisherApiModel>();

            try {
                var publisherModel = new PublisherQueryApiModel();
                var publishers = await _registryService.QueryAllPublishersAsync(publisherModel);

                if (publishers != null) {
                    foreach (var pub in publishers) {
                        var publisher = pub; // await _registryService.GetPublisherAsync(pub.Id);
                        pageResult.Results.Add(publisher);
                    }
                }
            }
            catch (Exception e) {
                _logger.Warning("Can not get publisher list");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                _logger.Warning(errorMessage);
                pageResult.Error = e.Message;
            }

            pageResult.PageSize = 10;
            pageResult.RowCount = pageResult.Results.Count;
            pageResult.PageCount = (int)Math.Ceiling((decimal)pageResult.RowCount / 10);
            return pageResult;
        }

        /// <summary>
        /// Unregister application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        public async Task<string> UnregisterApplicationAsync(string applicationId) {

            try {
                await _registryService.UnregisterApplicationAsync(applicationId);
            }
            catch (Exception exception) {
                var errorMessageTrace = string.Concat(exception.Message, exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                _logger.Error(errorMessageTrace);
                return errorMessageTrace;
            }
            return null;
        }

        /// <summary>
        /// GetSupervisorListAsync
        /// </summary>
        /// <returns>SupervisorApiModel</returns>
        public async Task<PagedResult<SupervisorApiModel>> GetSupervisorListAsync() {

            var pageResult = new PagedResult<SupervisorApiModel>();

            try {
                var model = new SupervisorQueryApiModel();

                var supervisors = await _registryService.QueryAllSupervisorsAsync(model);
                if (supervisors != null) {
                    foreach (var sup in supervisors) {
                        var supervisor = sup; // await _registryService.GetSupervisorAsync(sup.Id);
                        pageResult.Results.Add(supervisor);
                    }
                }
            }
            catch (Exception e) {
                _logger.Warning("Can not get supervisor list");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                _logger.Warning(errorMessage);
                pageResult.Error = e.Message;
            }

            pageResult.PageSize = 10;
            pageResult.RowCount = pageResult.Results.Count;
            pageResult.PageCount = (int)Math.Ceiling((decimal)pageResult.RowCount / 10);
            return pageResult;
        }

        /// <summary>
        /// GetSupervisorStatusAsync
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <returns>SupervisorStatusApiModel</returns>
        public async Task<SupervisorStatusApiModel> GetSupervisorStatusAsync(string supervisorId) {
            var supervisorStatus = new SupervisorStatusApiModel();

            try {
                supervisorStatus = await _registryService.GetSupervisorStatusAsync(supervisorId);
            }
            catch (Exception exception) {
                var errorMessageTrace = string.Concat(exception.Message, exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                _logger.Error(errorMessageTrace);
            }

            return supervisorStatus;
        }

        /// <summary>
        /// ResetSupervisorAsync
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <returns>bool</returns>
        public async Task<string> ResetSupervisorAsync(string supervisorId) {
            var supervisorStatus = new SupervisorStatusApiModel();

            try {
                await _registryService.ResetSupervisorAsync(supervisorId);
                return string.Empty;
            }
            catch (Exception exception) {
                var errorMessageTrace = string.Concat(exception.Message, exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                _logger.Error(errorMessageTrace);
                return exception.Message;
            }
        }

        private readonly IRegistryServiceApi _registryService;
        private readonly ILogger _logger;
        public string PathAll = "All";
    }
}
