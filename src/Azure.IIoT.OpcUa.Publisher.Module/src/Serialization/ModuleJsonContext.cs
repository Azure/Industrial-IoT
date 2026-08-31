// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Serialization
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Microsoft.AspNetCore.Mvc;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        GenerationMode = JsonSourceGenerationMode.Metadata,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(ProblemDetails))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(Dictionary<string, JsonElement>))]
    [JsonSerializable(typeof(IDictionary<string, object>))]
    [JsonSerializable(typeof(string))]
    internal sealed partial class ModuleJsonContext : JsonSerializerContext
    {
    }

    internal static class ModuleJsonRegistration
    {
        [ModuleInitializer]
        [SuppressMessage("Usage", "CA2255",
            Justification = "Registers source-generated HTTP error metadata before " +
                "the minimal API JSON options are composed.")]
        internal static void Register()
        {
            Json.RegisterTypeInfoResolver(ModuleJsonContext.Default);
        }
    }
}
