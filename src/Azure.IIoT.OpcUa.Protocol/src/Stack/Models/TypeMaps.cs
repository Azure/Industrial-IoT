// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Caches constant identifier definitions of a generated type
    /// </summary>
    public sealed class TypeMaps {

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
        private TypeMaps(Type type) {
            var fields = type.GetFields(
                BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields) {
                try {
                    var value = (uint)field.GetValue(type);
                    _reverse.Add(field.Name, value);
                    _forward.Add(value, field.Name);
                }
                catch {
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
        public bool TryGetBrowseName(uint id, out string value) {
            return _forward.TryGetValue(id, out value);
        }

        /// <summary>
        /// Get identifier
        /// </summary>
        /// <param name="value"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool TryGetIdentifier(string value, out uint id) {
            return _reverse.TryGetValue(value, out id);
        }

        private readonly Dictionary<uint, string> _forward =
            new Dictionary<uint, string>();
        private readonly Dictionary<string, uint> _reverse =
            new Dictionary<string, uint>();

    }
}
