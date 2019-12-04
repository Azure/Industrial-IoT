// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public class Registry {

        public Registry(IRegistryServiceApi registryService) {
            _registryService = registryService;
        }

        public async Task<PagedResult<EndpointInfoApiModel>> GetEndpointListAsync(
            string supervisorId) {

            // TODO Use query
            var pageResult = new PagedResult<EndpointInfoApiModel>();

            try {
                var endpoints = await _registryService.ListAllEndpointsAsync();

                var allApplications = await _registryService.ListAllApplicationsAsync();
                if (allApplications != null) {
                    foreach (var application in allApplications) {
                        if (application.SupervisorId == supervisorId) {
                            foreach (var endpoint in endpoints) {
                                if (endpoint.ApplicationId == application.ApplicationId) {
                                    pageResult.Results.Add(endpoint);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                Trace.TraceWarning("Can not get endpoint list");
                var errorMessage = string.Format(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceWarning(errorMessage);
            }

            pageResult.PageSize = 10;
            pageResult.RowCount = pageResult.Results.Count;
            pageResult.PageCount = (int)Math.Ceiling((decimal)pageResult.RowCount / 10);
            return pageResult;
        }


        public async Task<PagedResult<SupervisorInfo>> GetSupervisorListAsync() {
            var pageResult = new PagedResult<SupervisorInfo>();

            try {
                var supervisors = await _registryService.ListAllSupervisorsAsync();
                var applications = await _registryService.ListAllApplicationsAsync();

                if (supervisors != null) {
                    foreach (var supervisor in supervisors) {
                        var supervisorInfo = new SupervisorInfo {
                            SupervisorModel = supervisor,
                            HasApplication = false,
                            ScanStatus = (supervisor.Discovery == DiscoveryMode.Off) || (supervisor.Discovery == null) ? false : true
                        };
                        foreach (var application in applications) {
                            if (application.SupervisorId == supervisor.Id) {
                                supervisorInfo.HasApplication = true;
                            }
                        }
                        pageResult.Results.Add(supervisorInfo);
                    }
                }
            }
            catch (Exception e) {
                Trace.TraceWarning("Can not get supervisors list");
                var errorMessage = string.Format(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceWarning(errorMessage);
            }

            pageResult.PageSize = 10;
            pageResult.RowCount = pageResult.Results.Count;
            pageResult.PageCount = (int)Math.Ceiling((decimal)pageResult.RowCount / 10);
            return pageResult;
        }


        public async Task<PagedResult<ApplicationInfoApiModel>> GetApplicationListAsync() {
            var pageResult = new PagedResult<ApplicationInfoApiModel>();

            try {
                var applications = await _registryService.ListAllApplicationsAsync();

                if (applications != null) {
                    foreach (var application in applications) {
                        pageResult.Results.Add(application);
                    }
                }
            }
            catch (Exception e) {
                Trace.TraceWarning("Can not get applications list");
                var errorMessage = string.Format(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceWarning(errorMessage);
            }

            pageResult.PageSize = 10;
            pageResult.RowCount = pageResult.Results.Count;
            pageResult.PageCount = (int)Math.Ceiling((decimal)pageResult.RowCount / 10);
            return pageResult;
        }


        public async Task SetScanAsync(SupervisorInfo supervisor, string ipMask, string portRange, bool forceScan) {
            //var model = new SupervisorUpdateApiModel {
            //    DiscoveryConfig = new DiscoveryConfigApiModel()
            //};
            var model = new DiscoveryConfigApiModel();

            DiscoveryMode discoveryMode; 

            if (forceScan == true) {
                model.AddressRangesToScan = string.Empty;
                model.PortRangesToScan = string.Empty;
            }
            else {
                if (supervisor.SupervisorModel.DiscoveryConfig != null) {
                    model = supervisor.SupervisorModel.DiscoveryConfig;
                }
            }

            if (supervisor.ScanStatus == true && forceScan == true) {
                //model.Discovery = DiscoveryMode.Fast;
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
                //model.Discovery = DiscoveryMode.Off;
                discoveryMode = DiscoveryMode.Off;
            }

            try {
                //await _registryService.UpdateSupervisorAsync(supervisor.SupervisorModel.Id, model);
                if (discoveryMode == DiscoveryMode.Off) {
                    await _registryService.SetDiscoveryModeAsync(supervisor.SupervisorModel.Id, discoveryMode, new DiscoveryConfigApiModel());
                }
                else {
                    await _registryService.SetDiscoveryModeAsync(supervisor.SupervisorModel.Id, discoveryMode, model);
                }
                
            }
            catch (Exception exception) {
                var errorMessageTrace = string.Format(exception.Message, exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                Trace.TraceError(errorMessageTrace);
            }
        }

        private readonly IRegistryServiceApi _registryService;
        private const int _5MINUTES = 300;
    }
}
