﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.Services;

using Opc.Ua;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service extensions
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Reads a byte string value safely in fragments if needed. Uses the byte
    /// string size limits to chunk the reads if needed. The first read happens
    /// as usual and no stream is allocated, if the result is below the limits
    /// the buffer that is read into is returned, otherwise buffers are added
    /// to a memory stream whose content is finally returned.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="variableId"></param>
    /// <param name="maxByteStringLength"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="ServiceResultException"></exception>
    public static async ValueTask<ReadOnlyMemory<byte>> ReadBytesAsync(
        this IAttributeServiceSet services, NodeId variableId,
        int maxByteStringLength = 0, CancellationToken ct = default)
    {
        if (maxByteStringLength == 0)
        {
            maxByteStringLength = DefaultEncodingLimits.MaxByteStringLength;
        }
        var offset = 0;
        MemoryStream? stream = null;
        try
        {
            while (true)
            {
                var valueToRead = new ReadValueId
                {
                    NodeId = variableId,
                    AttributeId = Attributes.Value,
                    IndexRange = new NumericRange(offset,
                        offset + maxByteStringLength - 1).ToString(),
                    DataEncoding = null
                };

                var readValueIds = new ReadValueIdCollection { valueToRead };
                var response = await services.ReadAsync(null, 0, TimestampsToReturn.Neither,
                    readValueIds, ct).ConfigureAwait(false);
                ClientBase.ValidateResponse(response.Results, readValueIds);
                ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, readValueIds);
                var wrappedValue = response.Results[0].WrappedValue;
                if (wrappedValue.TypeInfo.BuiltInType != BuiltInType.ByteString ||
                    wrappedValue.TypeInfo.ValueRank != ValueRanks.Scalar)
                {
                    throw new ServiceResultException(StatusCodes.BadTypeMismatch,
                        "Value is not a ByteString scalar.");
                }
                if (StatusCode.IsBad(response.Results[0].StatusCode))
                {
                    if (response.Results[0].StatusCode == StatusCodes.BadIndexRangeNoData)
                    {
                        // this happens when the previous read has fetched all remaining data
                        break;
                    }
                    var serviceResult = ClientBase.GetResult(response.Results[0].StatusCode,
                        0, response.DiagnosticInfos, response.ResponseHeader);
                    throw new ServiceResultException(serviceResult);
                }
                if (response.Results[0].Value is not byte[] chunk || chunk.Length == 0)
                {
                    break;
                }
                if (chunk.Length < maxByteStringLength && offset == 0)
                {
                    // Done
                    return chunk;
                }
                stream ??= new MemoryStream();
                await stream.WriteAsync(chunk, ct).ConfigureAwait(false);
                if (chunk.Length < maxByteStringLength)
                {
                    // Done
                    break;
                }
                offset += maxByteStringLength;
            }
            return stream?.ToArray() ?? [];
        }
        finally
        {
            if (stream != null)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
