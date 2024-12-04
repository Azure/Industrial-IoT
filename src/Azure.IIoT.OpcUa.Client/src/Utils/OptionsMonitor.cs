// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Create options
/// </summary>
public static class OptionsFactory
{
    /// <summary>
    /// Create monitor
    /// </summary>
    /// <param name="options"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static OptionsMonitor<T> Create<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(
        T options)
    {
        return new OptionsMonitor<T>(options);
    }

    /// <summary>
    /// Create monitor
    /// </summary>
    /// <param name="configure"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static OptionsMonitor<T> Create<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(
        Action<T>? configure = null) where T : new()
    {
        var options = new OptionsMonitor<T>(new T());
        configure?.Invoke(options.CurrentValue);
        return options;
    }
}

/// <summary>
/// Options monitor adapter
/// </summary>
/// <typeparam name="T"></typeparam>
public class OptionsMonitor<
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> :
    IOptionsMonitor<T>
{
    /// <summary>
    /// Create options
    /// </summary>
    /// <param name="option"></param>
    public OptionsMonitor(T option)
    {
        _currentValue = option;
    }

    /// <summary>
    /// Configure options
    /// </summary>
    /// <param name="configure"></param>
    public OptionsMonitor<T> Configure(Func<T, T> configure)
    {
        CurrentValue = configure(_currentValue);
        return this;
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
        public Listener(OptionsMonitor<T> monitor,
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
        private readonly OptionsMonitor<T> _monitor;
    }

    private readonly ConcurrentDictionary<Listener, Action<T, string?>> _listeners = new();
    private T _currentValue;
}
