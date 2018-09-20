// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Default {
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Stores in memory (no persistence)
    /// </summary>
    public class MemoryPersistence : IPersistenceProvider {

        /// <summary>
        /// Create in memory persistence provider
        /// </summary>
        /// <param name="logger"></param>
        public MemoryPersistence(ILogger logger) {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            _lock = new SemaphoreSlim(1);
            _master = new Dictionary<string, dynamic>();
        }

        /// <inheritdoc/>
        public async Task WriteAsync(IDictionary<string, dynamic> values) {
            await _lock.WaitAsync();
            try {
                foreach (var value in values) {
                    if (value.Value == null) {
                        _master.Remove(value.Key);
                    }
                    else if (_master.ContainsKey(value.Key)) {
                        _master[value.Key] = value.Value;
                    }
                    else {
                        _master.Add(value.Key, value.Value);
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<dynamic> ReadAsync(string key) {
            await _lock.WaitAsync();
            try {
                if (_master.TryGetValue(key, out var result)) {
                    return result;
                }
                return null;
            }
            finally {
                _lock.Release();
            }
        }

        private readonly Dictionary<string, dynamic> _master;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _lock;
    }
}
