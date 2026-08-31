// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic
{
    using System;
    using System.Collections;

    /// <summary>
    /// Lambda comparer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class FuncCompare<T> : IEqualityComparer<T>, IEqualityComparer
    {
        /// <summary>
        /// Create comparer.
        /// </summary>
        /// <param name="comparer"></param>
        public FuncCompare(Func<T?, T?, bool> comparer)
            : this(comparer, _ => 0)
        {
        }

        /// <summary>
        /// Create comparer.
        /// </summary>
        /// <param name="comparer"></param>
        /// <param name="hash"></param>
        public FuncCompare(Func<T?, T?, bool> comparer, Func<T, int> hash)
        {
            _comparer = comparer;
            _hash = hash;
        }

        /// <inheritdoc/>
        public bool Equals(T? x, T? y)
        {
            return _comparer(x, y);
        }

        /// <inheritdoc/>
        public int GetHashCode(T obj)
        {
            return _hash(obj);
        }

        /// <inheritdoc/>
        public new bool Equals(object? x, object? y)
        {
            if (x == y)
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }
            if (x is T a && y is T b)
            {
                return Equals(a, b);
            }
            throw new ArgumentException("Invalid type", nameof(x));
        }

        /// <inheritdoc/>
        public int GetHashCode(object? obj)
        {
            if (obj == null)
            {
                return 0;
            }
            if (obj is T x)
            {
                return GetHashCode(x);
            }
            throw new ArgumentException("Invalid type", nameof(obj));
        }

        private readonly Func<T?, T?, bool> _comparer;
        private readonly Func<T, int> _hash;
    }
}
