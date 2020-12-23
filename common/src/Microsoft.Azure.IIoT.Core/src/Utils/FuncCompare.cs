// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic {

    /// <summary>
    /// Lambda comparer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class FuncCompare<T> : IEqualityComparer<T> {

        /// <summary>
        /// Create comparer
        /// </summary>
        /// <param name="comparer"></param>
        public FuncCompare(Func<T, T, bool> comparer)
            : this(comparer, t => 0) {
        }

        /// <summary>
        /// Create comparer
        /// </summary>
        /// <param name="comparer"></param>
        /// <param name="hash"></param>
        public FuncCompare(Func<T, T, bool> comparer,
            Func<T, int> hash) {
            _comparer = comparer;
            _hash = hash;
        }

        /// <inheritdoc/>
        public bool Equals(T x, T y) {
            return _comparer(x, y);
        }

        /// <inheritdoc/>
        public int GetHashCode(T obj) {
            return _hash(obj);
        }

        private readonly Func<T, T, bool> _comparer;
        private readonly Func<T, int> _hash;
    }
}
