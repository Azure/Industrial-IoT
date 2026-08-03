// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.AzureSdk
{
    using System;
    using System.Runtime.Serialization;
    using Xunit;

    public sealed class ConnectionStringTests
    {
        [Fact]
        public void ParseReadsKnownKeysCaseInsensitively()
        {
            var connectionString = ConnectionString.Parse(
                "hostname=hub.azure-devices.net;deviceid=device;moduleid=module;" +
                "gatewayhostname=edge;sharedaccesskey=key");

            Assert.Equal("hub.azure-devices.net", connectionString.HostName);
            Assert.Equal("device", connectionString.DeviceId);
            Assert.Equal("module", connectionString.ModuleId);
            Assert.Equal("edge", connectionString.GatewayHostName);
        }

        [Fact]
        public void ParseIgnoresEmptySegments()
        {
            var connectionString = ConnectionString.Parse(
                ";HostName=hub.azure-devices.net;;DeviceId=device;");

            Assert.Equal("hub.azure-devices.net", connectionString.HostName);
            Assert.Equal("device", connectionString.DeviceId);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ParseRejectsNullOrEmptyInput(string? value)
        {
            Assert.Throws<ArgumentNullException>(() => ConnectionString.Parse(value!));
        }

        [Theory]
        [InlineData("HostName")]
        [InlineData("NotAKey=value")]
        [InlineData("HostName=one;HostName=two")]
        public void ParseRejectsMalformedOrUnknownPairs(string value)
        {
            Assert.ThrowsAny<Exception>(() => ConnectionString.Parse(value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("HostName")]
        [InlineData("NotAKey=value")]
        [InlineData("HostName=one;HostName=two")]
        public void TryParseReturnsFalseForInvalidInput(string? value)
        {
            var parsed = ConnectionString.TryParse(value!, out var connectionString);

            Assert.False(parsed);
            Assert.Null(connectionString);
        }

        [Fact]
        public void EndpointPrefersAccountNameThenAccountEndpointThenEndpoint()
        {
            var accountName = ConnectionString.Parse(
                "AccountName=account;AccountEndpoint=account-endpoint;Endpoint=endpoint");
            var accountEndpoint = ConnectionString.Parse(
                "AccountEndpoint=account-endpoint;Endpoint=endpoint");
            var endpoint = ConnectionString.Parse("Endpoint=endpoint");

            Assert.Equal("account", accountName.Endpoint);
            Assert.Equal("account-endpoint", accountEndpoint.Endpoint);
            Assert.Equal("endpoint", endpoint.Endpoint);
        }

        [Fact]
        public void HubNameUsesEntityPathBeforeHostNamePrefix()
        {
            var entityPath = ConnectionString.Parse(
                "HostName=hub.azure-devices.net;EntityPath=events");
            var hostName = ConnectionString.Parse("HostName=hub.azure-devices.net");
            var withoutHub = ConnectionString.Parse("HostName=localhost");

            Assert.Equal("events", entityPath.HubName);
            Assert.Equal("hub", hostName.HubName);
            Assert.Null(withoutHub.HubName);
        }

        [Fact]
        public void CreateServiceConnectionStringSetsServiceCredentials()
        {
            var connectionString = ConnectionString.CreateServiceConnectionString(
                "hub.azure-devices.net", "owner", "key");
            var reparsed = ConnectionString.Parse(connectionString.ToString());

            Assert.Equal("hub.azure-devices.net", reparsed.HostName);
            Assert.Contains("SharedAccessKeyName=owner", connectionString.ToString());
            Assert.Contains("SharedAccessKey=key", connectionString.ToString());
        }

        [Fact]
        public void CreateModuleConnectionStringSetsModuleCredentials()
        {
            var connectionString = ConnectionString.CreateModuleConnectionString(
                "hub.azure-devices.net", "device", "module", "key");
            var reparsed = ConnectionString.Parse(connectionString.ToString());

            Assert.Equal("hub.azure-devices.net", reparsed.HostName);
            Assert.Equal("device", reparsed.DeviceId);
            Assert.Equal("module", reparsed.ModuleId);
            Assert.Contains("SharedAccessKey=key", connectionString.ToString());
        }

        [Fact]
        public void MissingOptionalValuesReturnNull()
        {
            var connectionString = ConnectionString.Parse("SharedAccessKey=key");

            Assert.Null(connectionString.HostName);
            Assert.Null(connectionString.DeviceId);
            Assert.Null(connectionString.ModuleId);
            Assert.Null(connectionString.GatewayHostName);
            Assert.Null(connectionString.EntityPath);
            Assert.Null(connectionString.Endpoint);
            Assert.Null(connectionString.HubName);
        }
    }
}
