// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Asset
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    public sealed class ThingDescription
    {
        [JsonProperty("@context")]
#pragma warning disable CA1819 // Properties should not return arrays
        public object[]? Context { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("securityDefinitions")]
        public SecurityDefinitions? SecurityDefinitions { get; set; }

        [JsonProperty("security")]
#pragma warning disable CA1819 // Properties should not return arrays
        public string[]? Security { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        [JsonProperty("@type")]
#pragma warning disable CA1819 // Properties should not return arrays
        public string[]? Type { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("base")]
        public string? Base { get; set; }

        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("properties")]
#pragma warning disable CA2227 // Collection properties should be read only
        public Dictionary<string, Property>? Properties { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        // Actions
        // Events
        // Forms
    }

    public sealed class OpcUaNamespaces
    {
        [JsonProperty("opcua")]
#pragma warning disable CA1819 // Properties should not return arrays
        public Uri[]? Namespaces { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }

    public sealed class Property
    {
        [JsonProperty("type")]
        public TypeEnum Type { get; set; }

        [JsonProperty("opcua:nodeId")]
        public string? OpcUaNodeId { get; set; }

        [JsonProperty("readOnly")]
        public bool ReadOnly { get; set; }

        [JsonProperty("observable")]
        public bool Observable { get; set; }

        [JsonProperty("forms")]
#pragma warning disable CA1819 // Properties should not return arrays
        public object[]? Forms { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum Op
    {
        [EnumMember(Value = "readproperty")]
        ReadProperty,
        [EnumMember(Value = "writeproperty")]
        WriteProperty,
        [EnumMember(Value = "observeproperty")]
        ObserveProperty,
        [EnumMember(Value = "unobserveproperty")]
        UnobserveProperty,
    }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum TypeEnum
    {
        [EnumMember(Value = "number")]
        Number
    }

    public abstract class Form
    {
        [JsonProperty("href")]
        public string? Href { get; set; }
        [JsonProperty("op")]
#pragma warning disable CA1819 // Properties should not return arrays
        public Op[]? Op { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }

    public sealed class SecurityDefinitions
    {
        [JsonProperty("nosec_sc")]
        public NosecSc? NosecSc { get; set; }
    }

    public sealed class NosecSc
    {
        [JsonProperty("scheme")]
        public string? Scheme { get; set; }
    }
}
