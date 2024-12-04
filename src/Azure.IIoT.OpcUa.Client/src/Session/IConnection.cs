// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Control session connectivity
/// </summary>
internal interface IConnection
{
    /// <summary>
    /// Create or recreate the session
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    ValueTask OpenAsync(CancellationToken ct = default);

    /// <summary>
    /// Reconnect the session
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    ValueTask ReconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Close the session
    /// </summary>
    /// <param name="closeChannel"></param>
    /// <param name="deleteSubscriptions"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    ValueTask<ServiceResult> CloseAsync(bool closeChannel,
        bool deleteSubscriptions, CancellationToken ct = default);
}
