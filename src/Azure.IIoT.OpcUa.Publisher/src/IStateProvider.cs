// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for state storage
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IStateProvider<T>
    {
        /// <summary>
        /// Store state
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<bool> StoreAsync(string key,
            T value, CancellationToken ct = default);

        /// <summary>
        /// Load state
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<T?> LoadAsync(string key,
            CancellationToken ct = default);

        /// <summary>
        /// Remove state
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask RemoveAsync(string key,
            CancellationToken ct = default);
    }
}
