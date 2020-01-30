// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System;
    using System.Linq;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    public class Registry {

        /// <summary>
        /// Create registry
        /// </summary>
        /// <param name="registryService"></param>
        public Registry(IRegistryServiceApi registryService) {
            _registryService = registryService;
        }

        /// <summary>
        /// GetEndpointListAsync
        /// </summary>
        /// <param name="discovererId"></param>
        /// <returns>EndpointInfoApiModel</returns>
        public async Task<PagedResult<EndpointInfoApiModel>> GetEndpointListAsync(
            string discovererId, string applicationId) {

            var pageResult = new PagedResult<EndpointInfoApiModel>();

            try {
                var model = new EndpointRegistrationQueryApiModel();
                model.DiscovererId = discovererId == PathAll ? null : discovererId;
                model.ApplicationId = applicationId == PathAll ? null : applicationId;

                var endpoints = await _registryService.QueryAllEndpointsAsync(model);
                foreach (var endpoint in endpoints) {
                    pageResult.Results.Add(endpoint);
                }
            }
            catch (Exception e) {
                Trace.TraceWarning("Can not get endpoint list");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceWarning(errorMessage);
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

                if (discoverers != null) {
                    if (discoverers.Count() > 0) {
                        foreach (var discoverer in discoverers) {
                            var info = new DiscovererInfo {
                                DiscovererModel = discoverer,
                                HasApplication = false,
                                ScanStatus = (discoverer.Discovery == DiscoveryMode.Off) || (discoverer.Discovery == null) ? false : true
                            };
                            applicationModel.DiscovererId = discoverer.Id;
                            var applications = await _registryService.QueryAllApplicationsAsync(applicationModel);
                            if (applications != null) {
                                info.HasApplication = true;
                            }
                            pageResult.Results.Add(info);
                        }
                    }
                    else {
                        pageResult.Results.Add(new DiscovererInfo {
                            DiscovererModel = new DiscovererApiModel { Id = "No Discoveres Found" }
                        });
                    }
                }
            }
            catch (Exception e) {
                Trace.TraceWarning("Can not get discoverers as list");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceWarning(errorMessage);
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
                    foreach (var application in applications) {
                        pageResult.Results.Add(application);
                    }
                }
            }
            catch (Exception e) {
                Trace.TraceWarning("Can not get applications list");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceWarning(errorMessage);
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
            var model = discoverer.DiscovererModel.DiscoveryConfig;
            DiscoveryMode discoveryMode;

            if (model == null) {
                model = new DiscoveryConfigApiModel();
            }

            if (discoverer.ScanStatus == true) {
                discoveryMode = DiscoveryMode.Fast;
            }
            else {
                discoveryMode = DiscoveryMode.Off;
            }
            
            try {
                await _registryService.SetDiscoveryModeAsync(discoverer.DiscovererModel.Id, discoveryMode, model);
            }
            catch (Exception exception) {
                var errorMessageTrace = string.Concat(exception.Message, exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                Trace.TraceError(errorMessageTrace);
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
        public async Task<string> UpdateDiscovererAsync(DiscovererInfo discoverer, DiscoveryConfigApiModel config) {
            var model = new DiscovererUpdateApiModel();
            model.DiscoveryConfig = discoverer.DiscovererModel.DiscoveryConfig;

            if (config.AddressRangesToScan != null) {
                model.DiscoveryConfig.AddressRangesToScan = config.AddressRangesToScan;
            }
            if (config.PortRangesToScan != null) {
                model.DiscoveryConfig.PortRangesToScan = config.PortRangesToScan;
            }
            if (config.ActivationFilter != null) {
                model.DiscoveryConfig.ActivationFilter = config.ActivationFilter;
            }
            if (config.MaxNetworkProbes != null && config.MaxNetworkProbes != 0) {
                model.DiscoveryConfig.MaxNetworkProbes = config.MaxNetworkProbes;
            }
            if (config.MaxPortProbes != null && config.MaxPortProbes != 0) {
                model.DiscoveryConfig.MaxPortProbes = config.MaxPortProbes;
            }
            if (config.NetworkProbeTimeoutMs != null && config.NetworkProbeTimeoutMs != 0) {
                model.DiscoveryConfig.NetworkProbeTimeoutMs = config.NetworkProbeTimeoutMs;
            }
            if (config.PortProbeTimeoutMs != null && config.PortProbeTimeoutMs != 0) {
                model.DiscoveryConfig.PortProbeTimeoutMs = config.PortProbeTimeoutMs;
            }
            if (config.IdleTimeBetweenScansSec != null && config.IdleTimeBetweenScansSec != 0) {
                model.DiscoveryConfig.IdleTimeBetweenScansSec = config.IdleTimeBetweenScansSec;
            }
            else {
                model.DiscoveryConfig.IdleTimeBetweenScansSec = _5MINUTES;
            }

            try {
                await _registryService.UpdateDiscovererAsync(discoverer.DiscovererModel.Id, model);
            }
            catch (Exception exception) {
                var errorMessageTrace = string.Concat(exception.Message, exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                Trace.TraceError(errorMessageTrace);
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
                    foreach (var gateway in gateways) {
                        pageResult.Results.Add(gateway);
                    }
                }
            }
            catch (Exception e) {
                Trace.TraceWarning("Can not get gateways list");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceWarning(errorMessage);
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
                    foreach (var publisher in publishers) {
                        pageResult.Results.Add(publisher);
                    }
                }
            }
            catch (Exception e) {
                Trace.TraceWarning("Can not get publisher list");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceWarning(errorMessage);
                pageResult.Error = e.Message;
            }

            pageResult.PageSize = 10;
            pageResult.RowCount = pageResult.Results.Count;
            pageResult.PageCount = (int)Math.Ceiling((decimal)pageResult.RowCount / 10);
            return pageResult;
        }

        private readonly IRegistryServiceApi _registryService;
        private const int _5MINUTES = 300;
        public string PathAll = "All";
    }
}
