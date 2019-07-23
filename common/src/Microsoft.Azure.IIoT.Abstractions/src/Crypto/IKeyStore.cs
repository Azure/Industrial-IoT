// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages private keys and provides key operations
    /// </summary>
    public interface IKeyStore : IDigestSigner {

        /// <summary>
        /// Create a new key using the specified arguments and returns
        /// the handle to use to refer to the key.
        /// </summary>
        /// <param name="name">Name of the key</param>
        /// <param name="create">How to create</param>
        /// <param name="store">How to store</param>
        /// <param name="ct"></param>
        /// <exception cref="ConflictingResourceException"></exception>
        /// <returns></returns>
        Task<KeyHandle> CreateKeyAsync(string name, CreateKeyParams create,
            KeyStoreProperties store = null, CancellationToken ct = default);

        /// <summary>
        /// Imports a Key under a particular name
        /// </summary>
        /// <param name="name">Name of the key</param>
        /// <param name="key">Key</param>
        /// <param name="store">How to store</param>
        /// <param name="ct"></param>
        /// <exception cref="ConflictingResourceException"></exception>
        /// <returns></returns>
        Task<KeyHandle> ImportKeyAsync(string name, Key key,
            KeyStoreProperties store = null, CancellationToken ct = default);

        /// <summary>
        /// Get key handle
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ct"></param>
        /// <exception cref="ResourceNotFoundException"></exception>
        /// <returns></returns>
        Task<KeyHandle> GetKeyHandleAsync(string name,
            CancellationToken ct = default);

        /// <summary>
        /// Get public part of an asymmetric key
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Key> GetPublicKeyAsync(KeyHandle handle,
            CancellationToken ct = default);

        /// <summary>
        /// Export exportable key
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ct"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <returns></returns>
        Task<Key> ExportKeyAsync(KeyHandle handle,
            CancellationToken ct = default);

        /// <summary>
        /// Accept Private Key with key Id
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DisableKeyAsync(KeyHandle handle,
            CancellationToken ct = default);

        /// <summary>
        /// Delete Private Key with key Id
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeleteKeyAsync(KeyHandle handle,
            CancellationToken ct = default);
    }
}