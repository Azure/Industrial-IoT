// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.IIoT.Diagnostics;
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
            var controllers = new List<Controller>();

            // Set all properties
            foreach (KeyValuePair<string, dynamic> setting in settings) {
                if (GetInvoker(setting.Key.ToLowerInvariant(), out var invoker)) {
                    _logger.Error("Setting unsupported", () => new { setting });
                    continue;
                }
                try {
                    var controller = invoker.Set(setting.Key, setting.Value);
                    if (controller != null && !controllers.Contains(controller)) {
                        controllers.Add(controller); // To apply only affected controllers
                    }
                }
                catch (Exception ex) {
                    _logger.Error("Error processing setting", () => new { setting, ex });
                }
            }

            // Apply settings on all affected controllers
            if (controllers.Any()) {
                var sw = Stopwatch.StartNew();
                await Task.WhenAll(controllers.Select(c => c.SafeApplyAsync()));
                _logger.Debug($"Applying new settings took {sw.Elapsed}...", () => { });
            }

            // Gather new values from controller
            var reported = new TwinCollection();
            foreach (KeyValuePair<string, dynamic> setting in settings) {
                if (GetInvoker(setting.Key.ToLowerInvariant(), out var invoker)) {
                    reported[setting.Key] = null;
                    continue;
                }
                try {
                    if (!invoker.Get(setting.Key, out var value)) {
                        value = setting.Value; // No getter, use desired.
                    }
                    reported[setting.Key] = value;
                }
                catch (Exception ex) {
                    _logger.Error("Error retrieving setting", () => new { setting, ex });
                    reported[setting.Key] = null;
                }
            }
            return reported;
        }

        /// <summary>
        /// Get cached invoker
        /// </summary>
        /// <param name="key"></param>
        /// <param name="invoker"></param>
        /// <returns></returns>
        private bool GetInvoker(string key, out CascadingInvoker invoker) =>
            !_calltable.TryGetValue(key, out invoker) &&
            !_calltable.TryGetValue(kDefaultProp, out invoker);

        /// <summary>
        /// Add target to calltable
        /// </summary>
        /// <param name="target"></param>
        private void AddToCallTable(object target) {

            var version = target.GetType().GetCustomAttribute<VersionAttribute>(true)?.Numeric
                ?? ulong.MaxValue;

            var apply = target.GetType().GetMethod("ApplyAsync");
            if (apply != null) {
                if (apply.GetParameters().Length != 0 || apply.ReturnType != typeof(Task)) {
                    apply = null;
                }
            }

            var controller = new Controller(target, version, apply, _logger);

            foreach (var propInfo in target.GetType().GetProperties()) {
                if (!propInfo.CanWrite || propInfo.GetIndexParameters().Length > 1) {
                    // must be able to write
                    continue;
                }
                var name = propInfo.Name.ToLowerInvariant();
                var indexers = propInfo.GetIndexParameters();
                var indexed = false;
                if (indexers.Length == 1 && indexers[0].ParameterType == typeof(string)) {
                    // save .net indexer as default
                    if (name == "item") {
                        name = kDefaultProp;
                    }
                    indexed = true;
                }
                else if (indexers.Length != 0) {
                    // Unusable
                    continue;
                }
                if (!_calltable.TryGetValue(name, out var invoker)) {
                    invoker = new CascadingInvoker(_logger);
                    _calltable.Add(name, invoker);
                }
                invoker.Add(controller, propInfo, indexed);
            }
        }

        /// <summary>
        /// Wraps a controller
        /// </summary>
        private class Controller {

            /// <summary>
            /// Target
            /// </summary>
            public object Target { get; }

            /// <summary>
            /// Version
            /// </summary>
            public ulong Version { get; }

            /// <summary>
            /// Create cascading invoker
            /// </summary>
            public Controller(object controller, ulong version,
                MethodInfo applyMethod, ILogger logger) {
                Target = controller;
                Version = version;
                _logger = logger;
                _applyMethod = applyMethod;
            }

            /// <summary>
            /// Called to apply changes
            /// </summary>
            /// <returns></returns>
            public Task SafeApplyAsync() {
                try {
                    if (_applyMethod == null) {
                        return Task.CompletedTask;
                    }
                    return (Task)_applyMethod.Invoke(Target, new object[] { });
                }
                catch (Exception e) {
                    _logger.Error($"Exception applying changes! Continue...", () => new {
                        name = Target.GetType().Name,
                        method = _applyMethod.Name,
                        exception = e
                    });
                    // Eat exception.
                    return Task.CompletedTask;
                }
            }

            private readonly ILogger _logger;
            private readonly MethodInfo _applyMethod;
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
                _invokers = new SortedList<ulong, PropertyInvoker>(
                    Comparer<ulong>.Create((x, y) => (int)(x - y)));
            }

            /// <summary>
            /// Create invoker
            /// </summary>
            /// <param name="controller"></param>
            /// <param name="controllerProp"></param>
            /// <param name="indexed"></param>
            public void Add(Controller controller, PropertyInfo controllerProp,
                bool indexed) {
                _invokers.Add(controller.Version, new PropertyInvoker(controller,
                    controllerProp, indexed, _logger));
            }

            /// <summary>
            /// Called when a setting is to be set
            /// </summary>
            /// <param name="property"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public Controller Set(string property, dynamic value) {
                Exception e = null;
                foreach (var invoker in _invokers) {
                    try {
                        return invoker.Value.Set(property, value);
                    }
                    catch (Exception ex) {
                        // Save last error, and continue
                        _logger.Debug($"Setting '{property}' failed!", () => ex);
                        e = ex;
                    }
                }
                _logger.Error( $"Exception during setter invocation.", () => e);
                throw e;
            }

            /// <summary>
            /// Called to read setting
            /// </summary>
            /// <param name="property"></param>
            /// <returns></returns>
            public bool Get(string property, out dynamic value) {
                Exception e = null;
                foreach (var invoker in _invokers) {
                    try {
                        return invoker.Value.Get(property, out value);
                    }
                    catch (Exception ex) {
                        // Save last error, and continue
                        _logger.Debug($"Retrieving '{property}' failed!", () => ex);
                        e = ex;
                    }
                }
                _logger.Error($"Exception during getter invocation.", () => e);
                throw e;
            }

            private readonly ILogger _logger;
            private readonly SortedList<ulong, PropertyInvoker> _invokers;
        }

        /// <summary>
        /// Encapsulates applying a property
        /// </summary>
        private class PropertyInvoker {

            /// <summary>
            /// Create invoker
            /// </summary>
            /// <param name="controller"></param>
            /// <param name="property"></param>
            /// <param name="logger"></param>
            public PropertyInvoker(Controller controller, PropertyInfo property,
                bool indexed, ILogger logger) {
                _logger = logger;
                _controller = controller;
                _indexed = indexed;
                _property = property;
            }

            /// <summary>
            /// Called when a service is invoked
            /// </summary>
            /// <param name="property"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public Controller Set(string property, dynamic value) {
                try {
                    var cast = Cast(value, _property.PropertyType);
                    if (_indexed) {
                        _property.SetValue(_controller.Target, cast,
                            new object[] { property });
                    }
                    else {
                        _property.SetValue(_controller.Target, cast);
                    }
                    return _controller;
                }
                catch (Exception e) {
                    _logger.Warn($"Exception during setter invocation ", () => new {
                        name = _controller.Target.GetType().Name,
                        method = _property.Name,
                        exception = e
                    });
                    throw e;
                }
            }

            /// <summary>
            /// Called when a service is invoked
            /// </summary>
            /// <param name="property"></param>
            /// <returns></returns>
            public bool Get(string property, out dynamic value) {
                try {
                    if (!_property.CanRead) {
                        value = null;
                        return false;
                    }
                    if (_indexed) {
                        value = _property.GetValue(_controller.Target,
                            new object[] { property });
                    }
                    else {
                        value = _property.GetValue(_controller.Target);
                    }
                    return true;
                }
                catch (Exception e) {
                    _logger.Warn($"Exception during getter invocation ", () => new {
                        name = _controller.Target.GetType().Name,
                        method = _property.Name,
                        exception = e
                    });
                    throw e;
                }
            }

            /// <summary>
            /// Cast to object of type
            /// </summary>
            /// <param name="type"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public object Cast(dynamic value, Type type) {
                if (value == null) {
                    return null;
                }
                JToken val;
                try {
                    val = (JToken)value;
                }
                catch {
                    val = JToken.FromObject(value);
                }
                if (type == typeof(JToken)) {
                    return val;
                }
                return val.ToObject(type);
            }

            private readonly ILogger _logger;
            private readonly Controller _controller;
            private readonly PropertyInfo _property;
            private readonly bool _indexed;
        }

        private const string kDefaultProp = "@default";
        private readonly ILogger _logger;
        private readonly Dictionary<string, CascadingInvoker> _calltable;
    }
}
