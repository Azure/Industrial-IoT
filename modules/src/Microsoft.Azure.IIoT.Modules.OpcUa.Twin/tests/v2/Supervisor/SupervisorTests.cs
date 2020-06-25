// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Supervisor {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Hub;
    using Autofac;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class SupervisorTests {

        [Fact]
        public async Task TestListSupervisors() {
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {

                    // Setup
                    var registry = services.Resolve<ISupervisorRegistry>();

                    // Act
                    var supervisors = await registry.ListAllSupervisorsAsync();

                    // Assert
                    Assert.Single(supervisors);
                    Assert.True(supervisors.Single().Connected.Value);
                    Assert.True(supervisors.Single().OutOfSync.Value);
                    Assert.Equal(device, SupervisorModelEx.ParseDeviceId(supervisors.Single().Id, out var moduleId));
                    Assert.Equal(module, moduleId);
                });
            }
        }

        [Fact]
        public async Task TestGetSupervisor() {
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {

                    // Setup
                    var registry = services.Resolve<ISupervisorRegistry>();

                    // Act
                    var supervisor = await registry.GetSupervisorAsync(SupervisorModelEx.CreateSupervisorId(device, module));

                    // Assert
                    Assert.True(supervisor.Connected.Value);
                    Assert.True(supervisor.OutOfSync.Value);
                });
            }
        }

        [Fact]
        public async Task TestGetSupervisorStatus() {
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {

                    // Setup
                    var diagnostics = services.Resolve<ISupervisorDiagnostics>();

                    // Act
                    var status = await diagnostics.GetSupervisorStatusAsync(SupervisorModelEx.CreateSupervisorId(device, module));

                    // Assert
                    Assert.Equal(status.DeviceId, device);
                    Assert.Equal(status.ModuleId, module);
                    Assert.Null(status.SiteId);
                    Assert.Empty(status.Endpoints);
                });
            }
        }

        [Fact]
        public async Task TestActivateEndpoint() {
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {

                    // Setup
                    var supervisorId = SupervisorModelEx.CreateSupervisorId(device, module);
                    var activation = services.Resolve<IEndpointActivation>();
                    var hub = services.Resolve<IIoTHubTwinServices>();
                    var twin = new EndpointInfoModel {
                        Registration = new EndpointRegistrationModel {
                            Endpoint = new EndpointModel {
                                Url = "opc.tcp://test"
                            },
                            SupervisorId = supervisorId
                        },
                        ApplicationId = "ua326029342304923"
                    }.ToEndpointRegistration(_serializer).ToDeviceTwin(_serializer);
                    await hub.CreateOrUpdateAsync(twin);
                    var registry = services.Resolve<IEndpointRegistry>();
                    var endpoints = await registry.ListAllEndpointsAsync();
                    var ep1 = endpoints.FirstOrDefault();
                    Assert.NotNull(ep1);

                    // Act
                    await activation.ActivateEndpointAsync(ep1.Registration.Id);
                    endpoints = await registry.ListAllEndpointsAsync();
                    var ep2 = endpoints.FirstOrDefault();
                    var diagnostics = services.Resolve<ISupervisorDiagnostics>();
                    var status = await diagnostics.GetSupervisorStatusAsync(supervisorId);

                    // Assert
                    Assert.Equal(device, status.DeviceId);
                    Assert.Equal(module, status.ModuleId);
                    Assert.Null(status.SiteId);
                    Assert.Single(status.Endpoints);
                    Assert.Equal(ep1.Registration.Id, status.Endpoints.Single().Id);
                    Assert.Equal(EndpointActivationState.ActivatedAndConnected, status.Endpoints.Single().ActivationState);
                    Assert.Equal(EndpointActivationState.ActivatedAndConnected, ep2.ActivationState);
                    Assert.True(
                        ep2.EndpointState == EndpointConnectivityState.Connecting ||
                        ep2.EndpointState == EndpointConnectivityState.NotReachable);
                });
            }
        }

        [Fact]
        public async Task TestActivateDeactivateEndpointAsync() {
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {

                    // Setup
                    var supervisorId = SupervisorModelEx.CreateSupervisorId(device, module);
                    var activation = services.Resolve<IEndpointActivation>();
                    var hub = services.Resolve<IIoTHubTwinServices>();
                    var twin = new EndpointInfoModel {
                        Registration = new EndpointRegistrationModel {
                            Endpoint = new EndpointModel {
                                Url = "opc.tcp://test"
                            },
                            SupervisorId = supervisorId
                        },
                        ApplicationId = "ua326029342304923"
                    }.ToEndpointRegistration(_serializer).ToDeviceTwin(_serializer);

                    await hub.CreateOrUpdateAsync(twin);
                    var registry = services.Resolve<IEndpointRegistry>();
                    var endpoints = await registry.ListAllEndpointsAsync();
                    var ep1 = endpoints.FirstOrDefault();
                    Assert.NotNull(ep1);

                    // Act
                    await activation.ActivateEndpointAsync(ep1.Registration.Id);
                    endpoints = await registry.ListAllEndpointsAsync();
                    var ep2 = endpoints.FirstOrDefault();
                    await activation.DeactivateEndpointAsync(ep2.Registration.Id);
                    var diagnostics = services.Resolve<ISupervisorDiagnostics>();
                    endpoints = await registry.ListAllEndpointsAsync();
                    var ep3 = endpoints.FirstOrDefault();
                    var status = await diagnostics.GetSupervisorStatusAsync(supervisorId);

                    // Assert
                    Assert.Equal(device, status.DeviceId);
                    Assert.Equal(module, status.ModuleId);
                    Assert.Null(status.SiteId);
                    Assert.Empty(status.Endpoints);
                    Assert.Equal(EndpointActivationState.Deactivated, ep3.ActivationState);
                    Assert.Equal(EndpointConnectivityState.Disconnected, ep3.EndpointState);
                });
            }
        }

        [Fact]
        public async Task TestActivateDeactivateEndpoint20TimesAsync() {
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {

                    // Setup
                    var supervisorId = SupervisorModelEx.CreateSupervisorId(device, module);
                    var activation = services.Resolve<IEndpointActivation>();
                    var hub = services.Resolve<IIoTHubTwinServices>();
                    var twin = new EndpointInfoModel {
                        Registration = new EndpointRegistrationModel {
                            Endpoint = new EndpointModel {
                                Url = "opc.tcp://test"
                            },
                            SupervisorId = supervisorId
                        },
                        ApplicationId = "ua326029342304923"
                    }.ToEndpointRegistration(_serializer).ToDeviceTwin(_serializer);

                    await hub.CreateOrUpdateAsync(twin);
                    var registry = services.Resolve<IEndpointRegistry>();
                    var endpoints = await registry.ListAllEndpointsAsync();
                    var ep1 = endpoints.FirstOrDefault();
                    Assert.NotNull(ep1);

                    for (var i = 0; i < 20; i++) {
                        // Act
                        await activation.ActivateEndpointAsync(ep1.Registration.Id);
                        await activation.DeactivateEndpointAsync(ep1.Registration.Id);
                    }

                    var diagnostics = services.Resolve<ISupervisorDiagnostics>();
                    endpoints = await registry.ListAllEndpointsAsync();
                    var ep3 = endpoints.FirstOrDefault();
                    var status = await diagnostics.GetSupervisorStatusAsync(supervisorId);

                    // Assert
                    Assert.Equal(device, status.DeviceId);
                    Assert.Equal(module, status.ModuleId);
                    Assert.Null(status.SiteId);
                    Assert.Empty(status.Endpoints);
                    Assert.Equal(EndpointActivationState.Deactivated, ep3.ActivationState);
                    Assert.Equal(EndpointConnectivityState.Disconnected, ep3.EndpointState);
                });
            }
        }

        [Fact]
        public async Task TestActivateDeactivate20Endpoints5TimesMultiThreadedAsync() {
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {

                    // Setup
                    var supervisorId = SupervisorModelEx.CreateSupervisorId(device, module);
                    var activation = services.Resolve<IEndpointActivation>();
                    var hub = services.Resolve<IIoTHubTwinServices>();

                    for (var i = 0; i < 20; i++) {
                        var twin = new EndpointInfoModel {
                            Registration = new EndpointRegistrationModel {
                                Endpoint = new EndpointModel {
                                    Url = "opc.tcp://test"
                                },
                                SupervisorId = supervisorId
                            },
                            ApplicationId = "uas" + i
                        }.ToEndpointRegistration(_serializer).ToDeviceTwin(_serializer);
                        await hub.CreateOrUpdateAsync(twin);
                    }

                    var registry = services.Resolve<IEndpointRegistry>();
                    var endpoints = await registry.ListAllEndpointsAsync();

                    for (var i = 0; i < 5; i++) {
                        await Task.WhenAll(endpoints.Select(ep => activation.ActivateEndpointAsync(ep.Registration.Id)));
                        await Task.WhenAll(endpoints.Select(ep => activation.DeactivateEndpointAsync(ep.Registration.Id)));
                    }

                    var diagnostics = services.Resolve<ISupervisorDiagnostics>();
                    endpoints = await registry.ListAllEndpointsAsync();
                    var status = await diagnostics.GetSupervisorStatusAsync(supervisorId);

                    // Assert
                    Assert.Equal(device, status.DeviceId);
                    Assert.Equal(module, status.ModuleId);
                    Assert.Null(status.SiteId);
                    Assert.Empty(status.Endpoints);
                    Assert.True(endpoints.All(ep => ep.ActivationState == EndpointActivationState.Deactivated));
                    Assert.True(endpoints.All(ep => ep.EndpointState == EndpointConnectivityState.Disconnected));
                });
            }
        }

        private readonly IJsonSerializer _serializer = new NewtonSoftJsonSerializer();
    }
}
