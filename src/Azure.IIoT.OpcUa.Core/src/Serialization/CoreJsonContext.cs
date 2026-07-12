// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Serialization
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Rpc.Models;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Source-generated <see cref="JsonSerializerContext"/> for the core
    /// foundation types. Using a compile-time context keeps serialization
    /// Native-AOT and trim safe (no reflection-based metadata discovery).
    /// </summary>
    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(ErrorDetails))]
    [JsonSerializable(typeof(MethodChunkModel))]
    internal sealed partial class CoreJsonContext : JsonSerializerContext
    {
    }
}
