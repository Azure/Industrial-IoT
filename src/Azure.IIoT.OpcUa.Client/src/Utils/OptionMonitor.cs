// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Helper
    /// </summary>
    public static class OptionMonitor
    {
        /// <summary>
        /// Create monitor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IOptionsMonitor<T> Create<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(
            T options)
        {
            return new OptionMonitor<T>(options);
        }

        /// <summary>
        /// Create monitor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IOptionsMonitor<T> Create<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>()
            where T : new()
        {
            return new OptionMonitor<T>(new T());
        }
    }

    /// <summary>
    /// Options monitor adapter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OptionMonitor<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> :
        IOptionsMonitor<T>
    {
        /// <summary>
        /// Create
        /// </summary>
        /// <param name="options"></param>
        public OptionMonitor(T options)
        {
            _currentValue = options;
        }

        /// <inheritdoc/>
        public T CurrentValue
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                foreach (var listener in _listeners)
                {
                    listener.Value(value, null);
                }
            }
        }

        /// <inheritdoc/>
        public T Get(string? name)
        {
            return CurrentValue;
        }

        /// <inheritdoc/>
        public IDisposable? OnChange(Action<T, string?> listener)
        {
            return new Listener(this, listener);
        }

        /// <summary>
        /// Disposable listener
        /// </summary>
        private sealed class Listener : IDisposable
        {
            /// <inheritdoc/>
            public Listener(OptionMonitor<T> monitor,
                Action<T, string?> listener)
            {
                _monitor = monitor;
                _monitor._listeners.TryAdd(this, listener);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _monitor._listeners.TryRemove(this, out _);
            }
            private readonly OptionMonitor<T> _monitor;
        }

        private readonly ConcurrentDictionary<Listener, Action<T, string?>> _listeners = new();
        private T _currentValue;
    }
}
