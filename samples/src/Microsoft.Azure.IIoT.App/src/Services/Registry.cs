// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.App.Common;
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
        /// <param name="commonHelper"></param>
        public Registry(IRegistryServiceApi registryService, ILogger logger, UICommon commonHelper) {
            _registryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commonHelper = commonHelper ?? throw new ArgumentNullException(nameof(commonHelper));
        }

        /// <summary>
        /// GetEndpointListAsync
        /// </summary>
        /// <param name="discovererId"></param>
        /// <param name="previousPage"></param>
        /// <returns>EndpointInfoApiModel</returns>
        public async Task<PagedResult<EndpointInfo>> GetEndpointListAsync(
            string discovererId, string applicationId, string supervisorId, PagedResult<EndpointInfo> previousPage = null) {

            var pageResult = new PagedResult<EndpointInfo>();

            try {
                var endpoints = new EndpointInfoListApiModel();
                var query = new EndpointRegistrationQueryApiModel {
                    DiscovererId = discovererId == PathAll ? null : discovererId,
                    ApplicationId = applicationId == PathAll ? null : applicationId,
                    SupervisorId = supervisorId == PathAll ? null : supervisorId,
                    IncludeNotSeenSince = true
                };

                if (string.IsNullOrEmpty(previousPage?.ContinuationToken)) {
                    endpoints = await _registryService.QueryEndpointsAsync(query, null, _commonHelper.PageLength);
                    if (!string.IsNullOrEmpty(endpoints.ContinuationToken)) {
                        pageResult.PageCount = 2;
                    }
                }
                else {
                    endpoints = await _registryService.ListEndpointsAsync(previousPage.ContinuationToken, null, _commonHelper.PageLength);

                    if (string.IsNullOrEmpty(endpoints.ContinuationToken)) {
                        pageResult.PageCount = previousPage.PageCount;
                    }
                    else {
                        pageResult.PageCount = previousPage.PageCount + 1;
                    }
                }

                foreach (var ep in endpoints.Items) {
                    // Get non cached version of endpoint
                    var endpoint = await _registryService.GetEndpointAsync(ep.Registration.Id);
                    pageResult.Results.Add(new EndpointInfo {
                        EndpointModel = endpoint
                    });
                }
                if (previousPage != null) {
                    previousPage.Results.AddRange(pageResult.Results);
                    pageResult.Results = previousPage.Results;
                }

                pageResult.ContinuationToken = endpoints.ContinuationToken;
                pageResult.PageSize = _commonHelper.PageLength;
                pageResult.RowCount = pageResult.Results.Count;
            }
            catch (UnauthorizedAccessException) {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                var message = "Cannot get endpoint list";
                _logger.Warning(e, message);
                pageResult.Error = message;
            }
            return pageResult;
        }

        /// <summary>
        /// GetDiscovererListAsync
        /// </summary>
        /// <param name="previousPage"></param>
        /// <returns>DiscovererInfo</returns>
        public async Task<PagedResult<DiscovererInfo>> GetDiscovererListAsync(PagedResult<DiscovererInfo> previousPage =  null) {
            var pageResult = new PagedResult<DiscovererInfo>();

            try {
                var discovererModel = new DiscovererQueryApiModel();
                var applicationModel = new ApplicationRegistrationQueryApiModel();
                var discoverers = new DiscovererListApiModel();

                if (string.IsNullOrEmpty(previousPage?.ContinuationToken)) {
                    discoverers = await _registryService.QueryDiscoverersAsync(discovererModel, _commonHelper.PageLengthSmall);
                    if (!string.IsNullOrEmpty(discoverers.ContinuationToken)) {
                        pageResult.PageCount = 2;
                    }
                }
                else {
                    discoverers = await _registryService.ListDiscoverersAsync(previousPage.ContinuationToken, _commonHelper.PageLengthSmall);

                    if (string.IsNullOrEmpty(discoverers.ContinuationToken)) {
                        pageResult.PageCount = previousPage.PageCount;
                    }
                    else {
                        pageResult.PageCount = previousPage.PageCount + 1;
                    }
                }

                if (discoverers != null && discoverers.Items.Any()) {
                    foreach (var disc in discoverers.Items) {
                        var discoverer = await _registryService.GetDiscovererAsync(disc.Id);
                        var info = new DiscovererInfo {
                            DiscovererModel = discoverer,
                            HasApplication = false,
                            ScanStatus = (discoverer.Discovery == DiscoveryMode.Off) || (discoverer.Discovery == null) ? false : true
                        };
                        applicationModel.DiscovererId = discoverer.Id;
                        var applications = await _registryService.QueryApplicationsAsync(applicationModel, 1);
                        if (applications != null) {
                            info.HasApplication = true;
                        }
                        pageResult.Results.Add(info);
                    }
                    if (previousPage != null) {
                        previousPage.Results.AddRange(pageResult.Results);
                        pageResult.Results = previousPage.Results;
                    }

                    pageResult.ContinuationToken = discoverers.ContinuationToken;
                    pageResult.PageSize = _commonHelper.PageLengthSmall;
                    pageResult.RowCount = pageResult.Results.Count;
                }
                else {
                    pageResult.Error = "No Discoveres Found";
                }
            }
            catch (UnauthorizedAccessException) {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                var message = "Cannot get discoverers as list";
                _logger.Warning(e, message);
                pageResult.Error = message;
            }
            return pageResult;
        }

        /// <summary>
        /// GetApplicationListAsync
        /// </summary>
        /// <param name="previousPage"></param>
        /// <returns>ApplicationInfoApiModel</returns>
        public async Task<PagedResult<ApplicationInfoApiModel>> GetApplicationListAsync(PagedResult<ApplicationInfoApiModel> previousPage = null) {
            var pageResult = new PagedResult<ApplicationInfoApiModel>();

            try {
                var query = new ApplicationRegistrationQueryApiModel {
                    IncludeNotSeenSince = true
                };
                var applications = new ApplicationInfoListApiModel();

                if (string.IsNullOrEmpty(previousPage?.ContinuationToken)) {
                    applications = await _registryService.QueryApplicationsAsync(query, _commonHelper.PageLength);
                    if (!string.IsNullOrEmpty(applications.ContinuationToken)) {
                        pageResult.PageCount = 2;
                    }
                }
                else
                {
                    applications = await _registryService.ListApplicationsAsync(previousPage.ContinuationToken, _commonHelper.PageLength);

                    if (string.IsNullOrEmpty(applications.ContinuationToken)) {
                        pageResult.PageCount = previousPage.PageCount;
                    }
                    else {
                        pageResult.PageCount = previousPage.PageCount + 1;
                    }
                }

                if (applications != null) {
                    foreach (var app in applications.Items) {
                        var application = (await _registryService.GetApplicationAsync(app.ApplicationId)).Application;
                        pageResult.Results.Add(application);
                    }
                }
                if (previousPage != null) {
                    previousPage.Results.AddRange(pageResult.Results);
                    pageResult.Results = previousPage.Results;
                }

                pageResult.ContinuationToken = applications.ContinuationToken;
                pageResult.PageSize = _commonHelper.PageLength;
                pageResult.RowCount = pageResult.Results.Count;
            }
            catch (UnauthorizedAccessException) {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                var message = "Can not get applications list";
                _logger.Warning(e, message);
                pageResult.Error = message;
            }
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
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception exception) {
                _logger.Error(exception, "Failed to set discovery mode.");
                var errorMessageTrace = string.Concat(exception.Message,
                    exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
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
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception exception) {
                _logger.Error(exception, "Failed to update discoverer");
                var errorMessageTrace = string.Concat(exception.Message,
                    exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                return errorMessageTrace;
            }
            return null;
        }

        /// <summary>
        /// Discover servers
        /// </summary>
        /// <param name="discoverer"></param>
        /// <returns></returns>
        public async Task<string> DiscoverServersAsync(DiscovererInfo discoverer) {
            try {
                await _registryService.DiscoverAsync(
                    new DiscoveryRequestApiModel {
                        Id = discoverer.DiscoveryRequestId,
                        Discovery = DiscoveryMode.Fast,
                        Configuration = discoverer.Patch
                    });
                discoverer.Patch = new DiscoveryConfigApiModel();
            }
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception exception) {
                _logger.Error(exception, "Failed to discoverer servers.");
                var errorMessageTrace = string.Concat(exception.Message,
                    exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                return errorMessageTrace;
            }
            return null;
        }

        /// <summary>
        /// GetGatewayListAsync
        /// </summary>
        /// <param name="previousPage"></param>
        /// <returns>GatewayApiModel</returns>
        public async Task<PagedResult<GatewayApiModel>> GetGatewayListAsync(PagedResult<GatewayApiModel> previousPage = null) {
            var pageResult = new PagedResult<GatewayApiModel>();

            try {
                var gatewayModel = new GatewayQueryApiModel();
                var gateways = new GatewayListApiModel();

                if (string.IsNullOrEmpty(previousPage?.ContinuationToken)) {
                    gateways = await _registryService.QueryGatewaysAsync(gatewayModel, _commonHelper.PageLength);
                    if (!string.IsNullOrEmpty(gateways.ContinuationToken)) {
                        pageResult.PageCount = 2;
                    }
                }
                else {
                    gateways = await _registryService.ListGatewaysAsync(previousPage.ContinuationToken, _commonHelper.PageLength);

                    if (string.IsNullOrEmpty(gateways.ContinuationToken)) {
                        pageResult.PageCount = previousPage.PageCount;
                    }
                    else {
                        pageResult.PageCount = previousPage.PageCount + 1;
                    }
                }

                if (gateways != null) {
                    foreach (var gw in gateways.Items) {
                        var gateway = (await _registryService.GetGatewayAsync(gw.Id)).Gateway;
                        pageResult.Results.Add(gateway);
                    }
                }
                if (previousPage != null) {
                    previousPage.Results.AddRange(pageResult.Results);
                    pageResult.Results = previousPage.Results;
                }

                pageResult.ContinuationToken = gateways.ContinuationToken;
                pageResult.PageSize = _commonHelper.PageLength;
                pageResult.RowCount = pageResult.Results.Count;
            }
            catch (UnauthorizedAccessException) {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                var message = "Cannot get gateways list";
                _logger.Warning(e, message);
                pageResult.Error = message;
            }
            return pageResult;
        }

        /// <summary>
        /// GetPublisherListAsync
        /// </summary>
        /// <param name="previousPage"></param>
        /// <returns>PublisherApiModel</returns>
        public async Task<PagedResult<PublisherApiModel>> GetPublisherListAsync(PagedResult<PublisherApiModel> previousPage = null) {
            var pageResult = new PagedResult<PublisherApiModel>();

            try {
                var publisherModel = new PublisherQueryApiModel();
                var publishers = new PublisherListApiModel();

                if (string.IsNullOrEmpty(previousPage?.ContinuationToken)) {
                    publishers = await _registryService.QueryPublishersAsync(publisherModel, null, _commonHelper.PageLengthSmall);
                    if (!string.IsNullOrEmpty(publishers.ContinuationToken)) {
                        pageResult.PageCount = 2;
                    }
                }
                else {
                    publishers = await _registryService.ListPublishersAsync(previousPage.ContinuationToken, null, _commonHelper.PageLengthSmall);

                    if (string.IsNullOrEmpty(publishers.ContinuationToken)) {
                        pageResult.PageCount = previousPage.PageCount;
                    }
                    else {
                        pageResult.PageCount = previousPage.PageCount + 1;
                    }
                }

                if (publishers != null) {
                    foreach (var pub in publishers.Items) {
                        var publisher = await _registryService.GetPublisherAsync(pub.Id);
                        pageResult.Results.Add(publisher);
                    }
                }
                if (previousPage != null) {
                    previousPage.Results.AddRange(pageResult.Results);
                    pageResult.Results = previousPage.Results;
                }

                pageResult.ContinuationToken = publishers.ContinuationToken;
                pageResult.PageSize = _commonHelper.PageLengthSmall;
                pageResult.RowCount = pageResult.Results.Count;
            }
            catch (UnauthorizedAccessException) {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                var message = "Cannot get publisher list";
                _logger.Warning(e, message);
                pageResult.Error = message;
            }
            return pageResult;
        }

        /// <summary>
        /// Update publisher
        /// </summary>
        /// <param name="discoverer"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public async Task<string> UpdatePublisherAsync(PublisherInfo publisher) {
            try {
                await _registryService.UpdatePublisherAsync(publisher.PublisherModel.Id, new PublisherUpdateApiModel {
                    Configuration = publisher.PublisherModel.Configuration
                });
            }
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception exception) {
                _logger.Error(exception, "Failed to update publisher");
                var errorMessageTrace = string.Concat(exception.Message,
                    exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                return errorMessageTrace;
            }
            return null;
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
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception exception) {
                _logger.Error(exception, "Failed to unregister application");
                var errorMessageTrace = string.Concat(exception.Message, exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                return errorMessageTrace;
            }
            return null;
        }

        /// <summary>
        /// GetSupervisorListAsync
        /// </summary>
        /// <param name="previousPage"></param>
        /// <returns>SupervisorApiModel</returns>
        public async Task<PagedResult<SupervisorApiModel>> GetSupervisorListAsync(PagedResult<SupervisorApiModel> previousPage = null) {

            var pageResult = new PagedResult<SupervisorApiModel>();

            try {
                var model = new SupervisorQueryApiModel();
                var supervisors = new SupervisorListApiModel();

                if (string.IsNullOrEmpty(previousPage?.ContinuationToken)) {
                    supervisors = await _registryService.QuerySupervisorsAsync(model, null, _commonHelper.PageLength);
                    if (!string.IsNullOrEmpty(supervisors.ContinuationToken)) {
                        pageResult.PageCount = 2;
                    }
                }
                else {
                    supervisors = await _registryService.ListSupervisorsAsync(previousPage.ContinuationToken, null, _commonHelper.PageLengthSmall);

                    if (string.IsNullOrEmpty(supervisors.ContinuationToken)) {
                        pageResult.PageCount = previousPage.PageCount;
                    }
                    else {
                        pageResult.PageCount = previousPage.PageCount + 1;
                    }
                }

                if (supervisors != null) {
                    foreach (var sup in supervisors.Items) {
                        var supervisor = await _registryService.GetSupervisorAsync(sup.Id);
                        pageResult.Results.Add(supervisor);
                    }
                }
                if (previousPage != null) {
                    previousPage.Results.AddRange(pageResult.Results);
                    pageResult.Results = previousPage.Results;
                }

                pageResult.ContinuationToken = supervisors.ContinuationToken;
                pageResult.PageSize = _commonHelper.PageLength;
                pageResult.RowCount = pageResult.Results.Count;
            }
            catch (UnauthorizedAccessException) {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                var message = "Cannot get supervisor list";
                _logger.Warning(e, message);
                pageResult.Error = message;
            }
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
                _logger.Error(exception, "Failed to get status");
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
                _logger.Error(exception, "Failed to reset supervisor");
                return exception.Message;
            }
        }

        private readonly IRegistryServiceApi _registryService;
        private readonly ILogger _logger;
        private readonly UICommon _commonHelper;
        public string PathAll = "All";
    }
}
