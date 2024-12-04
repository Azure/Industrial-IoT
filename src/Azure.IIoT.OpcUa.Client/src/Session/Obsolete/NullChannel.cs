// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.Obsolete;

using Opc.Ua.Bindings;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Stub to make the underlying stack happy.
/// </summary>
internal sealed class NullChannel : ITransportChannel
{
    /// <inheritdoc/>
    public TransportChannelFeatures SupportedFeatures
        => throw NotSupported(nameof(SupportedFeatures));

    /// <inheritdoc/>
    public EndpointDescription EndpointDescription
        => throw NotSupported(nameof(EndpointDescription));

    /// <inheritdoc/>
    public EndpointConfiguration EndpointConfiguration
        => throw NotSupported(nameof(EndpointConfiguration));

    /// <inheritdoc/>
    public IServiceMessageContext MessageContext
        => throw NotSupported(nameof(MessageContext));

    /// <inheritdoc/>
    public ChannelToken CurrentToken
        => throw NotSupported(nameof(CurrentToken));

    /// <inheritdoc/>
    public int OperationTimeout { get; set; }

    /// <inheritdoc/>
    public IAsyncResult BeginClose(AsyncCallback callback,
        object callbackData)
    {
        throw NotSupported(nameof(BeginClose));
    }

    /// <inheritdoc/>
    public IAsyncResult BeginOpen(AsyncCallback callback,
        object callbackData)
    {
        throw NotSupported(nameof(BeginOpen));
    }

    /// <inheritdoc/>
    public IAsyncResult BeginReconnect(AsyncCallback callback,
        object callbackData)
    {
        throw NotSupported(nameof(BeginReconnect));
    }

    /// <inheritdoc/>
    public IAsyncResult BeginSendRequest(IServiceRequest request,
        AsyncCallback callback, object callbackData)
    {
        throw NotSupported(nameof(BeginSendRequest));
    }

    /// <inheritdoc/>
    public void Close()
    {
        throw NotSupported(nameof(Close));
    }

    /// <inheritdoc/>
    public Task CloseAsync(CancellationToken ct)
    {
        throw NotSupported(nameof(CloseAsync));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Do nothing
    }

    /// <inheritdoc/>
    public void EndClose(IAsyncResult result)
    {
        throw NotSupported(nameof(EndClose));
    }

    public void EndOpen(IAsyncResult result)
    {
        throw NotSupported(nameof(EndOpen));
    }

    /// <inheritdoc/>
    public void EndReconnect(IAsyncResult result)
    {
        throw NotSupported(nameof(EndReconnect));
    }

    /// <inheritdoc/>
    public IServiceResponse EndSendRequest(IAsyncResult result)
    {
        throw NotSupported(nameof(EndSendRequest));
    }

    /// <inheritdoc/>
    public Task<IServiceResponse> EndSendRequestAsync(
        IAsyncResult result, CancellationToken ct)
    {
        throw NotSupported(nameof(EndSendRequestAsync));
    }

    /// <inheritdoc/>
    public void Initialize(Uri url, TransportChannelSettings settings)
    {
        throw NotSupported(nameof(Initialize));
    }

    /// <inheritdoc/>
    public void Initialize(ITransportWaitingConnection connection,
        TransportChannelSettings settings)
    {
        throw NotSupported(nameof(Initialize));
    }

    /// <inheritdoc/>
    public void Open()
    {
        throw NotSupported(nameof(Open));
    }

    /// <inheritdoc/>
    public void Reconnect()
    {
        throw NotSupported(nameof(Reconnect));
    }

    /// <inheritdoc/>
    public void Reconnect(ITransportWaitingConnection connection)
    {
        throw NotSupported(nameof(Reconnect));
    }

    /// <inheritdoc/>
    public IServiceResponse SendRequest(IServiceRequest request)
    {
        throw NotSupported(nameof(SendRequest));
    }

    /// <inheritdoc/>
    public Task<IServiceResponse> SendRequestAsync(IServiceRequest request,
        CancellationToken ct)
    {
        throw NotSupported(nameof(SendRequestAsync));
    }

    /// <summary>
    /// Throw not supported exception
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private static ServiceResultException NotSupported(string name)
    {
        Debug.Fail(name + " not supported");
        return ServiceResultException.Create(StatusCodes.BadNotSupported,
            name + " deprecated");
    }
}
