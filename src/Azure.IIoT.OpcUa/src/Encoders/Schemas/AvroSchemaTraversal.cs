// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Avro;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Allows a encoder or decoder to follow the schema
    /// </summary>
    /// <returns></returns>
    public class AvroSchemaTraversal
    {
        /// <summary>
        /// Create traversal
        /// </summary>
        /// <param name="schema"></param>
        public AvroSchemaTraversal(Schema schema)
        {
            var list = new List<Schema>();
            Flatten(schema, list);
            _schemas = new Queue<Schema>(list);
        }

        /// <summary>
        /// Split
        /// </summary>
        /// <param name="original"></param>
        private AvroSchemaTraversal(AvroSchemaTraversal original)
        {
            _schemas = new Queue<Schema>(original._schemas.ToList());
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
        private static void Flatten(Schema schema, List<Schema> flat)
        {
            switch (schema)
            {
                case RecordSchema r:
                    foreach (var f in r.Fields)
                    {
                        flat.Add(f.Schema);
                        Flatten(f.Schema, flat);
                    }
                    break;
                case UnionSchema u:
                    foreach (var f in u.Schemas)
                    {
                        flat.Add(f);
                        Flatten(f, flat);
                    }
                    break;
                case MapSchema m:
                    flat.Add(m.ValueSchema);
                    Flatten(m.ValueSchema, flat);
                    break;
                default:
                    flat.Add(schema);
                    break;
            }
        }

        /// <summary>
        /// Pick the next schema in depth first traversal
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Schema Next()
        {
            if (!_schemas.TryDequeue(out var s))
            {
                throw new ArgumentException("Not found");
            }
            return s;
        }

        /// <summary>
        /// Get union schema
        /// </summary>
        /// <param name="unionFieldIndex"></param>
        /// <param name="unionCount"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Schema GetUnionSchema(int unionFieldIndex, int unionCount)
        {
            Schema? schema = null;
            for (var i = 0; i < unionCount; i++)
            {
                var s = Next();
                if (unionFieldIndex == i)
                {
                    // This is the one
                    schema = s;
                }
            }
            return schema ?? throw new ArgumentException("Not found");
        }

        /// <summary>
        /// Finalize
        /// </summary>
        public bool IsDone()
        {
            return _schemas.Count != 0;
        }

        private readonly Queue<Schema> _schemas;
    }

}
