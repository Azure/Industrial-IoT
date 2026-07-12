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
    using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides request routing to module controllers.
    /// </summary>
    public sealed class MethodRouter : IRpcHandler, IAwaitable<MethodRouter>,
        IDisposable, IAsyncDisposable
    {
        /// <inheritdoc/>
        public string MountPoint { get; }

        /// <summary>
        /// Property injection is not supported for generated descriptors. Register
        /// controller descriptors through the assembly generated registration table.
        /// </summary>
#pragma warning disable CA1044 // Properties should not be write only
        [Obsolete("Use the assembly-generated method-router descriptor table instead.")]
        public IEnumerable<IMethodController> Controllers
#pragma warning restore CA1044 // Properties should not be write only
        {
            set => throw new InvalidOperationException(
                "Method controllers must be registered through generated descriptors.");
        }

        /// <summary>
        /// Property DI to prevent circular dependency between host and invoker.
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
        /// Creates a router.
        /// </summary>
        /// <param name="servers">Servers to mount the router on.</param>
        /// <param name="logger">Router logger.</param>
        /// <param name="serializer">Generated JSON metadata provider.</param>
        /// <param name="summarizer">Exception summarizer.</param>
        /// <param name="options">Router options.</param>
        /// <param name="timeProvider">Clock used by the chunk server.</param>
        public MethodRouter(IEnumerable<IRpcServer> servers,
            ILogger<MethodRouter> logger, IMethodRouterJsonSerializer serializer,
            IExceptionSummarizer? summarizer = null,
            IOptions<RouterOptions>? options = null, TimeProvider? timeProvider = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            JsonSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _summarizer = summarizer;
            MountPoint = options?.Value.MountPoint ?? string.Empty;
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

        /// <summary>
        /// Gets the typed JSON metadata provider used by generated descriptors.
        /// </summary>
        public IMethodRouterJsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Registers a generated, typed method descriptor.
        /// </summary>
        /// <param name="methodName">The versioned method name.</param>
        /// <param name="descriptor">Generated method descriptor.</param>
        public void Register(string methodName, MethodRouteDescriptor descriptor)
        {
            ArgumentException.ThrowIfNullOrEmpty(methodName);
            ArgumentNullException.ThrowIfNull(descriptor);
            if (!_chunks.TryGetValue(methodName, out var invoker))
            {
                invoker = new MethodInvokerCollection(_logger, methodName);
                _chunks.Add(methodName, invoker);
            }
            if (invoker is not MethodInvokerCollection collection)
            {
                throw new InvalidOperationException(
                    $"Cannot add {methodName} since invoker is private.");
            }
            descriptor.Initialize(_logger, _summarizer);
            collection.Add(descriptor);
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
                await DisposeAsync(connections).WaitAsync(TimeSpan.FromSeconds(5))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.FailedToDispose(ex.Message);
            }
            static Task DisposeAsync(List<IAsyncDisposable> connections) =>
                Task.WhenAll(connections.Select(async connection =>
                    await connection.DisposeAsync().ConfigureAwait(false)));
        }

        /// <summary>
        /// Creates connections to servers.
        /// </summary>
        /// <param name="servers">Servers to connect.</param>
        /// <returns>The connected server handles.</returns>
        private async Task<List<IAsyncDisposable>> ConnectAsync(
            IEnumerable<IRpcServer> servers)
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

        private readonly ILogger _logger;
        private readonly IExceptionSummarizer? _summarizer;
        private readonly ChunkMethodServer _chunks;
        private readonly Task<List<IAsyncDisposable>> _connections;

        /// <summary>
        /// Holds descriptors that deliberately share a direct-method name. The
        /// original controller ordering and "try the next controller" behavior are
        /// retained without method metadata discovery.
        /// </summary>
        private sealed class MethodInvokerCollection : IMethodInvoker
        {
            /// <inheritdoc/>
            public string MethodName { get; }

            /// <summary>
            /// Creates a descriptor collection.
            /// </summary>
            /// <param name="logger">Router logger.</param>
            /// <param name="methodName">Versioned method name.</param>
            public MethodInvokerCollection(ILogger logger, string methodName)
            {
                _logger = logger;
                MethodName = methodName;
            }

            /// <summary>
            /// Adds a descriptor in controller registration order.
            /// </summary>
            /// <param name="descriptor">Descriptor to add.</param>
            public void Add(MethodRouteDescriptor descriptor)
            {
                _invokers.Add(descriptor);
            }

            /// <inheritdoc/>
            public async ValueTask<ReadOnlyMemory<byte>> InvokeAsync(
                ReadOnlyMemory<byte> payload, string contentType, IRpcHandler context,
                CancellationToken ct)
            {
                Exception? exception = null;
                foreach (var invoker in _invokers)
                {
                    try
                    {
                        return await invoker.InvokeAsync(payload, contentType, context, ct)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }
                _logger.InvocationException(exception);
                throw exception!;
            }

            private readonly ILogger _logger;
            private readonly List<MethodRouteDescriptor> _invokers = [];
        }
    }

    /// <summary>
    /// Provides source-generated JSON metadata to generated direct-method
    /// descriptors. Hosts supply this dependency so the router never discovers
    /// request or response types at runtime.
    /// </summary>
    public interface IMethodRouterJsonSerializer
    {
        /// <summary>
        /// Gets generated JSON metadata for a statically known type.
        /// </summary>
        /// <typeparam name="T">Request or response type.</typeparam>
        /// <returns>The generated type metadata.</returns>
        JsonTypeInfo<T> GetTypeInfo<T>();
    }

    /// <summary>
    /// Supplies source-generated JSON metadata to the direct-method serializer.
    /// </summary>
    public interface IMethodRouterJsonTypeInfoProvider
    {
        /// <summary>
        /// Gets source-generated metadata for a statically rooted type.
        /// </summary>
        /// <param name="type">The statically rooted contract type.</param>
        /// <returns>The source-generated metadata, or <see langword="null"/>.</returns>
        JsonTypeInfo? GetTypeInfo(Type type);
    }

    /// <summary>
    /// Adapts a source-generated <see cref="JsonSerializerContext"/> to direct
    /// method metadata. It never creates reflection metadata.
    /// </summary>
    public sealed class JsonContextMethodRouterJsonTypeInfoProvider :
        IMethodRouterJsonTypeInfoProvider
    {
        /// <summary>
        /// Creates the source-generated metadata provider.
        /// </summary>
        /// <param name="context">Source-generated JSON context.</param>
        public JsonContextMethodRouterJsonTypeInfoProvider(JsonSerializerContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public JsonTypeInfo? GetTypeInfo(Type type)
        {
            return _context.GetTypeInfo(type);
        }

        private readonly JsonSerializerContext _context;
    }

    /// <summary>
    /// Composes source-generated type-info providers registered by controller and
    /// contract assemblies. A missing contract is a startup/configuration error;
    /// no reflection resolver is used.
    /// </summary>
    public sealed class MethodRouterJsonSerializer : IMethodRouterJsonSerializer
    {
        /// <summary>
        /// Creates the composite serializer.
        /// </summary>
        /// <param name="providers">Source-generated type-info providers.</param>
        public MethodRouterJsonSerializer(
            IEnumerable<IMethodRouterJsonTypeInfoProvider> providers)
        {
            _providers = providers?.ToArray() ??
                throw new ArgumentNullException(nameof(providers));
        }

        /// <inheritdoc/>
        public JsonTypeInfo<T> GetTypeInfo<T>()
        {
            foreach (var provider in _providers)
            {
                if (provider.GetTypeInfo(typeof(T)) is JsonTypeInfo<T> typeInfo)
                {
                    return typeInfo;
                }
            }
            throw new InvalidOperationException(
                $"No source-generated JSON metadata provider registered for {typeof(T)}.");
        }

        private readonly IMethodRouterJsonTypeInfoProvider[] _providers;
    }

    /// <summary>
    /// Registers descriptors for controllers emitted by one compile-time assembly.
    /// </summary>
    public interface IMethodRouterDescriptorProvider
    {
        /// <summary>
        /// Registers descriptors when <paramref name="controller"/> belongs to this
        /// provider's generated assembly.
        /// </summary>
        /// <param name="router">Method router.</param>
        /// <param name="controller">Controller in host registration order.</param>
        /// <param name="serializer">Source-generated JSON metadata provider.</param>
        /// <returns><see langword="true"/> when the controller was handled.</returns>
        bool TryRegister(MethodRouter router, IMethodController controller,
            IMethodRouterJsonSerializer serializer);
    }

    /// <summary>
    /// Throws when no source-generated metadata is available.
    /// </summary>
    public sealed class MissingMethodRouterJsonTypeInfoProvider :
        IMethodRouterJsonTypeInfoProvider
    {
        /// <inheritdoc/>
        public JsonTypeInfo? GetTypeInfo(Type type)
        {
            throw new InvalidOperationException(
                $"No source-generated JSON metadata provider registered for {type}.");
        }
    }

    /// <summary>
    /// Typed direct-method descriptor emitted by the controller descriptor
    /// generator. Its delegate contains no method metadata lookup or invocation.
    /// </summary>
    public sealed class MethodRouteDescriptor : IMethodInvoker
    {
        /// <inheritdoc/>
        public string MethodName { get; }

        /// <summary>
        /// Creates a route descriptor.
        /// </summary>
        /// <param name="methodName">Controller method name.</param>
        /// <param name="filter">Controller exception filter.</param>
        /// <param name="invoke">Strongly typed generated invocation delegate.</param>
        public MethodRouteDescriptor(string methodName, ExceptionFilterAttribute? filter,
            Func<ReadOnlyMemory<byte>, CancellationToken,
                ValueTask<ReadOnlyMemory<byte>>> invoke)
        {
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            _filter = filter ?? new DefaultFilter();
            _invoke = invoke ?? throw new ArgumentNullException(nameof(invoke));
        }

        /// <inheritdoc/>
        public async ValueTask<ReadOnlyMemory<byte>> InvokeAsync(
            ReadOnlyMemory<byte> payload, string contentType, IRpcHandler context,
            CancellationToken ct)
        {
            try
            {
                return await _invoke(payload, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is AggregateException aggregate)
                {
                    ex = aggregate.Flatten().InnerExceptions.FirstOrDefault() ?? ex;
                }
                ex = _filter.Filter(ex, out var status);
                _logger?.MethodCallError(ex);
                throw ex.AsMethodCallStatusException(status, _summarizer);
            }
        }

        internal void Initialize(ILogger logger, IExceptionSummarizer? summarizer)
        {
            _logger = logger;
            _summarizer = summarizer;
        }

        private readonly ExceptionFilterAttribute _filter;
        private readonly Func<ReadOnlyMemory<byte>, CancellationToken,
            ValueTask<ReadOnlyMemory<byte>>> _invoke;
        private ILogger? _logger;
        private IExceptionSummarizer? _summarizer;

        /// <summary>
        /// Default compatibility exception filter.
        /// </summary>
        private sealed class DefaultFilter : ExceptionFilterAttribute
        {
            /// <inheritdoc/>
            public override Exception Filter(Exception exception, out int status)
            {
                status = 400;
                return exception;
            }
        }
    }

    /// <summary>
    /// JSON helpers used by generated descriptors. The descriptor generator passes
    /// only source-generated type metadata to these methods.
    /// </summary>
    public static class MethodRouterJson
    {
        /// <summary>
        /// Deserializes a request using generated type metadata.
        /// </summary>
        /// <typeparam name="T">Request type.</typeparam>
        /// <param name="payload">Request payload.</param>
        /// <param name="typeInfo">Generated request metadata.</param>
        /// <returns>The deserialized request.</returns>
        public static T? Deserialize<T>(ReadOnlyMemory<byte> payload,
            JsonTypeInfo<T> typeInfo)
        {
            return JsonSerializer.Deserialize(payload.Span, typeInfo);
        }

        /// <summary>
        /// Deserializes an object-property request value using generated metadata.
        /// </summary>
        /// <typeparam name="T">Request property type.</typeparam>
        /// <param name="payload">Request property payload.</param>
        /// <param name="typeInfo">Generated request metadata.</param>
        /// <returns>The deserialized request value.</returns>
        public static T? Deserialize<T>(JsonElement payload, JsonTypeInfo<T> typeInfo)
        {
            return payload.Deserialize(typeInfo);
        }

        /// <summary>
        /// Serializes a response using generated type metadata.
        /// </summary>
        /// <typeparam name="T">Response type.</typeparam>
        /// <param name="value">Response value.</param>
        /// <param name="typeInfo">Generated response metadata.</param>
        /// <returns>The JSON response payload.</returns>
        public static ReadOnlyMemory<byte> Serialize<T>(T value, JsonTypeInfo<T> typeInfo)
        {
            return JsonSerializer.SerializeToUtf8Bytes(value, typeInfo);
        }

        /// <summary>
        /// Drains and serializes an async stream with generated list metadata.
        /// </summary>
        /// <typeparam name="T">Stream element type.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="typeInfo">Generated list metadata.</param>
        /// <returns>The JSON response payload.</returns>
        public static async ValueTask<ReadOnlyMemory<byte>> DrainAsync<T>(
            IAsyncEnumerable<T> source, JsonTypeInfo<List<T>> typeInfo)
        {
            var values = new List<T>();
            await foreach (var value in source.WithCancellation(CancellationToken.None)
                .ConfigureAwait(false))
            {
                values.Add(value);
            }
            return JsonSerializer.SerializeToUtf8Bytes(values, typeInfo);
        }
    }

    /// <summary>
    /// Source-generated logging for <see cref="MethodRouter"/>.
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
