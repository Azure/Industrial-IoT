// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients
{
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Xunit;

    [Trait("Compatibility", "Authoritative")]
    public sealed class EventClientCapabilitiesTests
    {
        public static TheoryData<Type, EventClientCapabilities> ShippingClients { get; } = new()
        {
            {
                typeof(MqttClientTransport),
                EventClientCapabilities.Payload
                    | EventClientCapabilities.Topic
                    | EventClientCapabilities.QualityOfService
                    | EventClientCapabilities.Retain
                    | EventClientCapabilities.TimeToLive
                    | EventClientCapabilities.ContentType
                    | EventClientCapabilities.ContentEncoding
                    | EventClientCapabilities.CustomProperties
                    | EventClientCapabilities.CloudEvents
                    | EventClientCapabilities.TransportSecurity
                    | EventClientCapabilities.Authentication
            },
            {
                typeof(DaprPubSubClient),
                EventClientCapabilities.Payload
                    | EventClientCapabilities.Topic
                    | EventClientCapabilities.ContentType
                    | EventClientCapabilities.TransportSecurity
                    | EventClientCapabilities.Authentication
            },
            {
                typeof(HttpEventClient),
                EventClientCapabilities.Payload
                    | EventClientCapabilities.Topic
                    | EventClientCapabilities.ContentType
                    | EventClientCapabilities.ContentEncoding
                    | EventClientCapabilities.CustomProperties
                    | EventClientCapabilities.CloudEvents
                    | EventClientCapabilities.TransportSecurity
                    | EventClientCapabilities.Authentication
            },
            {
                typeof(EventHubsClient),
                EventClientCapabilities.Payload
                    | EventClientCapabilities.Topic
                    | EventClientCapabilities.ContentType
                    | EventClientCapabilities.ContentEncoding
                    | EventClientCapabilities.CustomProperties
                    | EventClientCapabilities.CloudEvents
                    | EventClientCapabilities.TransportSecurity
                    | EventClientCapabilities.Authentication
            },
            {
                typeof(IoTEdgeTransport),
                EventClientCapabilities.Payload
                    | EventClientCapabilities.Topic
                    | EventClientCapabilities.ContentType
                    | EventClientCapabilities.ContentEncoding
                    | EventClientCapabilities.CustomProperties
                    | EventClientCapabilities.CloudEvents
                    | EventClientCapabilities.TransportSecurity
                    | EventClientCapabilities.Authentication
            },
            {
                typeof(FileSystemEventClient),
                EventClientCapabilities.Payload
                    | EventClientCapabilities.ContentType
                    | EventClientCapabilities.ContentEncoding
                    | EventClientCapabilities.CustomProperties
                    | EventClientCapabilities.CloudEvents
            },
            { typeof(NullEventClient), 0 }
        };

        [Fact]
        public void ShippingClientInventoryIsComplete()
        {
            var expected = ShippingClients
                .Select(row => (Type)row[0])
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
            var actual = typeof(IEventClient).Assembly.ExportedTypes
                .Where(type => type is { IsClass: true, IsAbstract: false }
                    && typeof(IEventClient).IsAssignableFrom(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(ShippingClients))]
        public void ShippingClientDeclaresExactCapabilities(Type clientType,
            EventClientCapabilities expected)
        {
            Assert.True(typeof(IEventClientCapabilities).IsAssignableFrom(clientType));
            var client = Assert.IsAssignableFrom<IEventClientCapabilities>(
                RuntimeHelpers.GetUninitializedObject(clientType));

            Assert.Equal(expected, client.Capabilities);
        }

        [Fact]
        public void MqttV311DeclaresOnlyV311WireCapabilities()
        {
            Assert.Equal(EventClientCapabilities.Payload
                | EventClientCapabilities.Topic
                | EventClientCapabilities.QualityOfService
                | EventClientCapabilities.Retain
                | EventClientCapabilities.TransportSecurity
                | EventClientCapabilities.Authentication,
                MqttClientTransport.GetCapabilities(MqttVersion.v311));
        }

        [Fact]
        public void MqttDeclaresSchemaWhenSchemaRoutingIsConfigured()
        {
            Assert.True(MqttClientTransport
                .GetCapabilities(MqttVersion.v5, supportsSchema: true)
                .HasFlag(EventClientCapabilities.Schema));
        }

        [Fact]
        public void EventHubsDoesNotOverclaimGenericSchemaSupport()
        {
            var client = Assert.IsAssignableFrom<IEventClientCapabilities>(
                RuntimeHelpers.GetUninitializedObject(typeof(EventHubsClient)));

            Assert.False(client.Capabilities.HasFlag(EventClientCapabilities.Schema));
        }

        [Fact]
        public void DaprDoesNotClaimComponentSpecificMetadataSemantics()
        {
            var client = Assert.IsAssignableFrom<IEventClientCapabilities>(
                RuntimeHelpers.GetUninitializedObject(typeof(DaprPubSubClient)));
            var componentSpecific = EventClientCapabilities.QualityOfService
                | EventClientCapabilities.Retain
                | EventClientCapabilities.TimeToLive
                | EventClientCapabilities.ContentEncoding
                | EventClientCapabilities.CustomProperties;

            Assert.Equal((EventClientCapabilities)0,
                client.Capabilities & componentSpecific);
        }

        [Fact]
        public void FileSystemDoesNotClaimLosslessTopicPersistence()
        {
            var client = Assert.IsAssignableFrom<IEventClientCapabilities>(
                RuntimeHelpers.GetUninitializedObject(typeof(FileSystemEventClient)));

            Assert.False(client.Capabilities.HasFlag(EventClientCapabilities.Topic));
        }

        [Fact]
        public void OnlyMqttDeclaresRetainedTombstones()
        {
            var mqtt = Assert.IsAssignableFrom<IEventClientRetainedTombstoneCapabilities>(
                RuntimeHelpers.GetUninitializedObject(typeof(MqttClientTransport)));

            Assert.True(mqtt.SupportsRetainedTombstones);
            Assert.DoesNotContain(ShippingClients, entry =>
                !Equals(entry[0], typeof(MqttClientTransport))
                && typeof(IEventClientRetainedTombstoneCapabilities)
                    .IsAssignableFrom((Type)entry[0]));
        }

        [Fact]
        public void OnlyNetworkClientsDeclareTlsAndAuthentication()
        {
            var localClients = new HashSet<Type>
            {
                typeof(FileSystemEventClient),
                typeof(NullEventClient)
            };

            foreach (var row in ShippingClients)
            {
                var type = (Type)row[0];
                var capabilities = (EventClientCapabilities)row[1];
                var expected = !localClients.Contains(type);

                Assert.Equal(expected, capabilities.HasFlag(
                    EventClientCapabilities.TransportSecurity));
                Assert.Equal(expected, capabilities.HasFlag(
                    EventClientCapabilities.Authentication));
            }
        }
    }
}
