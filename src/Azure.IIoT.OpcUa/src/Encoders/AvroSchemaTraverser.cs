// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using global::Avro;
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
        /// Next should be the item in array
        /// </summary>
        public bool ExpectArrayItem { get; set; }

        /// <summary>
        /// Create traversal
        /// </summary>
        /// <param name="schema"></param>
        public AvroSchemaTraverser(Schema schema)
        {
            Push(schema);
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
            _schemas.Push(schema switch
            {
                RecordSchema r => new RecordTraverser(this, r),
                ArraySchema a => new ArrayTraverser(this, a),
                UnionSchema u => new UnionTraverser(this, u),
                _ => new Traverser(this, schema)
            });
        }

        /// <summary>
        /// Finalize
        /// </summary>
        public bool IsDone()
        {
            return _schemas.Count != 0;
        }

        /// <summary>
        /// Allows to pop travers from stack
        /// </summary>
        /// <param name="traverser"></param>
        private bool TryPop(Traverser traverser)
        {
            var top = _schemas.Pop();
            Debug.Assert(traverser == top);
            return TryMoveNext();
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
                return _outer.TryPop(this);
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
                if (_outer.ExpectArrayItem)
                {
                    _outer.ExpectArrayItem = false;
                    _outer.Push(_array.ItemSchema);
                    return true;
                }
                return _outer.TryPop(this);
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
                    _pos = -1;
                    return _outer.TryPop(this);
                }
                var field = _record.Fields[_pos];
                var fieldName = _outer.ExpectedFieldName;
                _outer.ExpectedFieldName = null;
                if (fieldName != null && field.Name != fieldName)
                {
                    if (!_record.TryGetField(fieldName, out field) &&
                        !_record.TryGetFieldAlias(fieldName, out field))
                    {
                        return false;
                    }
                    _pos = field.Pos;
                }
                _outer.Push(field.Schema);
                _pos++;
                return true;
            }

            private readonly RecordSchema _record;
            private int _pos;
        }

        private readonly Stack<Traverser> _schemas = new();
    }
}
