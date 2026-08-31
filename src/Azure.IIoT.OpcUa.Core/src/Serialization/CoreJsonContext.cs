// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Serialization
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Rpc.Models;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Source-generated <see cref="JsonSerializerContext"/> for the core
    /// foundation types. Using a compile-time context keeps serialization
    /// Native-AOT and trim safe (no reflection-based metadata discovery).
    /// </summary>
    [JsonSourceGenerationOptions(
        AllowTrailingCommas = true,
        DefaultBufferSize = 128,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        MaxDepth = 64,
        NumberHandling = JsonNumberHandling.AllowReadingFromString |
            JsonNumberHandling.AllowNamedFloatingPointLiterals,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(ErrorDetails))]
    [JsonSerializable(typeof(MethodChunkModel))]
    [JsonSerializable(typeof(IReadOnlySet<string>))]
    [JsonSerializable(typeof(bool[,]))]
    [JsonSerializable(typeof(byte[,]))]
    [JsonSerializable(typeof(short[,]))]
    [JsonSerializable(typeof(int[,]))]
    [JsonSerializable(typeof(long[,]))]
    [JsonSerializable(typeof(float[,]))]
    [JsonSerializable(typeof(double[,]))]
    [JsonSerializable(typeof(string[,]))]
    internal sealed partial class CoreJsonContext : JsonSerializerContext
    {
    }
}
