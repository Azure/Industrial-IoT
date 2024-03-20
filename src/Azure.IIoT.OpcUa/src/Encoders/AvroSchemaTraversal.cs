// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using global::Avro;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Allows a encoder or decoder to follow the schema
    /// </summary>
    /// <returns></returns>
    internal sealed class AvroSchemaTraversal
    {
        /// <summary>
        /// Create traversal
        /// </summary>
        /// <param name="schema"></param>
        public AvroSchemaTraversal(Schema schema)
        {
            var list = new List<(string?, Schema)>();
            Flatten((null, schema), list);
            _schemas = new Queue<(string?, Schema)>(list);
        }

        /// <summary>
        /// Split
        /// </summary>
        /// <param name="original"></param>
        private AvroSchemaTraversal(AvroSchemaTraversal original)
        {
            _schemas = new Queue<(string?, Schema)>(
                original._schemas.ToList());
        }

        /// <summary>
        /// Fork traversal to create a safe path
        /// </summary>
        /// <returns></returns>
        public AvroSchemaTraversal Fork()
        {
            return new AvroSchemaTraversal(this);
        }

        /// <summary>
        /// Flatten depth first like we will travers
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="flat"></param>
        private static void Flatten((string?, Schema) schema, List<(string?, Schema)> flat)
        {
            flat.Add(schema);
            var (_, s) = schema;
            switch (s)
            {
                case RecordSchema r:
                    foreach (var f in r.Fields)
                    {
                        Flatten((f.Name, f.Schema), flat);
                    }
                    break;
                case MapSchema m:
                    Flatten((null, m.ValueSchema), flat);
                    break;
            }
        }

        /// <summary>
        /// Try get the next field schema
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        public bool TryPop(string? fieldName, [NotNullWhen(true)] out Schema? schema)
        {
            if (!_schemas.TryDequeue(out var s) ||
                (fieldName != null && fieldName != s.Item1))
            {
                schema = null;
                return false;
            }
            schema = s.Item2;
            return true;
        }

        /// <summary>
        /// Try to peek the next schema and field value
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public bool TryPeek(out Schema? schema, out string? fieldName)
        {
            var result = _schemas.TryPeek(out var s);
            fieldName = s.Item1;
            schema = s.Item2;
            return result;
        }

        /// <summary>
        /// Finalize
        /// </summary>
        public bool IsDone()
        {
            return _schemas.Count != 0;
        }

        private readonly Queue<(string?, Schema)> _schemas;
    }
}
