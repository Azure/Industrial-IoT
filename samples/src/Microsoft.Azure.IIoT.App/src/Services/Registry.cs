// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services
{
    using global::Azure.IIoT.OpcUa.Services.Sdk;
    using global::Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class Registry
    {
        /// <summary>
        /// Create registry
        /// </summary>
        /// <param name="registryService"></param>
        /// <param name="logger"></param>
        /// <param name="commonHelper"></param>
        public Registry(IRegistryServiceApi registryService, ILogger logger, UICommon commonHelper)
        {
            _registryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commonHelper = commonHelper ?? throw new ArgumentNullException(nameof(commonHelper));
        }

        /// <summary>
        /// GetEndpointListAsync
        /// </summary>
        /// <param name="discovererId"></param>
        /// <param name="previousPage"></param>
        /// <returns>EndpointInfoModel</returns>
        public async Task<PagedResult<EndpointInfo>> GetEndpointListAsync(
            string discovererId, string applicationId, string supervisorId, PagedResult<EndpointInfo> previousPage = null)
        {
            var pageResult = new PagedResult<EndpointInfo>();

            try
            {
                var endpoints = new EndpointInfoListModel();
                var query = new EndpointRegistrationQueryModel
                {
                    DiscovererId = discovererId == PathAll ? null : discovererId,
                    ApplicationId = applicationId == PathAll ? null : applicationId,
                    IncludeNotSeenSince = true
                };

                if (string.IsNullOrEmpty(previousPage?.ContinuationToken))
                {
                    endpoints = await _registryService.QueryEndpointsAsync(query, null, _commonHelper.PageLength).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(endpoints.ContinuationToken))
                    {
                        pageResult.PageCount = 2;
                    }
                }
                else
                {
                    endpoints = await _registryService.ListEndpointsAsync(previousPage.ContinuationToken, null, _commonHelper.PageLength).ConfigureAwait(false);

                    pageResult.PageCount = string.IsNullOrEmpty(endpoints.ContinuationToken) ? previousPage.PageCount : previousPage.PageCount + 1;
                }

                foreach (var ep in endpoints.Items)
                {
                    // Get non cached version of endpoint
                    var endpoint = await _registryService.GetEndpointAsync(ep.Registration.Id).ConfigureAwait(false);
                    pageResult.Results.Add(new EndpointInfo
                    {
                        EndpointModel = endpoint
                    });
                }
                if (previousPage != null)
                {
                    previousPage.Results.AddRange(pageResult.Results);
                    pageResult.Results = previousPage.Results;
                }

                pageResult.ContinuationToken = endpoints.ContinuationToken;
                pageResult.PageSize = _commonHelper.PageLength;
                pageResult.RowCount = pageResult.Results.Count;
            }
            catch (UnauthorizedAccessException)
            {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e)
            {
                const string message = "Cannot get endpoint list";
                _logger.LogWarning(e, message);
                pageResult.Error = message;
            }
            return pageResult;
        }

        /// <summary>
        /// GetDiscovererListAsync
        /// </summary>
        /// <param name="previousPage"></param>
        /// <returns>DiscovererInfo</returns>
        public async Task<PagedResult<DiscovererInfo>> GetDiscovererListAsync(PagedResult<DiscovererInfo> previousPage = null)
        {
            var pageResult = new PagedResult<DiscovererInfo>();

            try
            {
                var discovererModel = new DiscovererQueryModel();
                var applicationModel = new ApplicationRegistrationQueryModel();
                var discoverers = new DiscovererListModel();

                if (string.IsNullOrEmpty(previousPage?.ContinuationToken))
                {
                    discoverers = await _registryService.QueryDiscoverersAsync(discovererModel, _commonHelper.PageLengthSmall).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(discoverers.ContinuationToken))
                    {
                        pageResult.PageCount = 2;
                    }
                }
                else
                {
                    discoverers = await _registryService.ListDiscoverersAsync(previousPage.ContinuationToken, _commonHelper.PageLengthSmall).ConfigureAwait(false);

                    pageResult.PageCount = string.IsNullOrEmpty(discoverers.ContinuationToken) ? previousPage.PageCount : previousPage.PageCount + 1;
                }

                if (discoverers?.Items.Count > 0)
                {
                    foreach (var disc in discoverers.Items)
                    {
                        var discoverer = await _registryService.GetDiscovererAsync(disc.Id).ConfigureAwait(false);
                        var info = new DiscovererInfo
                        {
                            DiscovererModel = discoverer,
                            HasApplication = false,
                            ScanStatus = discoverer.Discovery is not DiscoveryMode.Off and not null
                        };
                        applicationModel.DiscovererId = discoverer.Id;
                        var applications = await _registryService.QueryApplicationsAsync(applicationModel, 1).ConfigureAwait(false);
                        if (applications != null)
                        {
                            info.HasApplication = true;
                        }
                        pageResult.Results.Add(info);
                    }
                    if (previousPage != null)
                    {
                        previousPage.Results.AddRange(pageResult.Results);
                        pageResult.Results = previousPage.Results;
                    }

                    pageResult.ContinuationToken = discoverers.ContinuationToken;
                    pageResult.PageSize = _commonHelper.PageLengthSmall;
                    pageResult.RowCount = pageResult.Results.Count;
                }
                else
                {
                    pageResult.Error = "No Discoveres Found";
                }
            }
            catch (UnauthorizedAccessException)
            {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e)
            {
                const string message = "Cannot get discoverers as list";
                _logger.LogWarning(e, message);
                pageResult.Error = message;
            }
            return pageResult;
        }

        /// <summary>
        /// GetApplicationListAsync
        /// </summary>
        /// <param name="previousPage"></param>
        /// <returns>ApplicationInfoModel</returns>
        public async Task<PagedResult<ApplicationInfoModel>> GetApplicationListAsync(PagedResult<ApplicationInfoModel> previousPage = null)
        {
            var pageResult = new PagedResult<ApplicationInfoModel>();

            try
            {
                var query = new ApplicationRegistrationQueryModel
                {
                    IncludeNotSeenSince = true
                };
                var applications = new ApplicationInfoListModel();

                if (string.IsNullOrEmpty(previousPage?.ContinuationToken))
                {
                    applications = await _registryService.QueryApplicationsAsync(query, _commonHelper.PageLength).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(applications.ContinuationToken))
                    {
                        pageResult.PageCount = 2;
                    }
                }
                else
                {
                    applications = await _registryService.ListApplicationsAsync(previousPage.ContinuationToken, _commonHelper.PageLength).ConfigureAwait(false);

                    pageResult.PageCount = string.IsNullOrEmpty(applications.ContinuationToken) ? previousPage.PageCount : previousPage.PageCount + 1;
                }

                if (applications != null)
                {
                    foreach (var app in applications.Items)
                    {
                        var application = (await _registryService.GetApplicationAsync(app.ApplicationId).ConfigureAwait(false)).Application;
                        pageResult.Results.Add(application);
                    }
                }
                if (previousPage != null)
                {
                    previousPage.Results.AddRange(pageResult.Results);
                    pageResult.Results = previousPage.Results;
                }

                pageResult.ContinuationToken = applications.ContinuationToken;
                pageResult.PageSize = _commonHelper.PageLength;
                pageResult.RowCount = pageResult.Results.Count;
            }
            catch (UnauthorizedAccessException)
            {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e)
            {
                const string message = "Can not get applications list";
                _logger.LogWarning(e, message);
                pageResult.Error = message;
            }
            return pageResult;
        }

        /// <summary>
        /// SetScanAsync
        /// </summary>
        /// <param name="discoverer"></param>
        /// <returns></returns>
        public async Task<string> SetDiscoveryAsync(DiscovererInfo discoverer)
        {
            try
            {
                var discoveryMode = discoverer.ScanStatus ? DiscoveryMode.Fast : DiscoveryMode.Off;
                await _registryService.SetDiscoveryModeAsync(discoverer.DiscovererModel.Id, discoveryMode, discoverer.Patch).ConfigureAwait(false);
                discoverer.Patch = new DiscoveryConfigModel();
            }
            catch (UnauthorizedAccessException)
            {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to set discovery mode.");
                return string.Concat(exception.Message,
                    exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
            }
            return null;
        }

        /// <summary>
        /// UpdateDiscovererAsync
        /// </summary>
        /// <param name="discoverer"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public async Task<string> UpdateDiscovererAsync(DiscovererInfo discoverer)
        {
            try
            {
                await _registryService.UpdateDiscovererAsync(discoverer.DiscovererModel.Id, new DiscovererUpdateModel
                {
                    DiscoveryConfig = discoverer.Patch
                }).ConfigureAwait(false);
                discoverer.Patch = new DiscoveryConfigModel();
            }
            catch (UnauthorizedAccessException)
            {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to update discoverer");
                return string.Concat(exception.Message,
                    exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
            }
            return null;
        }

        /// <summary>
        /// Discover servers
        /// </summary>
        /// <param name="discoverer"></param>
        /// <returns></returns>
        public async Task<string> DiscoverServersAsync(DiscovererInfo discoverer)
        {
            try
            {
                await _registryService.DiscoverAsync(
                    new DiscoveryRequestModel
                    {
                        Id = discoverer.DiscoveryRequestId,
                        Discovery = DiscoveryMode.Fast,
                        Configuration = discoverer.Patch
                    }).ConfigureAwait(false);
                discoverer.Patch = new DiscoveryConfigModel();
            }
            catch (UnauthorizedAccessException)
            {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to discoverer servers.");
                return string.Concat(exception.Message,
                    exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
            }
            return null;
        }

        /// <summary>
        /// GetGatewayListAsync
        /// </summary>
        /// <param name="previousPage"></param>
        /// <returns>GatewayModel</returns>
        public async Task<PagedResult<GatewayModel>> GetGatewayListAsync(PagedResult<GatewayModel> previousPage = null)
        {
            var pageResult = new PagedResult<GatewayModel>();

            try
            {
                var gatewayModel = new GatewayQueryModel();
                var gateways = new GatewayListModel();

                if (string.IsNullOrEmpty(previousPage?.ContinuationToken))
                {
                    gateways = await _registryService.QueryGatewaysAsync(gatewayModel, _commonHelper.PageLength).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(gateways.ContinuationToken))
                    {
                        pageResult.PageCount = 2;
                    }
                }
                else
                {
                    gateways = await _registryService.ListGatewaysAsync(previousPage.ContinuationToken, _commonHelper.PageLength).ConfigureAwait(false);

                    pageResult.PageCount = string.IsNullOrEmpty(gateways.ContinuationToken) ? previousPage.PageCount : previousPage.PageCount + 1;
                }

                if (gateways != null)
                {
                    foreach (var gw in gateways.Items)
                    {
                        var gateway = (await _registryService.GetGatewayAsync(gw.Id).ConfigureAwait(false)).Gateway;
                        pageResult.Results.Add(gateway);
                    }
                }
                if (previousPage != null)
                {
                    previousPage.Results.AddRange(pageResult.Results);
                    pageResult.Results = previousPage.Results;
                }

                pageResult.ContinuationToken = gateways.ContinuationToken;
                pageResult.PageSize = _commonHelper.PageLength;
                pageResult.RowCount = pageResult.Results.Count;
            }
            catch (UnauthorizedAccessException)
            {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e)
            {
                const string message = "Cannot get gateways list";
                _logger.LogWarning(e, message);
                pageResult.Error = message;
            }
            return pageResult;
        }

        /// <summary>
        /// GetPublisherListAsync
        /// </summary>
        /// <param name="previousPage"></param>
        /// <returns>PublisherModel</returns>
        public async Task<PagedResult<PublisherModel>> GetPublisherListAsync(PagedResult<PublisherModel> previousPage = null)
        {
            var pageResult = new PagedResult<PublisherModel>();

            try
            {
                var publisherModel = new PublisherQueryModel();
                var publishers = new PublisherListModel();

                if (string.IsNullOrEmpty(previousPage?.ContinuationToken))
                {
                    publishers = await _registryService.QueryPublishersAsync(publisherModel, null, _commonHelper.PageLengthSmall).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(publishers.ContinuationToken))
                    {
                        pageResult.PageCount = 2;
                    }
                }
                else
                {
                    publishers = await _registryService.ListPublishersAsync(previousPage.ContinuationToken, null, _commonHelper.PageLengthSmall).ConfigureAwait(false);

                    pageResult.PageCount = string.IsNullOrEmpty(publishers.ContinuationToken) ? previousPage.PageCount : previousPage.PageCount + 1;
                }

                if (publishers != null)
                {
                    foreach (var pub in publishers.Items)
                    {
                        var publisher = await _registryService.GetPublisherAsync(pub.Id).ConfigureAwait(false);
                        pageResult.Results.Add(publisher);
                    }
                }
                if (previousPage != null)
                {
                    previousPage.Results.AddRange(pageResult.Results);
                    pageResult.Results = previousPage.Results;
                }

                pageResult.ContinuationToken = publishers.ContinuationToken;
                pageResult.PageSize = _commonHelper.PageLengthSmall;
                pageResult.RowCount = pageResult.Results.Count;
            }
            catch (UnauthorizedAccessException)
            {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e)
            {
                const string message = "Cannot get publisher list";
                _logger.LogWarning(e, message);
                pageResult.Error = message;
            }
            return pageResult;
        }

        /// <summary>
        /// Unregister application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        public async Task<string> UnregisterApplicationAsync(string applicationId)
        {
            try
            {
                await _registryService.UnregisterApplicationAsync(applicationId).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to unregister application");
                return string.Concat(exception.Message, exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
            }
            return null;
        }

        /// <summary>
        /// GetSupervisorListAsync
        /// </summary>
        /// <param name="previousPage"></param>
        /// <returns>SupervisorModel</returns>
        public async Task<PagedResult<SupervisorModel>> GetSupervisorListAsync(PagedResult<SupervisorModel> previousPage = null)
        {
            var pageResult = new PagedResult<SupervisorModel>();

            try
            {
                var model = new SupervisorQueryModel();
                var supervisors = new SupervisorListModel();

                if (string.IsNullOrEmpty(previousPage?.ContinuationToken))
                {
                    supervisors = await _registryService.QuerySupervisorsAsync(model, null, _commonHelper.PageLength).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(supervisors.ContinuationToken))
                    {
                        pageResult.PageCount = 2;
                    }
                }
                else
                {
                    supervisors = await _registryService.ListSupervisorsAsync(previousPage.ContinuationToken, null, _commonHelper.PageLengthSmall).ConfigureAwait(false);

                    pageResult.PageCount = string.IsNullOrEmpty(supervisors.ContinuationToken) ? previousPage.PageCount : previousPage.PageCount + 1;
                }

                if (supervisors != null)
                {
                    foreach (var sup in supervisors.Items)
                    {
                        var supervisor = await _registryService.GetSupervisorAsync(sup.Id).ConfigureAwait(false);
                        pageResult.Results.Add(supervisor);
                    }
                }
                if (previousPage != null)
                {
                    previousPage.Results.AddRange(pageResult.Results);
                    pageResult.Results = previousPage.Results;
                }

                pageResult.ContinuationToken = supervisors.ContinuationToken;
                pageResult.PageSize = _commonHelper.PageLength;
                pageResult.RowCount = pageResult.Results.Count;
            }
            catch (UnauthorizedAccessException)
            {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e)
            {
                const string message = "Cannot get supervisor list";
                _logger.LogWarning(e, message);
                pageResult.Error = message;
            }
            return pageResult;
        }

        private readonly IRegistryServiceApi _registryService;
        private readonly ILogger _logger;
        private readonly UICommon _commonHelper;
        public string PathAll = "All";
    }
}
