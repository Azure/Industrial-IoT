// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Autofac.Extras.Moq;
    using AutoFixture;
    using AutoFixture.Kernel;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class SecurityNotificationTests {

        [Fact]
        public void SendSecurityAlertWhenEndpointModeUnsecure() {
            SecurityMode mode = SecurityMode.None;
            CreateEndpointFixtures(mode, "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256", out var endpoints);

            using (var mock = AutoMock.GetLoose()) {
                var mockIotTelemetryService = new IoTHubServices(null);
                mock.Provide<IIoTHubTelemetryServices>(mockIotTelemetryService);
                var service = mock.Create<SecurityNotificationService>();

                // Run
                var t = service.OnEndpointAddedAsync(endpoints.FirstOrDefault().Registration);

                // Assert
                Assert.True(t.IsCompletedSuccessfully);
                mockIotTelemetryService.Events.TryTake(out var eventMessage);
                eventMessage.Message.Properties.TryGetValue(SystemProperties.InterfaceId, out string val);
                Assert.Equal("http://security.azureiot.com/SecurityAgent/1.0.0", val);
            }
        }

        [Fact]
        public void SendSecurityAlertWhenEndpointPolicyUnsecure() {
            SecurityMode mode = SecurityMode.Best;
            CreateEndpointFixtures(mode, "http://opcfoundation.org/UA/SecurityPolicy#None", out var endpoints);

            using (var mock = AutoMock.GetLoose())
            {
                var mockIotTelemetryService = new IoTHubServices(null);
                mock.Provide<IIoTHubTelemetryServices>(mockIotTelemetryService);
                var service = mock.Create<SecurityNotificationService>();

                // Run
                var t = service.OnEndpointAddedAsync(endpoints.FirstOrDefault().Registration);

                // Assert
                Assert.True(t.IsCompletedSuccessfully);
                mockIotTelemetryService.Events.TryTake(out var eventMessage);
                eventMessage.Message.Properties.TryGetValue(SystemProperties.InterfaceId, out string val);
                Assert.Equal("http://security.azureiot.com/SecurityAgent/1.0.0", val);
            }
        }

        [Fact]
        public void DoNotSendSecurityAlertWhenEndpointPolicyAndModeSecure() {
            SecurityMode mode = SecurityMode.Best;
            CreateEndpointFixtures(mode, "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256", out var endpoints);

            using (var mock = AutoMock.GetLoose())
            {
                var mockIotTelemetryService = new IoTHubServices(null);
                mock.Provide<IIoTHubTelemetryServices>(mockIotTelemetryService);
                var service = mock.Create<SecurityNotificationService>();

                // Run
                var t = service.OnEndpointAddedAsync(endpoints.FirstOrDefault().Registration);

                // Assert
                Assert.True(t.IsCompletedSuccessfully);
                mockIotTelemetryService.Events.TryTake(out var eventMessage);
                Assert.Null(eventMessage);
           }
        }
        /// <summary>
        /// Helper to create app fixtures
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="policy"></param>
        /// <param name="endpoints"></param>
        private static void CreateEndpointFixtures(SecurityMode mode, string policy, out List<EndpointInfoModel> endpoints) {
            var fix = new Fixture();
            fix.Customizations.Add(new TypeRelay(typeof(JToken), typeof(JObject)));
            var superx = fix.Create<string>();
            endpoints = fix
                .Build<EndpointInfoModel>()
                .Without(x => x.Registration)
                .Do(x => x.Registration = fix
                    .Build<EndpointRegistrationModel>()
                    .With(y => y.SupervisorId, superx)
                    .Without(y => y.Endpoint)
                    .Do(y => y.Endpoint = fix
                        .Build<EndpointModel>()
                        .With(z => z.SecurityMode , mode)
                        .With(z => z.SecurityPolicy , policy)
                        .Create())
                    .Create())
                .CreateMany(1)
                .ToList();
        }
    }
}
