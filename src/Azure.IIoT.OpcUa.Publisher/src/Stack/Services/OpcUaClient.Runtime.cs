// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Core.Utils;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed partial class OpcUaClient
    {
        ChannelDiagnosticModel IOpcUaClientRuntime.LastDiagnostics => LastDiagnostics;

        Task<ISessionHandle> IOpcUaClientRuntime.AcquireAsync(int? connectTimeout,
            int? serviceCallTimeout, CancellationToken ct)
        {
            return AcquireAsync(connectTimeout, serviceCallTimeout, ct);
        }

        Task<T> IOpcUaClientRuntime.RunAsync<T>(Func<ServiceCallContext, Task<T>> service,
            int? connectTimeout, int? serviceCallTimeout, CancellationToken ct)
        {
            return RunAsync(service, connectTimeout, serviceCallTimeout, ct);
        }

        IAsyncEnumerable<T> IOpcUaClientRuntime.RunAsync<T>(AsyncEnumerableBase<T> operation,
            int? connectTimeout, int? serviceCallTimeout, CancellationToken ct)
        {
            return RunAsync(operation, connectTimeout, serviceCallTimeout, ct);
        }

        async ValueTask<ISubscription> IOpcUaClientRuntime.RegisterAsync(
            SubscriptionModel subscription, ISubscriber subscriber, CancellationToken ct)
        {
            return await RegisterAsync(subscription, subscriber, ct).ConfigureAwait(false);
        }

        Task IOpcUaClientRuntime.ResetAsync(CancellationToken ct)
        {
            return ResetAsync(ct);
        }

        Task<SessionDiagnosticsModel?> IOpcUaClientRuntime.GetSessionDiagnosticsAsync(
            CancellationToken ct)
        {
            return GetSessionDiagnosticsAsync(ct);
        }

        ValueTask IOpcUaClientRuntime.CloseAsync(bool shutdown, bool fromManagementLoop)
        {
            return CloseAsync(shutdown, fromManagementLoop);
        }

        bool IOpcUaClientRuntime.TryAddRef()
        {
            if (_disposed)
            {
                return false;
            }
            AddRef();
            if (!_disposed)
            {
                return true;
            }
            Release();
            return false;
        }

        void IOpcUaClientRuntime.AddRef(string? token, TimeSpan? expiresAfter)
        {
            AddRef(token, expiresAfter);
        }
    }

    /// <summary>
    /// Classic rollback runtime retained for explicit direct construction.
    /// </summary>
    internal sealed class ClassicOpcUaClientRuntimeStrategy : IOpcUaClientRuntimeStrategy
    {
        public static ClassicOpcUaClientRuntimeStrategy Instance { get; } = new();

        public IOpcUaClientRuntime Create(OpcUaClientRuntimeContext context)
        {
            return new OpcUaClient(context.Configuration, context.Connection,
                context.LoggerFactory, context.TimeProvider, context.Metrics,
                context.OnClose, context.Notifier,
                context.Connection.Connection.IsReverseConnect() ?
                    context.ReverseConnectManager : null,
                context.DiagnosticsCallback, context.ClientOptions,
                context.SubscriptionOptions, endpointSelector: context.EndpointSelector);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        private ClassicOpcUaClientRuntimeStrategy()
        {
        }
    }
}
