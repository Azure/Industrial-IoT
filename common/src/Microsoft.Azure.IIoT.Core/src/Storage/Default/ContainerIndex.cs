// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Default {
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A container based distributed incremental index
    /// </summary>
    public sealed class ContainerIndex : IContainerIndex {

        /// <summary>
        /// Create index in indices container
        /// </summary>
        /// <param name="db"></param>
        /// <param name="name"></param>
        public ContainerIndex(IItemContainerFactory db, string name = null) {
            if (string.IsNullOrEmpty(name)) {
                name = "default";
            }
            _container = db.OpenAsync("indices").Result;
            _indices = _container.AsDocuments();
            _id = $"__idx_doc_{name}__";
        }

        /// <summary>
        /// Create index on top of container
        /// </summary>
        /// <param name="container"></param>
        public ContainerIndex(IItemContainer container) {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _indices = _container.AsDocuments();
            _id = $"__idx_doc_{_container.Name}__";
        }

        /// <inheritdoc/>
        public async Task<uint> AllocateAsync(CancellationToken ct) {
            while (true) {
                // Get current value
                var cur = await _indices.FindAsync<Bitmap>(_id, ct);
                if (cur == null) {
                    // Add new index
                    try {
                        var idx = new Bitmap();
                        var value = idx.Allocate();
                        await _indices.AddAsync(idx, ct, _id,
                            kWithStrongConsistency);
                        return value;
                    }
                    catch (ConflictingResourceException) {
                        // Doc was added from another process/thread
                    }
                }
                else {
                    // Get next free index
                    try {
                        var idx = new Bitmap(cur.Value);
                        var value = idx.Allocate();
                        await _indices.ReplaceAsync(cur, idx, ct,
                            kWithStrongConsistency);
                        return value; // Success - return index
                    }
                    catch (ResourceOutOfDateException) {
                        // Etag is no match
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task FreeAsync(uint index, CancellationToken ct) {
            while (true) {
                // Get current value
                var cur = await _indices.FindAsync<Bitmap>(_id, ct,
                    kWithStrongConsistency);
                if (cur == null) {
                    return;
                }
                try {
                    var idx = new Bitmap(cur.Value);
                    if (idx.Free(index)) {
                        await _indices.ReplaceAsync(cur, idx, ct,
                            kWithStrongConsistency);
                    }
                    return;
                }
                catch (ResourceOutOfDateException) {
                    // Etag is no match - try again to free
                }
            }
        }

        private static readonly OperationOptions kWithStrongConsistency =
            new OperationOptions { Consistency = OperationConsistency.Strong };
        private readonly IItemContainer _container;
        private readonly IDocuments _indices;
        private readonly string _id;
    }
}
