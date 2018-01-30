// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net;
using Newtonsoft.Json;
using WebService.Test.helpers;
using WebService.Test.helpers.Http;
using Xunit;
using Xunit.Abstractions;

namespace WebService.Test.IntegrationTests
{
    /// <summary>
    /// This test suite summarizes the integration between Device Simulation
    /// and IoT Hub manager. If the test fails, most likely the integration
    /// with Device Simulation is not working anymore.
    ///
    /// IMPORTANT: these tests should not be modified without a plan to modify
    /// the Device Simulation microservice. Most likely, other integrations
    /// will break if one of these tests fails.
    /// </summary>
    public class DeviceSimulationIntegrationTest
    {
        // Pull Request don't have access to secret credentials, which are
        // required to run tests interacting with Azure IoT Hub.
        // The tests should run when working locally and when merging branches.
        private readonly bool credentialsAvailable;

        private readonly HttpClient httpClient;
        private readonly string hostname;

        public DeviceSimulationIntegrationTest(ITestOutputHelper log)
        {
            this.httpClient = new HttpClient(log);
            this.credentialsAvailable = !CIVariableHelper.IsPullRequest(log);
            this.hostname = AssemblyInitialize.Current.WsHostname;
        }

        /// <summary>
        /// IMPORTANT: the test should not be modified without considering
        /// the impact on other microservices.
        /// </summary>
        [SkippableFact, Trait(Constants.TYPE, Constants.INTEGRATION_TEST)]
        public void MissingDevice()
        {
            Skip.IfNot(this.credentialsAvailable, "Credentials not available for Pull Requests");

            // Act
            var deviceId = "TEST-MissingDevice-" + Guid.NewGuid();
            var request = new HttpRequest();
            request.SetUriFromString(this.hostname + "/v1/devices/" + WebUtility.UrlDecode(deviceId));
            var response = this.httpClient.GetAsync(request).Result;

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <summary>
        /// IMPORTANT: the test should not be modified without considering
        /// the impact on other microservices.
        /// </summary>
        [SkippableFact, Trait(Constants.TYPE, Constants.INTEGRATION_TEST)]
        public void CreateDevice()
        {
            Skip.IfNot(this.credentialsAvailable, "Credentials not available for Pull Requests");

            // Act
            var inputDevice = new DeviceModelUsedByDeviceSimulation
            {
                Id = "TEST-CreateDevice-" + Guid.NewGuid()
            };
            var request = new HttpRequest();
            request.SetContent(inputDevice);
            request.SetUriFromString(this.hostname + "/v1/devices");
            var response = this.httpClient.PostAsync(request).Result;

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var outputDevice = JsonConvert.DeserializeObject<DeviceModelUsedByDeviceSimulation>(response.Content);
            Assert.Equal(inputDevice.Id, outputDevice.Id);
        }

        /// <summary>
        /// IMPORTANT: the test should not be modified without considering
        /// the impact on other microservices.
        /// </summary>
        [SkippableFact, Trait(Constants.TYPE, Constants.INTEGRATION_TEST)]
        public void CreateAndGetDevice()
        {
            Skip.IfNot(this.credentialsAvailable, "Credentials not available for Pull Requests");

            // Act
            var inputDevice = new DeviceModelUsedByDeviceSimulation
            {
                Id = "TEST-CreateDevice-" + Guid.NewGuid()
            };
            var request = new HttpRequest();
            request.SetContent(inputDevice);
            request.SetUriFromString(this.hostname + "/v1/devices");
            this.httpClient.PostAsync(request).Wait();
            request = new HttpRequest();
            request.SetUriFromString(this.hostname + "/v1/devices/" + WebUtility.UrlDecode(inputDevice.Id));
            var response = this.httpClient.GetAsync(request).Result;

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var outputDevice = JsonConvert.DeserializeObject<DeviceModelUsedByDeviceSimulation>(response.Content);
            Assert.Equal(inputDevice.Id, outputDevice.Id);
        }

        /// <summary>
        /// See https://github.com/Azure/device-simulation-dotnet/blob/master/Services/Models/Device.cs
        /// IMPORTANT: don't change this class without updating and versioning
        /// the dependant client services
        /// </summary>
        private sealed class DeviceModelUsedByDeviceSimulation
        {
            [JsonProperty(PropertyName = "Etag")]
            public string Etag { get; set; }

            [JsonProperty(PropertyName = "Id")]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "C2DMessageCount")]
            public int C2DMessageCount { get; set; }

            [JsonProperty(PropertyName = "LastActivity")]
            public DateTime LastActivity { get; set; }

            [JsonProperty(PropertyName = "Connected")]
            public bool Connected { get; set; }

            [JsonProperty(PropertyName = "Enabled")]
            public bool Enabled { get; set; }

            [JsonProperty(PropertyName = "LastStatusUpdated")]
            public DateTime LastStatusUpdated { get; set; }

            [JsonProperty(PropertyName = "IoTHubHostName")]
            public string IoTHubHostName { get; set; }

            [JsonProperty(PropertyName = "AuthPrimaryKey")]
            public string AuthPrimaryKey { get; set; }
        }
    }
}
