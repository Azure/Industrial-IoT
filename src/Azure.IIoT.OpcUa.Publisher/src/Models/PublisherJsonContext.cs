// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.Iot.Operations.Services.AssetAndDeviceRegistry.Models;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Text.Json.Serialization;

    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        GenerationMode = JsonSourceGenerationMode.Metadata,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        UseStringEnumConverter = true)]
    [JsonSerializable(typeof(DeviceEndpointConfiguration))]
    [JsonSerializable(typeof(DataSetEventConfiguration))]
    [JsonSerializable(typeof(DataSetConfiguration))]
    [JsonSerializable(typeof(DataSetDataPointConfiguration))]
    [JsonSerializable(typeof(EventGroupConfiguration))]
    [JsonSerializable(typeof(EventConfiguration))]
    [JsonSerializable(typeof(EventDataPointConfiguration))]
    [JsonSerializable(typeof(ManagementGroupConfiguration))]
    [JsonSerializable(typeof(ActionConfiguration))]
    [JsonSerializable(typeof(DiscoveredAsset))]
    [JsonSerializable(typeof(DiscoveredDevice))]
    [JsonSerializable(typeof(Azure.IIoT.OpcUa.Core.Messaging.QoS),
        TypeInfoPropertyName = "CoreQoS")]
    [JsonSerializable(typeof(Azure.IIoT.OpcUa.Core.Messaging.QoS?),
        TypeInfoPropertyName = "CoreNullableQoS")]
    [JsonSerializable(typeof(QoS), TypeInfoPropertyName = "AssetQoS")]
    [JsonSerializable(typeof(QoS?), TypeInfoPropertyName = "AssetNullableQoS")]
    internal sealed partial class PublisherJsonContext : JsonSerializerContext
    {
    }

    internal static class PublisherJsonRegistration
    {
        [ModuleInitializer]
        [SuppressMessage("Usage", "CA2255",
            Justification = "The module initializer registers this assembly's " +
                "source-generated metadata before its contracts are serialized.")]
        internal static void Register()
        {
            Json.RegisterTypeInfoResolver(PublisherJsonContext.Default);
        }
    }
}
