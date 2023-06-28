// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
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
        /// <param name="serializer"></param>
        public SettingsRouter(IJsonSerializer serializer, ILogger logger) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _calltable = new Dictionary<string, CascadingInvoker>();
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _lock.Dispose();
        }

        /// <inheritdoc/>
        public async Task<IDictionary<string, VariantValue>> ProcessSettingsAsync(
            IDictionary<string, VariantValue> settings) {
            var controllers = new List<Controller>();

            // Set all properties
            foreach (var setting in settings) {
                if (!TryGetInvoker(setting.Key, out var invoker)) {
                    _logger.Error("Setting {key}/{value} unsupported",
                        setting.Key, setting.Value.ToJson());
                }
                else {
                    try {
                        var controller = invoker.Set(setting.Key, setting.Value);
                        if (controller != null && !controllers.Contains(controller)) {
                            controllers.Add(controller); // To apply only affected controllers
                        }
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Error processing setting {key}/{value}",
                            setting.Key, setting.Value.ToJson());
                    }
                }
            }

            // Apply settings on all affected controllers and return reported
            var reported = new Dictionary<string, VariantValue>();
            if (controllers.Any()) {
                var sw = Stopwatch.StartNew();
                await _lock.WaitAsync();
                try {
                    await Task.WhenAll(controllers.Select(c => c.SafeApplyAsync()));
                    var invokers = controllers.SelectMany(c => c.Invokers).Distinct();
                    CollectSettingsFromControllers(reported, invokers);
                    _logger.Debug("Applying new settings took {elapsed}...", sw.Elapsed);
                }
                finally {
                    _lock.Release();
                }
            }
            return reported;
        }

        /// <inheritdoc/>
        public async Task<IDictionary<string, VariantValue>> GetSettingsStateAsync() {
            await _lock.WaitAsync();
            try {
                var reported = new Dictionary<string, VariantValue>();
                CollectSettingsFromControllers(reported, _calltable.Values);
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
        private void CollectSettingsFromControllers(Dictionary<string, VariantValue> reported,
            IEnumerable<CascadingInvoker> invokers) {
            foreach (var handler in invokers) {
                try {
                    if (string.IsNullOrEmpty(handler.Name)) {
                        // Get all indexes and retrieve them one by one
                        if (handler.GetIndexed(out var result)) {
                            foreach (var item in result) {
                                reported.AddOrUpdate(item.Key, item.Value);
                            }
                        }
                        continue;
                    }

                    if (handler.Get(handler.Name, out var value)) {
                        reported.AddOrUpdate(handler.Name, value);
                    }
                }
                catch (Exception ex) {
                    _logger.Debug(ex, "Error retrieving controller setting {setting}",
                        handler.Name);
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
                    MethodInfo indexer = null;
                    if (indexers.Length == 1 && indexers[0].ParameterType == typeof(string)) {
                        // save .net indexer as default
                        if (name == "item") {
                            name = kDefaultProp;
                        }

                        // Get property name enumerator on controller if any
                        indexer = target.GetType().GetMethod("GetPropertyNames");
                        if (indexer != null) {
                            if (indexer.GetParameters().Length != 0 ||
                                indexer.ReturnType != typeof(IEnumerable<string>)) {
                                indexer = null;
                            }
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
                    invoker.Add(controller, propInfo, _serializer, indexed, indexer);
                    controller.Add(invoker);
                }
            }
        }

        /// <summary>
        /// Wraps a controller
        /// </summary>
        private class Controller {

            /// <summary>
            /// Attached invokers
            /// </summary>
            public IEnumerable<CascadingInvoker> Invokers => _attachedTo;

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
                    return (Task)_applyMethod.Invoke(Target, Array.Empty<object>());
                }
                catch (Exception e) {
                    return Task.FromException(e);
                }
            }

            /// <summary>
            /// Add invoker found on controller
            /// </summary>
            /// <param name="invoker"></param>
            internal void Add(CascadingInvoker invoker) {
                _attachedTo.Add(invoker);
            }

            private readonly ILogger _logger;
            private readonly MethodInfo _applyMethod;
            private readonly List<CascadingInvoker> _attachedTo =
                new List<CascadingInvoker>();
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
            /// <param name="serializer"></param>
            /// <param name="indexed"></param>
            /// <param name="indexer"></param>
            public void Add(Controller controller, PropertyInfo controllerProp,
                IJsonSerializer serializer, bool indexed, MethodInfo indexer) {
                _invokers.Add(controller.Version, new PropertyInvoker(controller,
                    controllerProp, indexed, indexer, serializer, _logger));
            }

            /// <summary>
            /// Called when a setting is to be set
            /// </summary>
            /// <param name="property"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public Controller Set(string property, VariantValue value) {
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
            public bool Get(string property, out VariantValue value) {
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

            /// <summary>
            /// Returns all indexer values
            /// </summary>
            /// <param name="result"></param>
            /// <returns></returns>
            public bool GetIndexed(out IEnumerable<KeyValuePair<string, VariantValue>> result) {
                Exception e = null;
                foreach (var invoker in _invokers) {
                    try {
                        return invoker.Value.GetIndexed(out result);
                    }
                    catch (Exception ex) {
                        // Save last error, and continue
                        _logger.Debug(ex, "Retrieving indexed values failed!");
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
            /// <param name="indexer"></param>
            /// <param name="serializer"></param>
            /// <param name="logger"></param>
            public PropertyInvoker(Controller controller, PropertyInfo property,
                bool indexed, MethodInfo indexer, IJsonSerializer serializer, ILogger logger) {
                _serializer = serializer;
                _logger = logger;
                _controller = controller;
                _indexed = indexed;
                _indexer = indexer;
                _property = property;
            }

            /// <summary>
            /// Called when a service is invoked
            /// </summary>
            /// <param name="property"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public Controller Set(string property, VariantValue value) {
                try {
                    var cast = value.ConvertTo(_property.PropertyType);
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
                    throw;
                }
            }

            /// <summary>
            /// Called when a service is invoked
            /// </summary>
            /// <param name="property"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public bool Get(string property, out VariantValue value) {
                try {
                    if (!_property.CanRead) {
                        value = _serializer.FromObject(null);
                        return false;
                    }
                    object gotten;
                    if (_indexed) {
                        gotten = _property.GetValue(_controller.Target,
                            new object[] { property });
                    }
                    else {
                        gotten = _property.GetValue(_controller.Target);
                    }
                    value = _serializer.FromObject(gotten);
                    return true;
                }
                catch (Exception e) {
                    _logger.Warning(e,
                        "Exception during getter {controller} {name} invocation",
                        _controller.Target.GetType().Name, _property.Name);
                    throw;
                }
            }

            /// <summary>
            /// Returns all indexed values
            /// </summary>
            /// <param name="values"></param>
            /// <returns></returns>
            public bool GetIndexed(out IEnumerable<KeyValuePair<string, VariantValue>> values) {
                try {
                    if (_property.CanRead && _indexed && _indexer != null) {
                        // Get property names
                        var indexes = _indexer.Invoke(_controller.Target, Array.Empty<object>());
                        if (indexes is IEnumerable<string> properties) {
                            var results = new Dictionary<string, VariantValue>();

                            foreach (var property in properties) {
                                if (Get(property, out var value)) {
                                    results.AddOrUpdate(property, value);
                                }
                            }
                            values = results;
                            return true;
                        }
                    }
                    values = null;
                    return false;
                }
                catch (Exception e) {
                    _logger.Warning(e,
                        "Exception collecting all indexed values on {controller}.",
                        _controller.Target.GetType().Name);
                    throw;
                }
            }

            private readonly ILogger _logger;
            private readonly IJsonSerializer _serializer;
            private readonly Controller _controller;
            private readonly PropertyInfo _property;
            private readonly bool _indexed;
            private readonly MethodInfo _indexer;
        }

        private const string kDefaultProp = "@default";
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly Dictionary<string, CascadingInvoker> _calltable;
        private readonly SemaphoreSlim _lock;
    }
}
