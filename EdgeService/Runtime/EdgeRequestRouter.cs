// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService.v1;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides request routing to module request controllers
    /// </summary>
    public class EdgeRequestRouter : IEdgeRequestRouter {

        /// <summary>
        /// Create router
        /// </summary>
        /// <param name="v1Methods"></param>
        public EdgeRequestRouter(
            IModuleMethodsV1 v1Methods // Could use autofac dictionary also here....
            // ...
            ) {

            _calltable = new Dictionary<string, Invoker>();
            AddToCallTable(v1Methods, 1);
            // ...
        }

        /// <summary>
        /// Called when a service is invoked
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodResponse> InvokeMethodAsync(MethodRequest request) {
            if (!_calltable.TryGetValue(request.Name.ToLowerInvariant(), out var invoker)) {
                return new MethodResponse((int)MethodResposeStatusCode.BadRequest);
            }
            return await invoker.InvokeAsync(request);
        }

        /// <summary>
        /// Add target to calltable
        /// </summary>
        /// <param name="target"></param>
        /// <param name="version"></param>
        private void AddToCallTable(object target, int version) {
            foreach (var methodInfo in target.GetType().GetMethods()) {
                if (!typeof(Task).IsAssignableFrom(methodInfo.ReturnType)) {
                    // must be assignable from task
                    continue;
                }
                // Must have 0 or 1 generic arguments.
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
                _calltable.Add($"{name.ToLowerInvariant()}_v{version}",
                    new Invoker(target, methodInfo));
            }
        }

        /// <summary>
        /// Encapsulates invoking a service
        /// </summary>
        private class Invoker {

            /// <summary>
            /// Create invoker
            /// </summary>
            /// <param name="target"></param>
            /// <param name="method"></param>
            public Invoker(object target, MethodInfo method) {
                _target = target;
                _method = method;
                _params = _method.GetParameters();
                _result = _methodResponseAsContinuation.MakeGenericMethod(
                    _method.ReturnParameter.ParameterType);
            }

            /// <summary>
            /// Called when a service is invoked
            /// </summary>
            /// <param name="request"></param>
            /// <returns></returns>
            public Task<MethodResponse> InvokeAsync(MethodRequest request) {
                Task returned;
                if (_params.Length == 1) {
                    returned = (Task)_method.Invoke(_target, new[] {
                        JsonConvert.DeserializeObject(request.DataAsJson, _params[0].ParameterType)
                    });
                }
                else {
                    var data = (JObject)JToken.Parse(request.DataAsJson);
                    returned = (Task)_method.Invoke(_target, _params.Select(param => {
                        if (data.TryGetValue(param.Name, out var value)) {
                            return value.ToObject(param.ParameterType);
                        }
                        return param.HasDefaultValue ? param.DefaultValue : null;
                    }).ToArray());
                }
                return (Task<MethodResponse>)_result.Invoke(this, new[] { returned });
            }

            /// <summary>
            /// Helper to convert to method response
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="task"></param>
            /// <returns></returns>
            public Task<MethodResponse> MethodResponseAsContinuation<T>(Task<T> task) {
                return task.ContinueWith(tr => {
                    var response = JsonConvert.SerializeObject(tr.Result);
                    return new MethodResponse(Encoding.UTF8.GetBytes(response), 200);
                });
            }
            private static readonly MethodInfo _methodResponseAsContinuation =
                typeof(Invoker).GetMethod(nameof(MethodResponseAsContinuation),
                    BindingFlags.Public | BindingFlags.Instance);

            private readonly object _target;
            private readonly MethodInfo _method;
            private readonly ParameterInfo[] _params;
            private readonly MethodInfo _result;
        }

        private readonly Dictionary<string, Invoker> _calltable;
    }
}