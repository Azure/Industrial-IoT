// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Devices.Edge.Hosting {
    using Microsoft.Azure.Devices.Edge.Services;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.IIoT.Common.Diagnostics;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides set/get routing to controllers
    /// </summary>
    public class SettingsRouter : ISettingsRouter {

        /// <summary>
        /// Property Di to prevent circular dependency between host and controller
        /// </summary>
        public IEnumerable<ISettingsController> Controllers {
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
        public SettingsRouter(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _calltable = new Dictionary<string, CascadingInvoker>();
        }

        /// <summary>
        /// Called to apply settings
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public async Task<TwinCollection> ProcessSettingsAsync(TwinCollection settings) {
            var reported = new TwinCollection();
            foreach (KeyValuePair<string, dynamic> setting in settings) {
                if (!_calltable.TryGetValue(setting.Key.ToLowerInvariant(), out var invoker) &&
                    !_calltable.TryGetValue("@default", out invoker)) {
                    _logger.Error("Setting unsupported", () => new { setting });
                    reported[setting.Key] = null;
                    continue;
                }
                try {
                    await invoker.SetAsync(setting.Key, setting.Value);
                    reported[setting.Key] = setting.Value;
                }
                catch (Exception ex) {
                    _logger.Error("Error processing setting", () => new { setting, ex });
                    reported[setting.Key] = null;
                }
            }
            return reported;
        }

        /// <summary>
        /// Add target to calltable
        /// </summary>
        /// <param name="target"></param>
        private void AddToCallTable(object target) {
            if (target.GetType().GetInterfaces()
                    .Any(t => typeof(IMethodController).IsAssignableFrom(t))) {
                throw new ArgumentException("Settings and method controllers should not mix");
            }
            var version = target.GetType().GetCustomAttribute<VersionAttribute>(true);
            foreach (var methodInfo in target.GetType().GetMethods()) {
                if (!typeof(Task).IsAssignableFrom(methodInfo.ReturnType)) {
                    // must be assignable from task
                    continue;
                }
                // Must have 0 generic arguments.
                var tArgs = methodInfo.ReturnParameter.ParameterType
                    .GetGenericArguments();
                if (tArgs.Length != 0) {
                    continue;
                }
                var name = methodInfo.Name;
                var param = methodInfo.GetParameters();
                if (name == "SetAsync") {
                    if (param.Length != 2 ||
                        param[0].ParameterType != typeof(string)) {
                        continue;
                    }
                    name = "@default";
                }
                else if (name.StartsWith("Set", StringComparison.OrdinalIgnoreCase)) {
                    name = name.Substring(3);
                    if (name.EndsWith("Async", StringComparison.OrdinalIgnoreCase)) {
                        if (param.Length != 1) {
                            continue;
                        }
                        name = name.Substring(0, name.Length - 5);
                    }
                    name = name.ToLowerInvariant();
                }
                if (!_calltable.TryGetValue(name, out var invoker)) {
                    invoker = new CascadingInvoker(_logger);
                    _calltable.Add(name, invoker);
                }
                invoker.Add(target, methodInfo, version?.Numeric ?? ulong.MaxValue);
            }
        }

        /// <summary>
        /// Trys all setters until it can apply the setting.
        /// </summary>
        private class CascadingInvoker {

            /// <summary>
            /// Create cascading invoker
            /// </summary>
            public CascadingInvoker(ILogger logger) {
                _logger = logger;
                _invokers = new SortedList<ulong, SetterInvoker>(
                    Comparer<ulong>.Create((x, y) => (int)(x - y)));
            }

            /// <summary>
            /// Create invoker
            /// </summary>
            /// <param name="controller"></param>
            /// <param name="controllerMethod"></param>
            public void Add(object controller, MethodInfo controllerMethod, ulong version) {
                _invokers.Add(version, new SetterInvoker(controller, controllerMethod, _logger));
            }

            /// <summary>
            /// Called when a setting is to be set
            /// </summary>
            /// <param name="property"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public async Task SetAsync(string property, dynamic value) {
                Exception e = null;
                var sw = Stopwatch.StartNew();
                foreach (var invoker in _invokers) {
                    try {
                        await invoker.Value.SetAsync(property, value);
                        _logger.Debug(
                            $"Property '{property}' updated (took {sw.ElapsedMilliseconds} ms)!",
                                () => { });
                        return;
                    }
                    catch (Exception ex) {
                        // Save last error, and continue
                        _logger.Debug($"Updating '{property}' failed!", () => ex);
                        e = ex;
                    }
                }
                _logger.Error(
                    $"Exception during setter invocation (took {sw.ElapsedMilliseconds} ms).",
                        () => e);
                throw e;
            }

            private readonly ILogger _logger;
            private readonly SortedList<ulong, SetterInvoker> _invokers;
        }

        /// <summary>
        /// Encapsulates applying a setting
        /// </summary>
        private class SetterInvoker {

            /// <summary>
            /// Create invoker
            /// </summary>
            /// <param name="controller"></param>
            /// <param name="controllerMethod"></param>
            public SetterInvoker(object controller, MethodInfo controllerMethod, ILogger logger) {
                _logger = logger;
                _controller = controller;
                _controllerMethod = controllerMethod;
                _methodParams = _controllerMethod.GetParameters();
                _paramCast = _cast.MakeGenericMethod(
                    _methodParams[_methodParams.Length == 2 ? 1 : 0].ParameterType);
            }

            /// <summary>
            /// Called when a service is invoked
            /// </summary>
            /// <param name="property"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public Task SetAsync(string property, dynamic value) {
                try {
                    object[] inputs = { value };
                    var cast = _paramCast.Invoke(null, inputs);
                    if (_methodParams.Length == 2) {
                        inputs = new[] { property, cast };
                    }
                    else {
                        inputs = new[] { cast };
                    }
                    return (Task)_controllerMethod.Invoke(_controller, inputs);
                }
                catch (Exception e) {
                    _logger.Warn($"Exception during setter invocation ", () => new {
                        name = _controller.GetType().Name,
                        method = _controllerMethod.Name,
                        exception = e
                    });
                    return Task.FromException(e);
                }
            }

            /// <summary>
            /// Helper to convert to type using various ways.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static T Cast<T>(object obj) {
                if (obj is JToken jtoken) {
                    try {
                        return jtoken.ToObject<T>();
                    }
                    catch {
                        Console.WriteLine(jtoken);
                    }
                }
                try {
                    return (T)obj;
                }
                catch (Exception) {
                    try {
                        return (T)Convert.ChangeType(obj, typeof(T));
                    }
                    catch (Exception) {
                        Console.WriteLine(obj);
                        return default(T);
                    }
                }
            }

            private static readonly MethodInfo _cast =
                typeof(SetterInvoker).GetMethod(nameof(Cast),
                    BindingFlags.Public | BindingFlags.Static);

            private readonly ILogger _logger;
            private readonly object _controller;
            private readonly ParameterInfo[] _methodParams;
            private readonly MethodInfo _paramCast;
            private readonly MethodInfo _controllerMethod;
        }

        private readonly ILogger _logger;
        private readonly Dictionary<string, CascadingInvoker> _calltable;
    }
}