// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Utils
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;

    /// <summary>
    /// Caches constant identifier definitions of a generated type
    /// </summary>
    public sealed class TypeMaps
    {
        /// <summary>
        /// Data types type map
        /// </summary>
        public static Lazy<TypeMaps> DataTypes { get; } =
            new Lazy<TypeMaps>(() => new TypeMaps(typeof(DataTypes)), true);

        /// <summary>
        /// Reference types type map
        /// </summary>
        public static Lazy<TypeMaps> ReferenceTypes { get; } =
            new Lazy<TypeMaps>(() => new TypeMaps(typeof(ReferenceTypes)), true);

        /// <summary>
        /// Object types type map
        /// </summary>
        public static Lazy<TypeMaps> ObjectTypes { get; } =
            new Lazy<TypeMaps>(() => new TypeMaps(typeof(ObjectTypes)), true);

        /// <summary>
        /// Attributes type map
        /// </summary>
        public static Lazy<TypeMaps> Attributes { get; } =
            new Lazy<TypeMaps>(() => new TypeMaps(typeof(Attributes)), true);

        /// <summary>
        /// Attributes type map
        /// </summary>
        public static Lazy<TypeMaps> StatusCodes { get; } =
            new Lazy<TypeMaps>(() => new TypeMaps(typeof(StatusCodes)), true);

        /// <summary>
        /// Identifiers
        /// </summary>
        public IEnumerable<uint> Identifiers => _forward.Keys;

        /// <summary>
        /// Identifiers
        /// </summary>
        public IEnumerable<string> BrowseNames => _reverse.Keys;

        /// <summary>
        /// Initialize map
        /// </summary>
        /// <param name="type"></param>
        private TypeMaps(Type type)
        {
            var fields = type.GetFields(
                BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                try
                {
                    var value = (uint?)field.GetValue(type);
                    if (value.HasValue)
                    {
                        _reverse.Add(field.Name, value.Value);
                        _forward.Add(value.Value, field.Name);
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Get browse name
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetBrowseName(uint id, [NotNullWhen(true)] out string? value)
        {
            if (_forward.TryGetValue(id, out value) && value != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get identifier
        /// </summary>
        /// <param name="value"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool TryGetIdentifier(string value, out uint id)
        {
            return _reverse.TryGetValue(value, out id);
        }

        private readonly SortedDictionary<uint, string> _forward = [];
        private readonly SortedDictionary<string, uint> _reverse = [];
    }
}
