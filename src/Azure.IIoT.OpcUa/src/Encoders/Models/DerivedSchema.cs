// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Models
{
    using Avro;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Derived schema
    /// </summary>
    internal class DerivedSchema : NamedSchema
    {
        /// <inheritdoc/>
        public DerivedSchema(Type type, SchemaName name,
            IList<string>? aliases = null, PropertyMap? props = null,
            SchemaNames? names = null, string? doc = null)
            : base(type, name, GetSchemaNames(aliases, name),
                  props, names ?? new SchemaNames(), doc)
        {
        }

        /// <summary>
        /// Create derived schema
        /// </summary>
        /// <param name="name"></param>
        /// <param name="baseSchema"></param>
        /// <param name="ns"></param>
        /// <param name="aliases"></param>
        /// <returns></returns>
        public static DerivedSchema Create(string name,
            Schema baseSchema, string ns, string[] aliases)
        {
            return new DerivedSchema(baseSchema.Tag,
                new SchemaName(name, ns, null, null), aliases);
        }

        internal static IList<SchemaName>? GetSchemaNames(
            IEnumerable<string>? aliases, SchemaName typeName)
        {
            if (aliases == null)
            {
                return null;
            }
            return aliases.Select(alias => new SchemaName(
                alias, typeName.Namespace, null, null)).ToList();
        }
    }

}
