// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Encoders.Schemas.Avro;
    using Avro;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Allows a encoder or decoder to follow the schema
    /// </summary>
    internal sealed class AvroSchemaTraverser
    {
        /// <summary>
        /// Current schema
        /// </summary>
        public Schema Current => _schemas.Peek().Schema;

        /// <summary>
        /// Set Expected field name
        /// </summary>
        public string? ExpectedFieldName { get; set; }

        /// <summary>
        /// Set union selector
        /// </summary>
        public Func<UnionSchema, Schema?>? ExpectUnionItem { get; set; }

        /// <summary>
        /// Create traversal
        /// </summary>
        /// <param name="schema"></param>
        public AvroSchemaTraverser(Schema schema)
        {
            //
            // We need to push a root schema to start traversal
            // Either this is already a root schema for example
            // if the value was not a record or a record on a
            // field or we create an artificial one using array
            // schema to start moving to the actual schema the
            // first time move next is called.
            //
            Push(schema.IsRoot() ? schema : schema.AsArray());
        }

        /// <summary>
        /// Try to continue traversal to next schema
        /// </summary>
        /// <returns></returns>
        public bool TryMoveNext()
        {
            return _schemas.TryPeek(out var s) && s.TryMoveNext();
        }

        /// <summary>
        /// Push schema on stack
        /// </summary>
        /// <param name="schema"></param>
        public void Push(Schema schema)
        {
            if (_types.TryGetValue(schema.Fullname, out var seen))
            {
                //
                // Partial types must be replaced with full type
                // to handle recursive declarations correctly.
                //
                schema = seen;
            }
            else if (schema is RecordSchema record)
            {
                // Add the record to the list of types
                _types.AddOrUpdate(record.Fullname, record);
            }

            _schemas.Push(schema switch
            {
                RecordSchema r => new RecordTraverser(this, r),
                ArraySchema a => new ArrayTraverser(this, a),
                UnionSchema u => new UnionTraverser(this, u),
                _ => new Traverser(this, schema)
            });
        }

        /// <summary>
        /// Validate we completed traversal
        /// </summary>
        public bool IsDone()
        {
            Debug.Assert(_schemas.Count > 0);
            if (_schemas.Count == 1)
            {
                // See constructor
                var root = _schemas.Peek();
                return root.Schema is ArraySchema || root.Schema.IsRoot();
            }
            return false;
        }

        /// <summary>
        /// Allows to pop top traverser from stack
        /// </summary>
        public Schema Pop()
        {
            var traverser = _schemas.Pop();
            return traverser.Schema;
        }

        /// <summary>
        /// Generic traversal
        /// </summary>
        private class Traverser
        {
            /// <summary>
            /// Schema
            /// </summary>
            public Schema Schema { get; }

            /// <summary>
            /// Create record traverers
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="schema"></param>
            public Traverser(AvroSchemaTraverser outer, Schema schema)
            {
                _outer = outer;
                Schema = schema;
            }

            /// <inheritdoc/>
            public virtual bool TryMoveNext()
            {
                if (_outer.ExpectedFieldName != null)
                {
                    // There are no fields in here
                    return false;
                }
                return true;
            }

            protected readonly AvroSchemaTraverser _outer;
        }

        /// <summary>
        /// Array traversal
        /// </summary>
        private class ArrayTraverser : Traverser
        {
            /// <summary>
            /// Create record traverers
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="schema"></param>
            public ArrayTraverser(AvroSchemaTraverser outer,
                ArraySchema schema) : base(outer, schema)
            {
                _array = schema;
            }

            /// <inheritdoc/>
            public override bool TryMoveNext()
            {
                if (_outer.ExpectedFieldName != null)
                {
                    // There are no fields in here
                    return false;
                }
                _outer.Push(_array.ItemSchema);
                return true;
            }

            private readonly ArraySchema _array;
        }

        /// <summary>
        /// Array traversal
        /// </summary>
        private class UnionTraverser : Traverser
        {
            /// <summary>
            /// Create union traverser
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="schema"></param>
            public UnionTraverser(AvroSchemaTraverser outer,
                UnionSchema schema) : base(outer, schema)
            {
                _union = schema;
            }

            /// <inheritdoc/>
            public override bool TryMoveNext()
            {
                if (_outer.ExpectUnionItem == null)
                {
                    return false;
                }

                var selected = _outer.ExpectUnionItem(_union);
                if (selected == null)
                {
                    return false;
                }
                _outer.Push(selected);
                return true;
            }

            private readonly UnionSchema _union;
        }

        /// <summary>
        /// Record traverser
        /// </summary>
        private sealed class RecordTraverser : Traverser
        {
            /// <summary>
            /// Create record traverers
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="schema"></param>
            public RecordTraverser(AvroSchemaTraverser outer,
                RecordSchema schema) : base(outer, schema)
            {
                _record = schema;
            }

            /// <inheritdoc/>
            public override bool TryMoveNext()
            {
                Debug.Assert(_pos >= 0);

                if (_pos == _record.Count)
                {
                    _pos = 0;
                }
                var field = _record.Fields[_pos];
                var fieldName = _outer.ExpectedFieldName;
                _outer.ExpectedFieldName = null;
                if (fieldName != null && field.Name != fieldName)
                {
                    var schemaField = SchemaUtils.Escape(fieldName);
                    if (!_record.TryGetField(schemaField, out field) &&
                        !_record.TryGetFieldAlias(schemaField, out field))
                    {
                        return false;
                    }
                    _pos = field.Pos - 1;
                }
                _outer.Push(field.Schema);
                _pos++;
                return true;
            }

            private readonly RecordSchema _record;
            private int _pos;
        }

        private readonly Dictionary<string, RecordSchema> _types = new();
        private readonly Stack<Traverser> _schemas = new();
    }
}
