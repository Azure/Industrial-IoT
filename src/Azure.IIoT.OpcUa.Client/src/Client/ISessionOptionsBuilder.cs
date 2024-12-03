// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Builder
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISessionOptionsBuilder<T>
        where T : SessionOptions, new()
    {
        /// <summary>
        /// Set session name
        /// </summary>
        /// <param name="sessionName"></param>
        /// <returns></returns>
        ISessionOptionsBuilder<T> WithName(
            string sessionName);

        /// <summary>
        /// Set session timeout
        /// </summary>
        /// <param name="sessionTimeout"></param>
        /// <returns></returns>
        ISessionOptionsBuilder<T> WithTimeout(
            TimeSpan sessionTimeout);

        /// <summary>
        /// Set preferred locales
        /// </summary>
        /// <param name="preferredLocales"></param>
        /// <returns></returns>
        ISessionOptionsBuilder<T> WithPreferredLocales(
            IReadOnlyList<string> preferredLocales);

        /// <summary>
        /// Set keep alive interval
        /// </summary>
        /// <param name="keepAliveInterval"></param>
        /// <returns></returns>
        ISessionOptionsBuilder<T> WithKeepAliveInterval(
            TimeSpan keepAliveInterval);

        /// <summary>
        /// Set check domain
        /// </summary>
        /// <param name="checkDomain"></param>
        /// <returns></returns>
        ISessionOptionsBuilder<T> CheckDomain(
            bool checkDomain = true);

        /// <summary>
        /// Set disable complex type loading
        /// </summary>
        /// <param name="disableComplexTypeLoading"></param>
        /// <returns></returns>
        ISessionOptionsBuilder<T> DisableComplexTypeLoading(
            bool disableComplexTypeLoading = true);

        /// <summary>
        /// Set disable complex type preloading
        /// </summary>
        /// <param name="disableComplexTypePreloading"></param>
        /// <returns></returns>
        ISessionOptionsBuilder<T> DisableComplexTypePreloading(
            bool disableComplexTypePreloading = true);
    }
}
