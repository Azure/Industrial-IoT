// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.Devices.Client;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using System.Net;

    /// <summary>
    /// Provides request routing to module controllers
    /// </summary>
    public sealed class MethodRouter : IMethodRouter, IMethodHandler {

        /// <summary>
        /// Property Di to prevent circular dependency between host and controller
        /// </summary>
        public IEnumerable<IMethodController> Controllers {
            set {
                foreach (var controller in value) {
                    AddToCallTable(controller);
                }
            }
        }

        /// <summary>
        /// Property Di to prevent circular dependency between host and invoker
        /// </summary>
        public IEnumerable<IMethodInvoker> ExternalInvokers {
            set {
                foreach (var invoker in value) {
                    _calltable.AddOrUpdate(invoker.MethodName, invoker);
                }
            }
        }

        /// <summary>
        /// Create router
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public MethodRouter(IJsonSerializer serializer, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            // Create chunk server always
            var server = new ChunkMethodServer(_serializer, logger);
            _calltable = new Dictionary<string, IMethodInvoker> {
                { server.MethodName, server }
            };
        }

        /// <inheritdoc/>
        public async Task<MethodResponse> InvokeMethodAsync(MethodRequest request) {
            const int kMaxMessageSize = 127 * 1024;
            try {
                var result = await InvokeAsync(request.Name, request.Data,
                    ContentMimeType.Json);
                if (result.Length > kMaxMessageSize) {
                    _logger.Error("Result (Payload too large => {Length}", result.Length);
                    return new MethodResponse((int)HttpStatusCode.RequestEntityTooLarge);
                }
                return new MethodResponse(result, 200);
            }
            catch (MethodCallStatusException mex) {
                var payload = Encoding.UTF8.GetBytes(mex.ResponsePayload);
                return new MethodResponse(payload.Length > kMaxMessageSize ? null : payload,
                    mex.Result);
            }
            catch (Exception) {
                return new MethodResponse((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> InvokeAsync(string method, byte[] payload,
            string contentType) {
            if (!_calltable.TryGetValue(method.ToLowerInvariant(), out var invoker)) {
                throw new NotSupportedException(
                    $"Unknown controller method {method} called.");
            }
            return await invoker.InvokeAsync(payload, contentType, this);
        }

        /// <summary>
        /// Add target to calltable
        /// </summary>
        /// <param name="target"></param>
        private void AddToCallTable(object target) {
            var versions = target.GetType().GetCustomAttributes<VersionAttribute>(true)
                .Select(v => v.Value)
                .ToList();
            if (versions.Count == 0) {
                versions.Add(string.Empty);
            }
            foreach (var methodInfo in target.GetType().GetMethods()) {
                if (methodInfo.GetCustomAttribute<IgnoreAttribute>() != null) {
                    // Should be ignored
                    continue;
                }
                if (!typeof(Task).IsAssignableFrom(methodInfo.ReturnType)) {
                    // must be assignable from task
                    continue;
                }
                var tArgs = methodInfo.ReturnParameter.ParameterType
                    .GetGenericArguments();
                if (tArgs.Length > 1) {
                    // must have exactly 0 or one (serializable) type to return
                    continue;
                }
                var name = methodInfo.Name;
                if (name.EndsWith("Async", StringComparison.Ordinal)) {
                    name = name.Substring(0, name.Length - 5);
                }
                name = name.ToLowerInvariant();

                // Register for all defined versions
                foreach (var version in versions) {
                    var versionedName = name + version;
                    versionedName = versionedName.ToLowerInvariant();
                    if (!_calltable.TryGetValue(versionedName, out var invoker)) {
                        invoker = new DynamicInvoker(_logger);
                        _calltable.Add(versionedName, invoker);
                    }
                    if (invoker is DynamicInvoker dynamicInvoker) {
                        dynamicInvoker.Add(target, methodInfo, _serializer);
                    }
                    else {
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
        private class DynamicInvoker : IMethodInvoker {

            /// <inheritdoc/>
            public string MethodName { get; private set; }

            /// <summary>
            /// Create dynamic invoker
            /// </summary>
            public DynamicInvoker(ILogger logger) {
                _logger = logger;
                _invokers = new List<JsonMethodInvoker>();
            }

            /// <summary>
            /// Add invoker
            /// </summary>
            /// <param name="controller"></param>
            /// <param name="controllerMethod"></param>
            /// <param name="serializer"></param>
            public void Add(object controller, MethodInfo controllerMethod, IJsonSerializer serializer) {
                _logger.Verbose("Adding {controller}.{method} method to invoker...",
                    controller.GetType().Name, controllerMethod.Name);
                _invokers.Add(new JsonMethodInvoker(controller, controllerMethod, serializer, _logger));
                MethodName = controllerMethod.Name;
            }

            /// <inheritdoc/>
            public async Task<byte[]> InvokeAsync(byte[] payload, string contentType,
                IMethodHandler handler) {
                Exception e = null;
                foreach (var invoker in _invokers) {
                    try {
                        return await invoker.InvokeAsync(payload, contentType, handler);
                    }
                    catch (Exception ex) {
                        // Save last, and continue
                        e = ex;
                    }
                }
                _logger.Verbose(e, "Exception during method invocation.");
                throw e;
            }

            /// <inheritdoc/>
            public void Dispose() {
                foreach (var invoker in _invokers) {
                    invoker.Dispose();
                }
            }

            private readonly ILogger _logger;
            private readonly List<JsonMethodInvoker> _invokers;
        }

        /// <summary>
        /// Invokes a method with json payload
        /// </summary>
        private class JsonMethodInvoker : IMethodInvoker {

            /// <inheritdoc/>
            public string MethodName => _controllerMethod.Name;

            /// <summary>
            /// Default filter implementation if none is specified
            /// </summary>
            private class DefaultFilter : ExceptionFilterAttribute {
                public override Exception Filter(Exception exception, out int status) {
                    status = 400;
                    return exception;
                }
            }

            /// <summary>
            /// Create invoker
            /// </summary>
            /// <param name="controller"></param>
            /// <param name="controllerMethod"></param>
            /// <param name="serializer"></param>
            /// <param name="logger"></param>
            public JsonMethodInvoker(object controller, MethodInfo controllerMethod,
                IJsonSerializer serializer, ILogger logger) {
                _logger = logger;
                _serializer = serializer;
                _controller = controller;
                _controllerMethod = controllerMethod;
                _methodParams = _controllerMethod.GetParameters();
                _ef = _controllerMethod.GetCustomAttribute<ExceptionFilterAttribute>(true) ??
                    controller.GetType().GetCustomAttribute<ExceptionFilterAttribute>(true) ??
                    new DefaultFilter();
                var returnArgs = _controllerMethod.ReturnParameter.ParameterType.GetGenericArguments();
                if (returnArgs.Length > 0) {
                    _methodTaskContinuation = _methodResponseAsContinuation.MakeGenericMethod(
                        returnArgs[0]);
                }
            }

            /// <inheritdoc/>
            public Task<byte[]> InvokeAsync(byte[] payload, string contentType,
                IMethodHandler handler) {
                object task;
                try {
                    object[] inputs;
                    if (_methodParams.Length == 0) {
                        inputs = Array.Empty<object>();
                    }
                    else if (_methodParams.Length == 1) {
                        var data = _serializer.Deserialize(payload, _methodParams[0].ParameterType);
                        inputs = new[] { data };
                    }
                    else {
                        var data = _serializer.Parse(payload);
                        inputs = _methodParams.Select(param => {
                            if (data.TryGetProperty(param.Name,
                                out var value, StringComparison.InvariantCultureIgnoreCase)) {
                                return value.ConvertTo(param.ParameterType);
                            }
                            return param.HasDefaultValue ? param.DefaultValue : null;
                        }).ToArray();
                    }
                    task = _controllerMethod.Invoke(_controller, inputs);
                }
                catch (Exception e) {
                    task = Task.FromException(e);
                }
                if (_methodTaskContinuation == null) {
                    return VoidContinuation((Task)task);
                }
                return (Task<byte[]>)_methodTaskContinuation.Invoke(this, new[] {
                    task
                });
            }

            /// <inheritdoc/>
            public void Dispose() {
            }

            /// <summary>
            /// Helper to convert a typed response to buffer or throw appropriate
            /// exception as continuation.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="task"></param>
            /// <returns></returns>
            public Task<byte[]> MethodResultConverterContinuation<T>(Task<T> task) {
                return task.ContinueWith(tr => {
                    if (tr.IsFaulted || tr.IsCanceled) {
                        var ex = tr.Exception?.Flatten().InnerExceptions.FirstOrDefault();
                        if (ex == null) {
                            ex = new TaskCanceledException(tr);
                        }
                        _logger.Verbose(ex, "Method call error");
                        ex = _ef.Filter(ex, out var status);
                        if (ex is MethodCallStatusException) {
                            throw ex;
                        }
                        throw new MethodCallStatusException(ex != null ?
                           _serializer.SerializeToString(ex) : null, status);
                    }
                    return _serializer.SerializeToBytes(tr.Result).ToArray();
                });
            }

            /// <summary>
            /// Helper to convert a void response to buffer or throw appropriate
            /// exception as continuation.
            /// </summary>
            /// <param name="task"></param>
            /// <returns></returns>
            public Task<byte[]> VoidContinuation(Task task) {
                return task.ContinueWith(tr => {
                    if (tr.IsFaulted || tr.IsCanceled) {
                        var ex = tr.Exception?.Flatten().InnerExceptions.FirstOrDefault();
                        if (ex == null) {
                            ex = new TaskCanceledException(tr);
                        }
                        _logger.Verbose(ex, "Method call error");
                        ex = _ef.Filter(ex, out var status);
                        if (ex is MethodCallStatusException) {
                            throw ex;
                        }
                        throw new MethodCallStatusException(ex != null ?
                            _serializer.SerializeToString(ex) : null, status);
                    }
                    return Array.Empty<byte>();
                });
            }

            private static readonly MethodInfo _methodResponseAsContinuation =
                typeof(JsonMethodInvoker).GetMethod(nameof(MethodResultConverterContinuation),
                    BindingFlags.Public | BindingFlags.Instance);
            private readonly IJsonSerializer _serializer;
            private readonly ILogger _logger;
            private readonly object _controller;
            private readonly ParameterInfo[] _methodParams;
            private readonly ExceptionFilterAttribute _ef;
            private readonly MethodInfo _controllerMethod;
            private readonly MethodInfo _methodTaskContinuation;
        }

        private readonly ILogger _logger;
        private readonly IJsonSerializer _serializer;
        private readonly Dictionary<string, IMethodInvoker> _calltable;
    }
}
