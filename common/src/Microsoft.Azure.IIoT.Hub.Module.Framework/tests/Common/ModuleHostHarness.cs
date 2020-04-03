// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Utils;
    using Autofac;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class ModuleHostHarness {

        /// <summary>
        /// Module test harness
        /// </summary>
        /// <param name="controllers"></param>
        /// <param name="test"></param>
        /// <returns></returns>
        public async Task RunTestAsync(
            IEnumerable<object> controllers, Func<string, string, IContainer, Task> test) {

            var deviceId = "TestDevice";
            var moduleId = "TestModule";

            using (var hubContainer = CreateHubContainer()) {
                var services = hubContainer.Resolve<IIoTHubTwinServices>();

                // Create module
                var twin = await services.CreateAsync(new DeviceTwinModel {
                    Id = "TestDevice",
                    ModuleId = "TestModule"
                });
                var etag = twin.Etag;
                var device = await services.GetRegistrationAsync(twin.Id, twin.ModuleId);

                // Create module host with controller
                using (var moduleContainer = CreateModuleContainer(services, device,
                    controllers)) {
                    var edge = moduleContainer.Resolve<IModuleHost>();

                    // Act
                    await edge.StartAsync("testType", "TestSite", "MS", null);
                    twin = await services.GetAsync(deviceId, moduleId);

                    // Assert
                    Assert.NotEqual(etag, twin.Etag);
                    Assert.Equal("connected", twin.ConnectionState);
                    Assert.Equal("testType", twin.Properties.Reported[TwinProperty.Type]);
                    Assert.Equal("TestSite", twin.Properties.Reported[TwinProperty.SiteId]);
                    etag = twin.Etag;

                    await test(deviceId, moduleId, hubContainer);

                    twin = await services.GetAsync(deviceId, moduleId);
                    Assert.True(twin.Properties.Reported[TwinProperty.Type] == "testType");
                    Assert.True("TestSite" == twin.Properties.Reported[TwinProperty.SiteId]);
                    etag = twin.Etag;

                    // Act
                    await edge.StopAsync();
                    twin = await services.GetAsync(deviceId, moduleId);

                    // Assert
                    Assert.False((bool)twin.Properties.Reported[TwinProperty.Connected]);

                    // TODO : Fix cleanup!!!

                    // TODO :Assert.True("testType" != twin.Properties.Reported[TwinProperty.kType]);
                    // TODO :Assert.True("TestSite" != twin.Properties.Reported[TwinProperty.kSiteId]);
                    // TODO :Assert.Equal("disconnected", twin.ConnectionState);
                    Assert.NotEqual(etag, twin.Etag);
                }
            }
        }

        public class TestIoTHubConfig : IIoTHubConfig {
            public string IoTHubConnString =>
                ConnectionString.CreateServiceConnectionString(
                    "test.test.org", "iothubowner", Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))).ToString();
            public string IoTHubResourceId => null;
        }

        public class TestModuleConfig : IModuleConfig {

            public TestModuleConfig(DeviceModel device) {
                _device = device;
            }
            public string EdgeHubConnectionString =>
                ConnectionString.CreateModuleConnectionString("test.test.org",
                    _device.Id, _device.ModuleId, _device.Authentication.PrimaryKey)
                .ToString();

            public bool BypassCertVerification => true;

            public TransportOption Transport => TransportOption.Any;

            private readonly DeviceModel _device;
        }

        /// <summary>
        /// Create hub container
        /// </summary>
        /// <returns></returns>
        private IContainer CreateHubContainer() {
            var builder = new ContainerBuilder();
            builder.AddDiagnostics();
            builder.RegisterModule<IoTHubMockService>();
            builder.RegisterType<TestIoTHubConfig>()
                .AsImplementedInterfaces();
            return builder.Build();
        }

        /// <summary>
        /// Create module container
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="device"></param>
        /// <param name="controllers"></param>
        /// <returns></returns>
        private IContainer CreateModuleContainer(IIoTHubTwinServices hub, DeviceModel device,
            IEnumerable<object> controllers) {
            var builder = new ContainerBuilder();
            builder.AddDiagnostics();
            builder.RegisterInstance(hub)
                .AsImplementedInterfaces().ExternallyOwned();
            builder.RegisterInstance(new TestModuleConfig(device))
                .AsImplementedInterfaces();
            builder.RegisterModule<IoTHubMockModule>();
            foreach (var controller in controllers) {
                builder.RegisterInstance(controller)
                    .AsImplementedInterfaces();
            }
            return builder.Build();
        }
    }
}
