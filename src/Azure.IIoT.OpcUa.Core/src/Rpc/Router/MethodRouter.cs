// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Router
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Azure.IIoT.OpcUa.Core.Rpc.Protocol;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides request routing to module controllers
    /// </summary>
    [SuppressMessage("Trimming", "IL2026",
        Justification = "Reflection based router, hardened in a later phase.")]
    [SuppressMessage("Trimming", "IL2070",
        Justification = "Reflection based router, hardened in a later phase.")]
    [SuppressMessage("Trimming", "IL2072",
        Justification = "Reflection based router, hardened in a later phase.")]
    [SuppressMessage("Trimming", "IL2075",
        Justification = "Reflection based router, hardened in a later phase.")]
    [SuppressMessage("AotAnalysis", "IL3050",
        Justification = "Reflection based router, hardened in a later phase.")]
    public sealed class MethodRouter : IRpcHandler, IAwaitable<MethodRouter>,
        IDisposable, IAsyncDisposable
    {
        /// <inheritdoc/>
        public string MountPoint { get; }

        /// <summary>
        /// Property Di to prevent circular dependency between host and controller
        /// </summary>
#pragma warning disable CA1044 // Properties should not be write only
        public IEnumerable<IMethodController> Controllers
#pragma warning restore CA1044 // Properties should not be write only
        {
            set
            {
                foreach (var controller in value)
                {
                    AddToCallTable(controller);
                }
            }
        }

        /// <summary>
        /// Property Di to prevent circular dependency between host and invoker
        /// </summary>
#pragma warning disable CA1044 // Properties should not be write only
        public IEnumerable<IMethodInvoker> ExternalInvokers
#pragma warning restore CA1044 // Properties should not be write only
        {
            set
            {
                foreach (var invoker in value)
                {
                    _chunks.Add(invoker);
                }
            }
        }

        /// <summary>
        /// Create router
        /// </summary>
        /// <param name="servers"></param>
        /// <param name="logger"></param>
        /// <param name="summarizer"></param>
        /// <param name="options"></param>
        /// <param name="timeProvider"></param>
        public MethodRouter(IEnumerable<IRpcServer> servers,
            ILogger<MethodRouter> logger, IExceptionSummarizer? summarizer = null,
            IOptions<RouterOptions>? options = null, TimeProvider? timeProvider = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _summarizer = summarizer;

            MountPoint = options?.Value.MountPoint ?? string.Empty;

            // Create chunk server always
            _chunks = new ChunkMethodServer(logger,
                options?.Value.ChunkTimeout ?? TimeSpan.FromSeconds(30),
                timeProvider ?? TimeProvider.System, MountPoint);
            _connections = ConnectAsync(servers);
        }

        /// <inheritdoc/>
        public ValueTask<ReadOnlySequence<byte>> InvokeAsync(string method,
            ReadOnlySequence<byte> payload, string contentType, CancellationToken ct)
        {
            return _chunks.InvokeAsync(method, payload, contentType, ct);
        }

        /// <inheritdoc/>
        public IAwaiter<MethodRouter> GetAwaiter()
        {
            return _connections.AsAwaiter(this);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            _chunks.Dispose();
            var connections = await _connections.ConfigureAwait(false);
            try
            {
                await DisposeAsync(connections).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.FailedToDispose(ex.Message);
            }
            static Task DisposeAsync(List<IAsyncDisposable> connections) =>
                Task.WhenAll(connections.Select(async c => await c.DisposeAsync().ConfigureAwait(false)));
        }

        /// <summary>
        /// Create connection to servers
        /// </summary>
        /// <param name="servers"></param>
        /// <returns></returns>
        private async Task<List<IAsyncDisposable>> ConnectAsync(IEnumerable<IRpcServer> servers)
        {
            var disposables = new List<IAsyncDisposable>();
            foreach (var server in servers)
            {
                try
                {
                    var connection = await server.ConnectAsync(this).ConfigureAwait(false);
                    disposables.Add(connection);
                }
                catch (Exception ex)
                {
                    _logger.FailedToConnect(ex);
                }
            }
            if (disposables.Count == 0)
            {
                _logger.NotConnectedToServer();
            }
            return disposables;
        }

        /// <summary>
        /// Add target to calltable
        /// </summary>
        /// <param name="target"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void AddToCallTable(object target)
        {
            var versions = target.GetType().GetCustomAttributes<VersionAttribute>(true)
                .Select(v => v.Value)
                .ToList();
            if (versions.Count == 0)
            {
                versions.Add(string.Empty);
            }
            foreach (var methodInfo in target.GetType().GetMethods())
            {
                if (methodInfo.GetCustomAttribute<IgnoreAttribute>() != null)
                {
                    // Should be ignored
                    continue;
                }

                var returnType = methodInfo.ReturnType;
                if (!returnType.IsGenericType)
                {
                    if (returnType != typeof(Task) &&
                        returnType != typeof(ValueTask))
                    {
                        // must be task or valuetask
                        continue;
                    }
                }
                else
                {
                    returnType = returnType.GetGenericTypeDefinition();
                    if (returnType != typeof(IAsyncEnumerable<>) &&
                        returnType != typeof(Task<>) &&
                        returnType != typeof(ValueTask<>))
                    {
                        continue;
                    }
                    if (returnType.GetGenericArguments().Length > 1)
                    {
                        // must have exactly  one (serializable) type
                        continue;
                    }
                }

                var name = methodInfo.Name;
                if (name.EndsWith("Async", StringComparison.Ordinal))
                {
                    name = name[..^5];
                }

                // Register for all defined versions
                foreach (var version in versions)
                {
                    var versionedName = name + version;
                    if (!_chunks.TryGetValue(versionedName, out var invoker))
                    {
                        invoker = new DynamicInvoker(_logger, name, _summarizer);
                        _chunks.Add(versionedName, invoker);
                    }
                    if (invoker is DynamicInvoker dynamicInvoker)
                    {
                        dynamicInvoker.Add(target, methodInfo);
                    }
                    else
                    {
                        // Should never happen...
                        throw new InvalidOperationException(
                            $"Cannot add {versionedName} since invoker is private.");
                    }
                }
            }
        }

        /// <summary>
        /// Encapsulates invoking a matching service on the controller
        /// </summary>
        private class DynamicInvoker : IMethodInvoker
        {
            /// <inheritdoc/>
            public string MethodName { get; private set; }

            /// <summary>
            /// Create dynamic invoker
            /// </summary>
            /// <param name="logger"></param>
            /// <param name="methodName"></param>
            /// <param name="summarizer"></param>
            public DynamicInvoker(ILogger logger, string methodName, IExceptionSummarizer? summarizer)
            {
                MethodName = methodName;
                _logger = logger;
                _summarizer = summarizer;
                _invokers = [];
            }

            /// <summary>
            /// Add invoker
            /// </summary>
            /// <param name="controller"></param>
            /// <param name="controllerMethod"></param>
            public void Add(object controller, MethodInfo controllerMethod)
            {
                _logger.AddingMethod(controller.GetType().Name, controllerMethod.Name);
                _invokers.Add(new JsonMethodInvoker(controller, controllerMethod, _logger, _summarizer));
                MethodName = controllerMethod.Name;
            }

            /// <inheritdoc/>
            public async ValueTask<ReadOnlyMemory<byte>> InvokeAsync(ReadOnlyMemory<byte> payload,
                string contentType, IRpcHandler handler, CancellationToken ct)
            {
                Exception? e = null;
                foreach (var invoker in _invokers)
                {
                    try
                    {
                        return await invoker.InvokeAsync(payload, contentType,
                            handler, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Save last, and continue
                        e = ex;
                    }
                }
                _logger.InvocationException(e);
                throw e!;
            }

            private readonly ILogger _logger;
            private readonly IExceptionSummarizer? _summarizer;
            private readonly List<JsonMethodInvoker> _invokers;
        }

        /// <summary>
        /// Invokes a method with json payload
        /// </summary>
        private class JsonMethodInvoker : IMethodInvoker
        {
            /// <inheritdoc/>
            public string MethodName => _controllerMethod.Name;

            /// <summary>
            /// Default filter implementation if none is specified
            /// </summary>
            private sealed class DefaultFilter : ExceptionFilterAttribute
            {
                public override Exception Filter(Exception exception, out int status)
                {
                    status = 400;
                    return exception;
                }
            }

            /// <summary>
            /// Create invoker
            /// </summary>
            /// <param name="controller"></param>
            /// <param name="controllerMethod"></param>
            /// <param name="logger"></param>
            /// <param name="summarizer"></param>
            [UnconditionalSuppressMessage("Trimming", "IL2075",
                Justification = "Result accessors are resolved on closed framework " +
                    "generic types (Task<T>, ValueTask<T>, IAsyncEnumerable<T>) whose " +
                    "members are always preserved; no dynamic code is generated, so " +
                    "this is Native-AOT safe.")]
            public JsonMethodInvoker(object controller, MethodInfo controllerMethod,
                ILogger logger, IExceptionSummarizer? summarizer)
            {
                _logger = logger;
                _summarizer = summarizer;
                _controller = controller;
                _controllerMethod = controllerMethod;
                _methodParams = _controllerMethod.GetParameters();
                _ef = _controllerMethod.GetCustomAttribute<ExceptionFilterAttribute>(true) ??
                    controller.GetType().GetCustomAttribute<ExceptionFilterAttribute>(true) ??
                    new DefaultFilter();

                var returnType = _controllerMethod.ReturnParameter.ParameterType;
                if (!returnType.IsGenericType)
                {
                    _resultKind = returnType == typeof(ValueTask)
                        ? ResultKind.VoidValueTask : ResultKind.VoidTask;
                    return;
                }

                Debug.Assert(returnType.GetGenericArguments().Length == 1);

                var returnDefinition = returnType.GetGenericTypeDefinition();
                if (returnDefinition == typeof(IAsyncEnumerable<>))
                {
                    _resultKind = ResultKind.AsyncEnumerable;
                    // Closed IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken).
                    // Resolving a member on the closed type needs no MakeGenericMethod
                    // (no runtime code generation), so it is Native-AOT safe.
                    _getAsyncEnumerator = returnType.GetMethod("GetAsyncEnumerator")!;
                }
                else if (returnDefinition == typeof(ValueTask<>))
                {
                    _resultKind = ResultKind.TypedValueTask;
                    // Closed ValueTask<T>.AsTask() - no MakeGenericMethod / dynamic code.
                    _asTask = returnType.GetMethod("AsTask")!;
                }
                else
                {
                    Debug.Assert(returnDefinition == typeof(Task<>));
                    _resultKind = ResultKind.TypedTask;
                    // Closed Task<T>.Result - no MakeGenericMethod / dynamic code.
                    _resultProperty = returnType.GetProperty("Result")!;
                }
            }

            /// <summary>
            /// Shape of the controller method result, resolved once at
            /// registration so the per-call path needs no MakeGenericMethod.
            /// </summary>
            private enum ResultKind
            {
                VoidTask,
                VoidValueTask,
                TypedTask,
                TypedValueTask,
                AsyncEnumerable
            }

            /// <inheritdoc/>
            public async ValueTask<ReadOnlyMemory<byte>> InvokeAsync(ReadOnlyMemory<byte> payload,
                string contentType, IRpcHandler handler, CancellationToken ct)
            {
                object task;
                try
                {
                    object?[] GetInputArguments()
                    {
                        if (_methodParams.Length == 0)
                        {
                            return [];
                        }

                        if (_methodParams.Length == 1)
                        {
                            if (_methodParams[0].ParameterType == typeof(CancellationToken))
                            {
                                return [ct];
                            }
                            var singleParam = Json.Deserialize(payload,
                                _methodParams[0].ParameterType);
                            return [singleParam];
                        }

                        if ((_methodParams.Length == 2) &&
                            _methodParams[0].ParameterType != _methodParams[1].ParameterType &&
                            (_methodParams[1].ParameterType == typeof(CancellationToken) ||
                             _methodParams[0].ParameterType == typeof(CancellationToken)))
                        {
                            if (_methodParams[1].ParameterType == typeof(CancellationToken))
                            {
                                var singleParam = Json.Deserialize(payload,
                                    _methodParams[0].ParameterType);
                                return [singleParam, ct];
                            }
                            else
                            {
                                var singleParam = Json.Deserialize(payload,
                                    _methodParams[1].ParameterType);
                                return [ct, singleParam];
                            }
                        }

                        var data = Json.Parse(payload) as JsonObject;
                        return _methodParams.Select(param =>
                        {
                            if (param.ParameterType == typeof(CancellationToken))
                            {
                                return ct;
                            }
                            if (data != null &&
                                data.TryGetPropertyValue(param.Name!, out var value))
                            {
                                return value?.Deserialize(param.ParameterType, Json.Options);
                            }
                            return param.HasDefaultValue ? param.DefaultValue : null;
                        }).ToArray();
                    }
                    task = _controllerMethod.Invoke(_controller, GetInputArguments())!;
                }
                catch (Exception e)
                {
                    // Argument binding / synchronous failure before the async
                    // method returned its task. Surface it like a faulted task.
                    ThrowAsMethodCallStatusException(e);
                    throw; // unreachable, ThrowAsMethodCallStatusException does not return
                }

                switch (_resultKind)
                {
                    case ResultKind.VoidTask:
                        return await VoidTaskContinuationAsync((Task)task)
                            .ConfigureAwait(false);
                    case ResultKind.VoidValueTask:
                        return await VoidValueTaskAsync((ValueTask)task)
                            .ConfigureAwait(false);
                    default:
                        return await ConvertTypedResultAsync(task)
                            .ConfigureAwait(false);
                }
            }

            /// <summary>
            /// Convert a typed (Task&lt;T&gt;, ValueTask&lt;T&gt; or
            /// IAsyncEnumerable&lt;T&gt;) controller result into a serialized
            /// buffer or throw the appropriate exception. The typed result is
            /// extracted through member access on the closed return type, which -
            /// unlike the former MakeGenericMethod continuation - does not require
            /// runtime code generation and is therefore Native-AOT safe.
            /// </summary>
            /// <param name="task"></param>
            /// <returns></returns>
            [UnconditionalSuppressMessage("Trimming", "IL2075",
                Justification = "Controllers are rooted via DI so the result " +
                    "member metadata is preserved; no dynamic code is generated.")]
            [UnconditionalSuppressMessage("Trimming", "IL2072",
                Justification = "Controllers are rooted via DI so the result " +
                    "member metadata is preserved; no dynamic code is generated.")]
            private async Task<ReadOnlyMemory<byte>> ConvertTypedResultAsync(object task)
            {
                object? result;
                try
                {
                    switch (_resultKind)
                    {
                        case ResultKind.TypedTask:
                            await ((Task)task).ConfigureAwait(false);
                            result = _resultProperty!.GetValue(task);
                            break;
                        case ResultKind.TypedValueTask:
                            var asTask = (Task)_asTask!.Invoke(task, null)!;
                            await asTask.ConfigureAwait(false);
                            result = _asTask.ReturnType.GetProperty("Result")!
                                .GetValue(asTask);
                            break;
                        case ResultKind.AsyncEnumerable:
                            result = await DrainAsync(task).ConfigureAwait(false);
                            break;
                        default:
                            result = null;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ThrowAsMethodCallStatusException(ex);
                    throw; // unreachable
                }
                return Json.SerializeToMemory(result);
            }

            /// <summary>
            /// Drain an IAsyncEnumerable&lt;T&gt; (boxed as object) into a list
            /// using member access on the closed enumerator type. No
            /// MakeGenericType/MakeGenericMethod is used, so the path is
            /// Native-AOT safe.
            /// </summary>
            /// <param name="asyncEnumerable"></param>
            /// <returns></returns>
            [UnconditionalSuppressMessage("Trimming", "IL2075",
                Justification = "The async enumerator members are preserved; " +
                    "controllers are rooted via DI. No dynamic code is generated.")]
            private async Task<List<object?>> DrainAsync(object asyncEnumerable)
            {
                var enumerator = _getAsyncEnumerator!.Invoke(asyncEnumerable,
                    [CancellationToken.None])!;
                // Resolve members on the closed IAsyncEnumerator<T> interface (the
                // GetAsyncEnumerator return type) rather than the compiler-generated
                // state machine, where they are explicit interface implementations.
                var enumeratorType = _getAsyncEnumerator.ReturnType;
                var moveNextAsync = enumeratorType.GetMethod("MoveNextAsync")!;
                var current = enumeratorType.GetProperty("Current")!;
                var list = new List<object?>();
                try
                {
                    while (await ((ValueTask<bool>)moveNextAsync.Invoke(
                        enumerator, null)!).ConfigureAwait(false))
                    {
                        list.Add(current.GetValue(enumerator));
                    }
                }
                finally
                {
                    if (enumerator is IAsyncDisposable disposable)
                    {
                        await disposable.DisposeAsync().ConfigureAwait(false);
                    }
                }
                return list;
            }

            /// <summary>
            /// Helper to convert a void response to buffer or throw appropriate
            /// exception as continuation.
            /// </summary>
            /// <param name="task"></param>
            /// <returns></returns>
            public Task<ReadOnlyMemory<byte>> VoidTaskContinuationAsync(Task task)
            {
                return task.ContinueWith(tr =>
                {
                    if (tr.IsFaulted || tr.IsCanceled)
                    {
                        ThrowAsMethodCallStatusException(tr);
                    }
                    return ReadOnlyMemory<byte>.Empty;
                }, scheduler: TaskScheduler.Default);
            }

            /// <summary>
            /// Helper to convert a typed response to buffer or throw appropriate
            /// exception as continuation.
            /// </summary>
            /// <param name="task"></param>
            /// <returns></returns>
            public async Task<ReadOnlyMemory<byte>> VoidValueTaskAsync(ValueTask task)
            {
                try
                {
                    await task.ConfigureAwait(false);
                    return ReadOnlyMemory<byte>.Empty;
                }
                catch (Exception ex)
                {
                    ThrowAsMethodCallStatusException(ex);
                    throw;
                }
            }

            [DoesNotReturn]
            private void ThrowAsMethodCallStatusException(Exception ex)
            {
                if (ex is AggregateException aex)
                {
                    ex = aex.Flatten().InnerExceptions.FirstOrDefault() ?? ex;
                }
                _logger.MethodCallError(ex);
                ex = _ef.Filter(ex, out var status);
                throw ex.AsMethodCallStatusException(status, _summarizer);
            }

            [DoesNotReturn]
            private void ThrowAsMethodCallStatusException(Task tr)
            {
                var ex = tr.Exception?.Flatten().InnerExceptions.FirstOrDefault();
                ex ??= new TaskCanceledException(tr);
                _logger.MethodCallError(ex);
                ex = _ef.Filter(ex, out var status);
                throw ex.AsMethodCallStatusException(status, _summarizer);
            }

            private readonly ILogger _logger;
            private readonly IExceptionSummarizer? _summarizer;
            private readonly object _controller;
            private readonly ParameterInfo[] _methodParams;
            private readonly ExceptionFilterAttribute _ef;
            private readonly MethodInfo _controllerMethod;
            private readonly ResultKind _resultKind;
            private readonly PropertyInfo? _resultProperty;
            private readonly MethodInfo? _asTask;
            private readonly MethodInfo? _getAsyncEnumerator;
        }

        private readonly ILogger _logger;
        private readonly IExceptionSummarizer? _summarizer;
        private readonly ChunkMethodServer _chunks;
        private readonly Task<List<IAsyncDisposable>> _connections;
    }

    /// <summary>
    /// Source-generated logging for MethodRouter
    /// </summary>
    internal static partial class MethodRouterLogging
    {
        private const int EventClass = 20;

        [LoggerMessage(EventId = EventClass + 0, Level = LogLevel.Error,
            Message = "Failed to connect method router to rpc server.")]
        public static partial void FailedToConnect(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Error,
            Message = "Method router not connected to any rpc server.")]
        public static partial void NotConnectedToServer(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Trace,
            Message = "Adding {Controller}.{Method} method to invoker...")]
        public static partial void AddingMethod(this ILogger logger, string controller, string method);

        [LoggerMessage(EventId = EventClass + 3, Level = LogLevel.Trace,
            Message = "Exception during method invocation.")]
        public static partial void InvocationException(this ILogger logger, Exception? e);

        [LoggerMessage(EventId = EventClass + 4, Level = LogLevel.Trace,
            Message = "Method call error")]
        public static partial void MethodCallError(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = EventClass + 5, Level = LogLevel.Error,
            Message = "Failed to dispose all connections in time. {Message}.")]
        public static partial void FailedToDispose(this ILogger logger, string? message);
    }
}
