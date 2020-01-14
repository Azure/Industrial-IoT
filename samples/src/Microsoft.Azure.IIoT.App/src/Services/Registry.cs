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
                pageResult.Results.Add(new DiscovererInfo {
                    DiscovererModel = new DiscovererApiModel { Id = e.Message }
                });
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
                pageResult.Results.Add(new ApplicationInfoApiModel {
                    ApplicationId = e.Message
                });
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
        /// <param name="ipMask"></param>
        /// <param name="portRange"></param>
        /// <param name="forceScan"></param>
        /// <returns></returns>
        public async Task SetScanAsync(DiscovererInfo discoverer, string ipMask, string portRange, bool forceScan) {
            var model = new DiscoveryConfigApiModel();

            DiscoveryMode discoveryMode;

            if (forceScan == true) {
                model.AddressRangesToScan = string.Empty;
                model.PortRangesToScan = string.Empty;
            }
            else {
                if (discoverer.DiscovererModel.DiscoveryConfig != null) {
                    model = discoverer.DiscovererModel.DiscoveryConfig;
                }
            }

            if (discoverer.ScanStatus == true && forceScan == true) {
                discoveryMode = DiscoveryMode.Fast;

                if (ipMask != null) {
                    model.AddressRangesToScan = ipMask;
                }
                if (portRange != null) {
                    model.PortRangesToScan = portRange;
                }
                model.IdleTimeBetweenScansSec = _5MINUTES;
            }
            else {
                discoveryMode = DiscoveryMode.Off;
            }

            try {
                if (discoveryMode == DiscoveryMode.Off) {
                    await _registryService.SetDiscoveryModeAsync(discoverer.DiscovererModel.Id, discoveryMode, new DiscoveryConfigApiModel());
                }
                else {
                    await _registryService.SetDiscoveryModeAsync(discoverer.DiscovererModel.Id, discoveryMode, model);
                }

            }
            catch (Exception exception) {
                var errorMessageTrace = string.Concat(exception.Message, exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                Trace.TraceError(errorMessageTrace);
            }
        }

        private readonly IRegistryServiceApi _registryService;
        private const int _5MINUTES = 300;
        public string PathAll = "All";
    }
}
