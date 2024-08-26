// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Asset
{
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Diagnostics.CodeAnalysis;

    public delegate void OnAssetTagChange(AssetTag tag,
        object? value, StatusCode statusCode, DateTime timestamp);

    /// <summary>
    /// Asset interface
    /// </summary>
    public interface IAsset : IDisposable
    {
        /// <summary>
        /// Read from the asset tag
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        ServiceResult Read(AssetTag tag, ref object? value);

        /// <summary>
        /// Write to the asset tag
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        ServiceResult Write(AssetTag tag, ref object value);

        /// <summary>
        /// Observe
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="id"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"/>
        void Observe(AssetTag tag, uint id, OnAssetTagChange callback);

        /// <summary>
        /// Unobserve
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"/>
        void Unobserve(AssetTag tag, uint id);
    }

    /// <summary>
    /// Creates assets
    /// </summary>
    public interface IAssetFactory
    {
        /// <summary>
        /// Try connect
        /// </summary>
        /// <param name="tdBase"></param>
        /// <param name="logger"></param>
        /// <param name="asset"></param>
        /// <returns></returns>
        public abstract static bool TryConnect(Uri tdBase,
            ILogger logger, [NotNullWhen(true)] out IAsset? asset);
    }
}
