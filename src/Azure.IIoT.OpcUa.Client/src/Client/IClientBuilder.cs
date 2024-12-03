// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Opc.Ua.Configuration;
    using System;

    /// <inheritdoc/>
    public interface IClientBuilder<T, S, C, O> :
        IDependencyInjectionBuilder
        where T : PooledSessionOptions, new()
        where S : SessionOptions, new()
        where C : SessionCreateOptions, new()
        where O : ClientOptions, new()
    {
        /// <summary>
        /// Set whether the client is a client
        /// and server
        /// </summary>
        /// <returns></returns>
        IApplicationNameBuilder<T, S, C, O> NewClientServer { get; }

        /// <summary>
        /// Set whether the client is a client
        /// </summary>
        /// <returns></returns>
        IApplicationNameBuilder<T, S, C, O> NewClient { get; }
    }

    /// <inheritdoc/>
    public interface IApplicationNameBuilder<T, S, C, O> :
        IDependencyInjectionBuilder
        where T : PooledSessionOptions, new()
        where S : SessionOptions, new()
        where C : SessionCreateOptions, new()
        where O : ClientOptions, new()
    {
        /// <summary>
        /// Set application name
        /// </summary>
        /// <param name="applicationName"></param>
        /// <returns></returns>
        IApplicationUriBuilder<T, S, C, O> WithName(
            string applicationName);
    }

    /// <inheritdoc/>
    public interface IApplicationUriBuilder<T, S, C, O> :
        IDependencyInjectionBuilder
        where T : PooledSessionOptions, new()
        where S : SessionOptions, new()
        where C : SessionCreateOptions, new()
        where O : ClientOptions, new()
    {
        /// <summary>
        /// Set application uri
        /// </summary>
        /// <param name="applicationUri"></param>
        /// <returns></returns>
        IProductBuilder<T, S, C, O> WithUri(
            string applicationUri);
    }

    /// <inheritdoc/>
    public interface IProductBuilder<T, S, C, O> :
        IDependencyInjectionBuilder
        where T : PooledSessionOptions, new()
        where S : SessionOptions, new()
        where C : SessionCreateOptions, new()
        where O : ClientOptions, new()
    {
        /// <summary>
        /// Set product uri
        /// </summary>
        /// <param name="productUri"></param>
        /// <returns></returns>
        IApplicationConfigurationBuilder<T, S, C, O> WithProductUri(
            string productUri);
    }

    /// <inheritdoc/>
    public interface IApplicationConfigurationBuilder<T, S, C, O> :
        IDependencyInjectionBuilder
        where T : PooledSessionOptions, new()
        where S : SessionOptions, new()
        where C : SessionCreateOptions, new()
        where O : ClientOptions, new()
    {
        /// <summary>
        /// Configure the application
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        IApplicationConfigurationBuilder<T, S, C, O> WithConfiguration(
            Action<IApplicationConfigurationBuilderClientOptions> configure);

        /// <summary>
        /// Configure the security settings
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        IApplicationConfigurationBuilder<T, S, C, O> WithSecuritySetting(
            Action<IApplicationConfigurationBuilderSecurity> configure);

        /// <summary>
        /// Configure the transport options
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        IApplicationConfigurationBuilder<T, S, C, O> WithTransportQuota(
            Action<IApplicationConfigurationBuilderTransportQuotas> configure);

        /// <summary>
        /// Set options
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        IApplicationConfigurationBuilder<T, S, C, O> WithOption(
            Action<IClientOptionsBuilder<O>> configure);

        /// <summary>
        /// Build session builder
        /// </summary>
        /// <returns></returns>
        ISessionBuilder<T, S, C> Build();
    }
}
