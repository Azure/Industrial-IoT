// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.Devices.Shared;
    using Serilog;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Provides set/get routing to controllers
    /// </summary>
    public sealed class SettingsRouter : ISettingsRouter, IDisposable {

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
            _cache = new Dictionary<string, JToken>();
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _lock.Dispose();
        }

        /// <inheritdoc/>
        public async Task<TwinCollection> ProcessSettingsAsync(TwinCollection settings) {
            var controllers = new List<Controller>();

            // Set all properties
            foreach (KeyValuePair<string, dynamic> setting in settings) {
                if (!TryGetInvoker(setting.Key, out var invoker)) {
                    _logger.Error("Setting {key}/{value} unsupported",
                        setting.Key, setting.Value);
                }
                else {
                    try {
                        var controller = invoker.Set(setting.Key, setting.Value);
                        if (controller != null && !controllers.Contains(controller)) {
                            controllers.Add(controller); // To apply only affected controllers
                        }
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Error processing setting", setting);
                    }
                }
            }

            // Apply settings on all affected controllers
            if (controllers.Any()) {
                var sw = Stopwatch.StartNew();
                await Task.WhenAll(controllers.Select(c => c.SafeApplyAsync()));
                _logger.Debug("Applying new settings took {elapsed}...", sw.Elapsed);
            }

            await _lock.WaitAsync();
            try {
                // Gather current values from controller
                var reported = new TwinCollection();
                foreach (KeyValuePair<string, dynamic> setting in settings) {
                    if (TryGetInvoker(setting.Key, out var invoker)) {
                        try {
                            if (!invoker.Get(setting.Key, out var value)) {
                                value = setting.Value; // No getter, echo back desired value.
                            }
                            _cache[setting.Key] = JToken.FromObject(value);
                            reported[setting.Key] = value;
                            continue;
                        }
                        catch (Exception ex) {
                            _logger.Error(ex, "Error retrieving reported setting {setting}",
                                setting);
                            // Clear value...
                        }
                    }
                    // Clear value
                    reported[setting.Key] = null;
                    _cache[setting.Key] = null;
                }
                // Collect current values of all other settings
                var remaining = _calltable.Where(v => !settings.Contains(v.Key));
                CollectSettingsFromControllers(reported, remaining);
                return reported;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<TwinCollection> GetSettingsChangesAsync() {
            await _lock.WaitAsync();
            try {
                var reported = new TwinCollection();
                CollectSettingsFromControllers(reported, _calltable);
                return reported;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Get cached invoker
        /// </summary>
        /// <param name="key"></param>
        /// <param name="invoker"></param>
        /// <returns></returns>
        private bool TryGetInvoker(string key, out CascadingInvoker invoker) {
            return _calltable.TryGetValue(key.ToLowerInvariant(), out invoker) ||
                   _calltable.TryGetValue(kDefaultProp, out invoker);
        }

        /// <summary>
        /// Collect settings from the invokers
        /// </summary>
        /// <param name="reported"></param>
        /// <param name="invokers"></param>
        private void CollectSettingsFromControllers(TwinCollection reported,
            IEnumerable<KeyValuePair<string, CascadingInvoker>> invokers) {
            foreach (var handler in invokers) {
                try {
                    var key = handler.Value.Name;
                    if (string.IsNullOrEmpty(key)) {
                        continue; // DO not return default indexer values
                    }
                    if (!handler.Value.Get(key, out var value)) {
                        continue;
                    }
                    var obj = value == null ? null : JToken.FromObject(value);
                    _cache.TryGetValue(key.ToLowerInvariant(), out var cached);
                    if (cached == null && value == null) {
                        // Do not report - both are null and thus equal
                        continue;
                    }
                    if (cached != null && value != null &&
                        JToken.DeepEquals(cached, obj)) {
                        // Value is equal - do not report
                        continue;
                    }
                    _cache[key.ToLowerInvariant()] = obj;
                    reported[key] = value;
                }
                catch (Exception ex) {
                    _logger.Debug(ex, "Error retrieving controller setting {setting}",
                        handler.Key);
                }
            }
        }

        /// <summary>
        /// Add target to calltable
        /// </summary>
        /// <param name="target"></param>
        private void AddToCallTable(object target) {

            var versions = target.GetType().GetCustomAttributes<VersionAttribute>(true)
                .Select(v => v.Numeric)
                .ToList();
            if (versions.Count == 0) {
                versions.Add(ulong.MaxValue);
            }
            foreach (var version in versions) {
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
                    if (propInfo.GetCustomAttribute<IgnoreAttribute>() != null) {
                        // Should be ignored
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
            public async Task SafeApplyAsync() {
                try {
                    await ApplyInternalAsync();
                }
                catch (Exception e) {
                    _logger.Error(e, "Exception applying changes! Continue...",
                        Target.GetType().Name, _applyMethod.Name);
                }
            }

            /// <summary>
            /// Called to apply changes
            /// </summary>
            /// <returns></returns>
            public Task ApplyInternalAsync() {
                try {
                    if (_applyMethod == null) {
                        return Task.CompletedTask;
                    }
                    return (Task)_applyMethod.Invoke(Target, new object[] { });
                }
                catch (Exception e) {
                    return Task.FromException(e);
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
            /// Property name
            /// </summary>
            public string Name => _invokers.Values
                .FirstOrDefault(p => !string.IsNullOrEmpty(p.Name))?
                .Name;

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
                        _logger.Debug(ex, "Setting '{property}' failed!",
                            property);
                        e = ex;
                    }
                }
                _logger.Error(e, "Exception during setter invocation.");
                throw e;
            }

            /// <summary>
            /// Called to read setting
            /// </summary>
            /// <param name="property"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public bool Get(string property, out dynamic value) {
                Exception e = null;
                foreach (var invoker in _invokers) {
                    try {
                        return invoker.Value.Get(property, out value);
                    }
                    catch (Exception ex) {
                        // Save last error, and continue
                        _logger.Debug(ex, "Retrieving '{property}' failed!",
                            property);
                        e = ex;
                    }
                }
                _logger.Error(e, "Exception during getter invocation.");
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
            /// Property name
            /// </summary>
            public string Name => _property.Name.EqualsIgnoreCase("item") ?
                null : _property.Name;

            /// <summary>
            /// Create invoker
            /// </summary>
            /// <param name="controller"></param>
            /// <param name="property"></param>
            /// <param name="indexed"></param>
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
                    _logger.Warning(e,
                        "Exception during setter {controller} {name} invocation",
                        _controller.Target.GetType().Name, _property.Name);
                    throw e;
                }
            }

            /// <summary>
            /// Called when a service is invoked
            /// </summary>
            /// <param name="property"></param>
            /// <param name="value"></param>
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
                    _logger.Warning(e,
                        "Exception during getter {controller} {name} invocation",
                        _controller.Target.GetType().Name, _property.Name);
                    throw e;
                }
            }

            /// <summary>
            /// Cast to object of type
            /// </summary>
            /// <param name="value"></param>
            /// <param name="type"></param>
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
        private readonly Dictionary<string, JToken> _cache;
        private readonly Dictionary<string, CascadingInvoker> _calltable;
        private readonly SemaphoreSlim _lock;
    }
}
