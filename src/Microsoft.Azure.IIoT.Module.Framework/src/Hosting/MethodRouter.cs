// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using System.Net;
    using System.Diagnostics;

    /// <summary>
    /// Provides request routing to module controllers
    /// </summary>
    public class MethodRouter : IMethodRouter {

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
        /// Create router
        /// </summary>
        /// <param name="logger"></param>
        public MethodRouter(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _calltable = new Dictionary<string, DynamicInvoker>();
        }

        /// <summary>
        /// Called when a service is invoked
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodResponse> InvokeMethodAsync(MethodRequest request) {
            if (!_calltable.TryGetValue(request.Name.ToLowerInvariant(), out var invoker)) {
                _logger.Error($"Unknown controller method called", () => request);
                return new MethodResponse((int)HttpStatusCode.NotImplemented);
            }
            var sw = Stopwatch.StartNew();
            _logger.Debug("Invoking controller method... ", () => request);
            var result = await invoker.InvokeAsync(request);
            _logger.Debug($"... method invoked (took {sw.ElapsedMilliseconds} ms)",
                () => { });
            return result;
        }

        /// <summary>
        /// Add target to calltable
        /// </summary>
        /// <param name="target"></param>
        private void AddToCallTable(object target) {

            var version = target.GetType().GetCustomAttribute<VersionAttribute>(true);
            foreach (var methodInfo in target.GetType().GetMethods()) {
                if (!typeof(Task).IsAssignableFrom(methodInfo.ReturnType)) {
                    // must be assignable from task
                    continue;
                }
                var tArgs = methodInfo.ReturnParameter.ParameterType
                    .GetGenericArguments();
                if (tArgs.Length != 1) {
                    // must have exactly one (serializable) type to return
                    continue;
                }
                var name = methodInfo.Name;
                if (name.EndsWith("Async", StringComparison.Ordinal)) {
                    name = name.Substring(0, name.Length - 5);
                }
                name = name.ToLowerInvariant();
                if (version != null) {
                    name = name + ("_v" + version.Value);
                }
                name = name.ToLowerInvariant();
                if (!_calltable.TryGetValue(name, out var invoker)) {
                    invoker = new DynamicInvoker(_logger);
                    _calltable.Add(name, invoker);
                }
                invoker.Add(target, methodInfo);
            }
        }

        /// <summary>
        /// Encapsulates invoking a matching service on the controller
        /// </summary>
        private class DynamicInvoker {

            /// <summary>
            /// Create dynamic invoker
            /// </summary>
            public DynamicInvoker(ILogger logger) {
                _logger = logger;
                _invokers = new List<MethodInvoker>();
            }

            /// <summary>
            /// Create invoker
            /// </summary>
            /// <param name="controller"></param>
            /// <param name="controllerMethod"></param>
            public void Add(object controller, MethodInfo controllerMethod) {
#if LOG_VERBOSE
                _logger.Debug($"Adding {controller.GetType().Name}.{controllerMethod.Name}" +
                    " method to invoker...", () => { });
#endif
                _invokers.Add(new MethodInvoker(controller, controllerMethod, _logger));
            }

            /// <summary>
            /// Called when a setting is to be set
            /// </summary>
            /// <param name="request"></param>
            /// <returns></returns>
            public async Task<MethodResponse> InvokeAsync(MethodRequest request) {
                Exception e = null;
                foreach (var invoker in _invokers) {
                    try {
                        return await invoker.InvokeAsync(request);
                    }
                    catch (Exception ex) {
                        // Save last error, and continue
                        e = ex;
                    }
                }
                _logger.Error($"Exception during method invocation.", () => e);
                throw e;
            }

            private readonly ILogger _logger;
            private readonly List<MethodInvoker> _invokers;
        }

        /// <summary>
        /// Invokes a method
        /// </summary>
        private class MethodInvoker {

            /// <summary>
            /// Default filter implementation if none is specified
            /// </summary>
            private class DefaultFilter : ExceptionFilterAttribute {
                public override string Filter(Exception exception, out int status) {
                    status = (int)MethodResposeStatusCode.BadRequest;
                    return JsonConvertEx.SerializeObject(exception);
                }
            }

            /// <summary>
            /// Create invoker
            /// </summary>
            /// <param name="controller"></param>
            /// <param name="controllerMethod"></param>
            public MethodInvoker(object controller, MethodInfo controllerMethod, ILogger logger) {
                _logger = logger;
                _controller = controller;
                _controllerMethod = controllerMethod;
                _methodParams = _controllerMethod.GetParameters();
                _ef = _controllerMethod.GetCustomAttribute<ExceptionFilterAttribute>(true) ??
                    controller.GetType().GetCustomAttribute<ExceptionFilterAttribute>(true) ??
                    new DefaultFilter();
                _resultConverter = _methodResponseAsContinuation.MakeGenericMethod(
                    _controllerMethod.ReturnParameter.ParameterType
                        .GetGenericArguments()[0]);
            }

            /// <summary>
            /// Called when a service is invoked
            /// </summary>
            /// <param name="request"></param>
            /// <returns></returns>
            public Task<MethodResponse> InvokeAsync(MethodRequest request) {
                object[] inputs;
                object returned;
                var contentType = "json";
                try {
                    if (_methodParams.Length == 1) {

                        // TODO: parse content type as _bson, _json, _mpack, etc.
                        // and use correct decoder/encoder here.

                        var data = JsonConvertEx.DeserializeObject(request.DataAsJson,
                            _methodParams[0].ParameterType);
                        inputs = new[] { data };
                    }
                    else {
                        var data = (JObject)JToken.Parse(request.DataAsJson);
                        inputs = _methodParams.Select(param => {
                            if (data.TryGetValue(param.Name, out var value)) {
                                return value.ToObject(param.ParameterType);
                            }
                            return param.HasDefaultValue ? param.DefaultValue : null;
                        }).ToArray();
                    }
                    returned = _controllerMethod.Invoke(_controller, inputs);
                }
                catch (Exception e) {
                    _logger.Warn($"Exception during method invocation ", () => new {
                        name = _controller.GetType().Name,
                        method = _controllerMethod.Name,
                        exception = e
                    });
                    returned = Task.FromException(e);
                }

                // TODO: Pass content type
                return (Task<MethodResponse>)_resultConverter.Invoke(this, new[] {
                    returned, contentType
                });
            }

            /// <summary>
            /// Helper to convert to method response
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="task"></param>
            /// <returns></returns>
            public Task<MethodResponse> MethodResponseAsContinuation<T>(Task<T> task,
                string contentType) {
                const int kMaxMessageSize = 128 * 1024;
                return task.ContinueWith(tr => {
                    string response = null;
                    var status = 429;
                    if (!tr.IsFaulted) {
                        status = 200;
                        // TODO: Use correct content type

                        response = JsonConvertEx.SerializeObject(tr.Result);
                    }
                    else {
                        response = _ef.Filter(tr.Exception, out status);
                        _logger.Error($"Method call error", () => tr.Exception);
                    }
                    var result = Encoding.UTF8.GetBytes(
                        string.IsNullOrEmpty(response) ? "{}" : response);
                    if (result.Length > kMaxMessageSize) {
                        var ex = new MessageTooLargeException("Method call error",
                            result.Length, kMaxMessageSize);
                        _logger.Error(
                            $"Result ({contentType}) too large => {result.Length}",
                            () => ex);
                        result = Encoding.UTF8.GetBytes(_ef.Filter(ex, out status));
                        return new MethodResponse(result, status);
                    }
                    return new MethodResponse(result, status);
                });
            }
            private static readonly MethodInfo _methodResponseAsContinuation =
                typeof(MethodInvoker).GetMethod(nameof(MethodResponseAsContinuation),
                    BindingFlags.Public | BindingFlags.Instance);
            private readonly ILogger _logger;
            private readonly object _controller;
            private readonly ParameterInfo[] _methodParams;
            private readonly ExceptionFilterAttribute _ef;
            private readonly MethodInfo _controllerMethod;
            private readonly MethodInfo _resultConverter;
        }

        private readonly ILogger _logger;
        private readonly Dictionary<string, DynamicInvoker> _calltable;
    }
}
