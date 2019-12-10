﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public class Registry {

        public Registry(IRegistryServiceApi registryService) {
            _registryService = registryService;
        }

        public async Task<PagedResult<EndpointInfo>> GetEndpointListAsync(
            string supervisorId) {

            // TODO Use query
            var pageResult = new PagedResult<EndpointInfo>();

            try {
                var endpoints = await _registryService.ListAllEndpointsAsync();

                var allApplications = await _registryService.ListAllApplicationsAsync();
                if (allApplications != null) {
                    foreach (var application in allApplications) {
                        if (application.SupervisorId == supervisorId) {
                            foreach (var endpoint in endpoints) {
                                var EndpointInfo = new EndpointInfo {
                                    EndpointModel = endpoint,
                                    EndpointState = endpoint.ActivationState == EndpointActivationState.Deactivated ? false : true
                                };
                                if (endpoint.ApplicationId == application.ApplicationId) {
                                    pageResult.Results.Add(EndpointInfo);
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


        public async void SetScanAsync(SupervisorInfo supervisor, string ipMask, string portRange, bool forceScan) {
            var model = new SupervisorUpdateApiModel {
                DiscoveryConfig = new DiscoveryConfigApiModel()
            };

            if (forceScan == true) {
                model.DiscoveryConfig.AddressRangesToScan = string.Empty;
                model.DiscoveryConfig.PortRangesToScan = string.Empty;
            }
            else {
                model.DiscoveryConfig = supervisor.SupervisorModel.DiscoveryConfig;
            }

            if (supervisor.ScanStatus == false || forceScan == true) {
                model.Discovery = DiscoveryMode.Fast;

                if (ipMask != null) {
                    model.DiscoveryConfig.AddressRangesToScan = ipMask;
                }
                if (portRange != null) {
                    model.DiscoveryConfig.PortRangesToScan = portRange;
                }
            }
            else {
                model.Discovery = DiscoveryMode.Off;
            }

            try {
                await _registryService.UpdateSupervisorAsync(supervisor.SupervisorModel.Id, model);
            }
            catch (Exception exception) {
                var errorMessageTrace = string.Format(exception.Message, exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                Trace.TraceError(errorMessageTrace);
            }
        }

        private readonly IRegistryServiceApi _registryService;
    }
}
