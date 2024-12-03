// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Immutable relative path for lookups
    /// </summary>
    internal readonly struct ImmutableRelativePath : IEquatable<ImmutableRelativePath>
    {
        /// <summary>
        /// Path
        /// </summary>
        public IReadOnlyList<string> Path { get; }

        /// <summary>
        /// Empty
        /// </summary>
        public static readonly ImmutableRelativePath Empty =
            new(Array.Empty<string>());

        /// <summary>
        /// Create browse path
        /// </summary>
        /// <param name="path"></param>
        public ImmutableRelativePath(IReadOnlyList<string> path)
        {
            var result = new HashCode();
            foreach (var element in path)
            {
                result.Add(element);
            }
            _hashCode = result.ToHashCode();
            Path = path;
        }

        /// <summary>
        /// Create path from parent path and path entry
        /// </summary>
        /// <param name="parentPath"></param>
        /// <param name="browseName"></param>
        /// <returns></returns>
        public static ImmutableRelativePath Create(IReadOnlyList<string>? parentPath,
            string browseName)
        {
            var browsePath = parentPath != null ?
                new List<string>(parentPath) : [];
            browsePath.Add(browseName);
            return new ImmutableRelativePath(browsePath);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is ImmutableRelativePath path)
            {
                return Equals(path);
            }
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ImmutableRelativePath other)
        {
            if (other.Path.Count != Path.Count)
            {
                return false;
            }
            for (var i = 0; i < Path.Count; i++)
            {
                if (Path[i] != other.Path[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return Path.Aggregate((a, b) => a + b);
        }

        private readonly int _hashCode;
    }
}
