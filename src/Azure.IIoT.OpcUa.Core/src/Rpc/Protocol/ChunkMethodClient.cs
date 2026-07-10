// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Protocol
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Azure.IIoT.OpcUa.Core.Rpc.Models;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Chunked method provide reliable any size send/receive
    /// </summary>
    [SuppressMessage("Trimming", "IL2026",
        Justification = "Reflection based serializer, hardened in a later phase.")]
    [SuppressMessage("AotAnalysis", "IL3050",
        Justification = "Reflection based serializer, hardened in a later phase.")]
    public sealed class ChunkMethodClient : IMethodClient
    {
        /// <summary>
        /// Max chunk size
        /// </summary>
        public int MaxChunkLength { get; }

        /// <summary>
        /// Create client wrapping a json method client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public ChunkMethodClient(IRpcClient client,
            ILogger<ChunkMethodClient> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            //
            // assume base64 encoding is 33% reduction compared to raw bytes
            // plus the additional overhead of the model payload.
            //
            MaxChunkLength = (int)(_client.MaxMethodPayloadSizeInBytes * 0.66);
            if (MaxChunkLength == 0)
            {
                MaxChunkLength = 1;
            }
        }

        /// <inheritdoc/>
        public async ValueTask<ReadOnlyMemory<byte>> CallMethodAsync(string target,
            string method, ReadOnlyMemory<byte> payload, string contentType, TimeSpan? timeout,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentNullException(nameof(method));
            }
            if (payload.Length == 0)
            {
                payload = " "u8.ToArray();
            }
            contentType ??= ContentMimeType.Json;

            using var activity = kActivity.CreateActivity(target + method, ActivityKind.Client);

            // Send chunks
            var buffer = payload.ToArray().Zip(); // Gzip payload
            var status = 200;
            var received = new MemoryStream();
            await using (received.ConfigureAwait(false))
            {
                string? handle = null;
                for (var offset = 0; offset < buffer.Length; offset += MaxChunkLength)
                {
                    var length = Math.Min(buffer.Length - offset, MaxChunkLength);
                    var chunk = buffer.AsSpan(offset, length).ToArray();
                    MethodChunkModel chunkModel;
                    if (offset == 0)
                    {
                        chunkModel = new MethodChunkModel
                        {
                            Timeout = timeout,
                            MethodName = method,
                            ContentType = contentType,
                            ContentLength = buffer.Length,
                            MaxChunkLength = MaxChunkLength,
                            Payload = chunk
                        };
                        DistributedContextPropagator.Current.Inject(
                            activity, chunkModel, InjectProperties);
                    }
                    else
                    {
                        chunkModel = new MethodChunkModel
                        {
                            Handle = handle,
                            Payload = chunk
                        };
                    }
                    var result = await _client.CallAsync(target,
                        MethodNames.Call, Json.SerializeToMemory(chunkModel),
                        ContentMimeType.Json, timeout, ct).ConfigureAwait(false);
                    var response = Json.Deserialize<MethodChunkModel>(result);
                    if (response?.Payload != null)
                    {
                        received.Write(response.Payload);
                    }
                    if (response?.Status != null)
                    {
                        status = response.Status.Value;
                    }
                    handle = response?.Handle;
                }
                // Receive all responses
                while (!string.IsNullOrEmpty(handle))
                {
                    var chunkModel = new MethodChunkModel
                    {
                        Handle = handle,
                    };
                    var result = await _client.CallAsync(target,
                        MethodNames.Call, Json.SerializeToMemory(chunkModel),
                        ContentMimeType.Json, timeout, ct).ConfigureAwait(false);
                    var response = Json.Deserialize<MethodChunkModel>(result);
                    if (response?.Payload != null)
                    {
                        received.Write(response.Payload);
                    }
                    if (response?.Status != null)
                    {
                        status = response.Status.Value;
                    }
                    handle = response?.Handle;
                }
                var responsePayload = received.ToArray().Unzip();
                if (status != 200)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.ChunkedCallFailed(method, target, payload, status, AsString(responsePayload));
                    }
                    MethodCallStatusException.Throw(responsePayload, status);
                }
                return responsePayload;
            }
        }

        /// <summary>
        /// Convert to string
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static string AsString(byte[] buffer)
        {
            try
            {
                return buffer == null ? string.Empty : Encoding.UTF8.GetString(buffer);
            }
            catch
            {
                return Convert.ToBase64String(buffer);
            }
        }

        /// <summary>
        /// Inject properties
        /// </summary>
        /// <param name="carrier"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private static void InjectProperties(object? carrier, string key, string value)
        {
            if (carrier is MethodChunkModel chunkModel)
            {
                chunkModel.Properties ??= new Dictionary<string, string>();
                chunkModel.Properties[key] = value;
            }
        }

        private static readonly ActivitySource kActivity = new(typeof(ChunkMethodClient).FullName!);
        private readonly IRpcClient _client;
        private readonly ILogger _logger;
    }

    /// <summary>
    /// Source-generated logging for ChunkMethodClient
    /// </summary>
    internal static partial class ChunkMethodClientLogging
    {
        private const int EventClass = 0;

        [LoggerMessage(EventId = EventClass + 0, Level = LogLevel.Debug,
            Message = "Chunked call on {Method} on {Target} with {Payload} returned with error {Status}: {Result}")]
        public static partial void ChunkedCallFailed(this ILogger logger, string method, string target,
            ReadOnlyMemory<byte> payload, int status, string result);
    }
}
