// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Json.Schema
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;

    /// <summary>
    /// Json Schema writer
    /// </summary>
    public sealed class SchemaWriter : IDisposable
    {
        /// <summary>
        /// Create writer
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="options"></param>
        public SchemaWriter(Stream stream, JsonWriterOptions options)
        {
            _writer = new Utf8JsonWriter(stream, options);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _writer.Dispose();
        }

        /// <summary>
        /// Convert to string
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="indented"></param>
        /// <returns></returns>
        public static string SerializeAsString(JsonSchema schema, bool indented = false)
        {
            if (schema.SchemaVersion == null)
            {
                schema.SchemaVersion = SchemaVersion.Draft7;
            }
            using (var stream = new MemoryStream())
            {
                using (var writer = new SchemaWriter(stream, new JsonWriterOptions { Indented = indented }))
                {
                    writer.Write(schema);
                }
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        /// <summary>
        /// Write schema
        /// </summary>
        /// <param name="schema"></param>
        public void Write(JsonSchema schema)
        {
            Write(schema, Current != SchemaVersion.Draft4);
        }

        /// <summary>
        /// Write schema
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="allowTrueSchema"></param>
        private void Write(JsonSchema schema, bool allowTrueSchema)
        {
            if (schema.Allowed != null && allowTrueSchema) // TODO: should we throw here?
            {
                // From draft6 on
                //
                // booleans as schemas	allowable anywhere, not just
                // "additionalProperties" and "additionalItems"	true is
                // equivalent to {}, false is equivalent to {"not": {}},
                // but the intent is more clear and implementations can
                // optimize these cases more easily
                //
                _writer.WriteBooleanValue(schema.Allowed.Value);
                return;
            }

            _writer.WriteStartObject();
            var pop = false;
            if (schema.SchemaVersion != null && Current != schema.SchemaVersion)
            {
                _schemaVersion.Push(schema.SchemaVersion!);
                _writer.WriteString(SchemaVocabulary.Schema, schema.SchemaVersion);
                pop = true;
            }
            try
            {
                try
                {
                    if (schema.Reference != null)
                    {
                        // Write the reference
                        Write(SchemaVocabulary.Ref, schema.Reference);
                        return;
                    }

                    if (schema.Id != null)
                    {
                        Write(Current != SchemaVersion.Draft4
                            ? SchemaVocabulary.Id : SchemaVocabulary.IdPreDraft6,
                            schema.Id);
                    }

                    Write(SchemaVocabulary.Title, schema.Title);
                    Write(SchemaVocabulary.Description, schema.Description);
                    if (Current != SchemaVersion.Draft4)
                    {
                        Write(SchemaVocabulary.Examples, schema.Examples);
                        if (Current != SchemaVersion.Draft6)
                        {
                            Write(SchemaVocabulary.Comment, schema.Comment);
                        }
                    }

                    Write(SchemaVocabulary.Type, schema.Types);

                    Write(SchemaVocabulary.Enum, schema.Enum);

                    Write(SchemaVocabulary.Items, schema.Items, true);
                    Write(SchemaVocabulary.MinItems, schema.MinItems);
                    Write(SchemaVocabulary.MaxItems, schema.MaxItems);
                    Write(SchemaVocabulary.UniqueItems, schema.UniqueItems);
                    Write(SchemaVocabulary.AdditionalItems, schema.AdditionalItems, true);
                    if (Current != SchemaVersion.Draft4)
                    {
                        Write(SchemaVocabulary.Contains, schema.Contains);
                    }
                    Write(SchemaVocabulary.AllOf, schema.AllOf);
                    Write(SchemaVocabulary.OneOf, schema.OneOf);
                    Write(SchemaVocabulary.AnyOf, schema.AnyOf);
                    Write(SchemaVocabulary.Not, schema.Not, Current != SchemaVersion.Draft4);

                    Write(SchemaVocabulary.Properties, schema.Properties);
                    Write(SchemaVocabulary.MinProperties, schema.MinProperties);
                    Write(SchemaVocabulary.MaxProperties, schema.MaxProperties);
                    Write(SchemaVocabulary.Required, schema.Required);
                    Write(SchemaVocabulary.AdditionalProperties, schema.AdditionalProperties, true);
                    Write(SchemaVocabulary.Dependencies, schema.Dependencies);
                    if (Current != SchemaVersion.Draft4)
                    {
                        Write(SchemaVocabulary.PropertyNames, schema.PropertyNames, true);
                    }

                    Write(SchemaVocabulary.Minimum, schema.Minimum);
                    Write(SchemaVocabulary.Maximum, schema.Maximum);
                    Write(SchemaVocabulary.Default, schema.Default);
                    Write(SchemaVocabulary.Format, schema.Format);
                    Write(SchemaVocabulary.MultipleOf, schema.MultipleOf);
                    if (Current != SchemaVersion.Draft4)
                    {
                        Write(SchemaVocabulary.Const, schema.Const);
                        if (Current != SchemaVersion.Draft6)
                        {
                            if (schema.ReadOnly.HasValue)
                            {
                                Write(SchemaVocabulary.ReadOnly, schema.ReadOnly);
                            }
                            else
                            {
                                Write(SchemaVocabulary.WriteOnly, schema.WriteOnly);
                            }
                        }
                    }

                    Write(SchemaVocabulary.MinLength, schema.MinLength);
                    Write(SchemaVocabulary.MaxLength, schema.MaxLength);
                }
                finally
                {
                    Write(Current != SchemaVersion.Draft4
                        ? SchemaVocabulary.Defs : SchemaVocabulary.Definitions,
                        schema.Definitions);
                }
            }
            finally
            {
                _writer.WriteEndObject();
                if (pop)
                {
                    _schemaVersion.Pop();
                }
            }
        }

        /// <summary>
        /// Write schema
        /// </summary>
        /// <param name="name"></param>
        /// <param name="schema"></param>
        private void Write(ReadOnlySpan<char> name, JsonSchema schema)
        {
            if (name.Length != 0)
            {
                _writer.WritePropertyName(name);
            }
            Write(schema);
        }

        /// <summary>
        /// Write schema
        /// </summary>
        /// <param name="name"></param>
        /// <param name="schema"></param>
        /// <param name="allowTrueSchema"></param>
        private void Write(ReadOnlySpan<byte> name, JsonSchema? schema,
            bool allowTrueSchema)
        {
            if (schema == null)
            {
                return;
            }
            if (name.Length != 0)
            {
                _writer.WritePropertyName(name);
            }
            Write(schema, allowTrueSchema);
        }

        /// <summary>
        /// Write schemas
        /// </summary>
        /// <param name="name"></param>
        /// <param name="schemas"></param>
        private void Write(ReadOnlySpan<byte> name,
            IReadOnlyDictionary<string, JsonSchema>? schemas)
        {
            if (schemas == null || schemas.Count == 0)
            {
                return;
            }
            if (name.Length != 0)
            {
                _writer.WritePropertyName(name);
            }
            _writer.WriteStartObject();
            try
            {
                foreach (var kv in schemas)
                {
                    Write(kv.Key, kv.Value);
                }
            }
            finally
            {
                _writer.WriteEndObject();
            }
        }

        /// <summary>
        /// Write schemas
        /// </summary>
        /// <param name="name"></param>
        /// <param name="schemas"></param>
        /// <param name="allowSingleItem"></param>
        private void Write(ReadOnlySpan<byte> name,
            IReadOnlyList<JsonSchema>? schemas, bool allowSingleItem = false)
        {
            if (schemas == null)
            {
                return;
            }
            if (name.Length != 0)
            {
                _writer.WritePropertyName(name);
            }
            if (schemas.Count == 1 && allowSingleItem)
            {
                Write(schemas[0], Current != SchemaVersion.Draft4);
                return;
            }

            _writer.WriteStartArray();
            try
            {
                foreach (var schema in schemas)
                {
                    Write(schema);
                }
            }
            finally
            {
                _writer.WriteEndArray();
            }
        }

        /// <summary>
        /// Write limit
        /// </summary>
        /// <param name="name"></param>
        /// <param name="limit"></param>
        private void Write(ReadOnlySpan<byte> name, Limit? limit)
        {
            if (limit == null)
            {
                return;
            }
            if (limit.Exclusive)
            {
                if (name.SequenceCompareTo(SchemaVocabulary.Minimum) == 0)
                {
                    if (Current != SchemaVersion.Draft4)
                    {
                        name = SchemaVocabulary.ExclusiveMinimum;
                    }
                    else
                    {
                        Write(SchemaVocabulary.ExclusiveMinimum, limit.Exclusive);
                    }
                }
                else if (name.SequenceCompareTo(SchemaVocabulary.Maximum) == 0)
                {
                    if (Current != SchemaVersion.Draft4)
                    {
                        name = SchemaVocabulary.ExclusiveMaximum;
                    }
                    else
                    {
                        Write(SchemaVocabulary.ExclusiveMaximum, limit.Exclusive);
                    }
                }
            }
            Write(name, limit.Value);
        }

        /// <summary>
        /// Write const
        /// </summary>
        /// <param name="name"></param>
        /// <param name="constValue"></param>
        private void Write(ReadOnlySpan<byte> name, Const? constValue)
        {
            if (constValue == null)
            {
                return;
            }
            if (name.Length != 0)
            {
                _writer.WritePropertyName(name);
            }
            constValue.Write(_writer);
        }

        /// <summary>
        /// Write enum
        /// </summary>
        /// <param name="name"></param>
        /// <param name="constValues"></param>
        private void Write(ReadOnlySpan<byte> name, IReadOnlyList<Const>? constValues)
        {
            if (constValues == null)
            {
                return;
            }
            if (name.Length != 0)
            {
                _writer.WritePropertyName(name);
            }
            _writer.WriteStartArray();
            foreach (var constValue in constValues)
            {
                constValue.Write(_writer);
            }
            _writer.WriteEndArray();
        }

        /// <summary>
        /// Write uri
        /// </summary>
        /// <param name="name"></param>
        /// <param name="uriOrFragment"></param>
        private void Write(ReadOnlySpan<byte> name, UriOrFragment? uriOrFragment)
        {
            if (uriOrFragment == null)
            {
                return;
            }
            _writer.WriteString(name, uriOrFragment.ToString());
        }

        /// <summary>
        /// Write dependencies
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dependencies"></param>
        private void Write(ReadOnlySpan<byte> name,
            IReadOnlyDictionary<string, Dependency>? dependencies)
        {
            if (dependencies == null || dependencies.Count == 0) // Draft6 allows empty array
            {
                return;
            }
            _writer.WritePropertyName(name);
            _writer.WriteStartObject();
            foreach (var kv in dependencies)
            {
                Write(kv.Key, kv.Value);
            }
            _writer.WriteEndObject();
        }

        /// <summary>
        /// Write dependency
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dependency"></param>
        private void Write(ReadOnlySpan<char> name, Dependency dependency)
        {
            _writer.WritePropertyName(name);

            if (dependency.SchemaDependency != null)
            {
                Write(dependency.SchemaDependency);
            }
            else if (dependency.PropertyDependencies != null)
            {
                _writer.WriteStartArray();
                foreach (var arrayElement in dependency.PropertyDependencies)
                {
                    _writer.WriteStringValue(arrayElement);
                }
                _writer.WriteEndArray();
            }
        }

        /// <summary>
        /// Write schema types
        /// </summary>
        /// <param name="name"></param>
        /// <param name="schemaType"></param>
        private void Write(ReadOnlySpan<byte> name, IReadOnlyList<SchemaType>? schemaType)
        {
            if (schemaType == null)
            {
                return;
            }
#pragma warning disable CA1308 // Normalize strings to uppercase
            var types = schemaType.Select(st => st.ToString().ToLowerInvariant()).ToArray();
#pragma warning restore CA1308 // Normalize strings to uppercase
            if (types.Length == 1)
            {
                _writer.WriteString(name, types[0]);
                return;
            }

            if (name.Length > 0)
            {
                _writer.WritePropertyName(name);
            }
            _writer.WriteStartArray();
            foreach (var type in types)
            {
                _writer.WriteStringValue(type);
            }
            _writer.WriteEndArray();
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private void Write(ReadOnlySpan<byte> name, string? value)
        {
            if (value == null)
            {
                return;
            }
            _writer.WriteString(name, value);
        }

        /// <summary>
        /// Write values
        /// </summary>
        /// <param name="name"></param>
        /// <param name="values"></param>
        private void Write(ReadOnlySpan<byte> name, IReadOnlyList<string>? values)
        {
            if (values == null || values.Count == 0) // Draft8 allows empty array
            {
                return;
            }
            if (name.Length > 0)
            {
                _writer.WritePropertyName(name);
            }
            _writer.WriteStartArray();
            foreach (var type in values)
            {
                _writer.WriteStringValue(type);
            }
            _writer.WriteEndArray();
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private void Write(ReadOnlySpan<byte> name, int? value)
        {
            if (value == null)
            {
                return;
            }
            _writer.WriteNumber(name, value.Value);
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private void Write(ReadOnlySpan<byte> name, bool? value)
        {
            if (value == null)
            {
                return;
            }
            _writer.WriteBoolean(name, value.Value);
        }

        private string Current => _schemaVersion.Count == 0
            ? string.Empty : _schemaVersion.Peek();
        private readonly Stack<string> _schemaVersion = new();
        private readonly Utf8JsonWriter _writer;
    }
}
