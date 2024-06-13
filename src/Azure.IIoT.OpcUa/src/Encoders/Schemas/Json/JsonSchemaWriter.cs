// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Json
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
    public sealed class JsonSchemaWriter : IDisposable
    {
        /// <summary>
        /// Create writer
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="options"></param>
        public JsonSchemaWriter(Stream stream, JsonWriterOptions options)
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
            schema.SchemaVersion ??= JsonSchemaVersion.Draft7;

            using (var stream = new MemoryStream())
            {
                using (var writer = new JsonSchemaWriter(stream, new JsonWriterOptions { Indented = indented }))
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
            Write(schema, Current != JsonSchemaVersion.Draft4);
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
                _writer.WriteString(JsonSchemaVocabulary.Schema, schema.SchemaVersion);
                pop = true;
            }
            try
            {
                try
                {
                    if (schema.Reference != null)
                    {
                        // Write the reference
                        Write(JsonSchemaVocabulary.Ref, schema.Reference);
                        return;
                    }

                    if (schema.Id != null)
                    {
                        Write(Current != JsonSchemaVersion.Draft4
                            ? JsonSchemaVocabulary.Id : JsonSchemaVocabulary.IdPreDraft6,
                            schema.Id);
                    }

                    Write(JsonSchemaVocabulary.Title, schema.Title);
                    Write(JsonSchemaVocabulary.Description, schema.Description);
                    if (Current != JsonSchemaVersion.Draft4)
                    {
                        Write(JsonSchemaVocabulary.Examples, schema.Examples);
                        if (Current != JsonSchemaVersion.Draft6)
                        {
                            Write(JsonSchemaVocabulary.Comment, schema.Comment);
                        }
                    }

                    Write(JsonSchemaVocabulary.Type, schema.Types);

                    Write(JsonSchemaVocabulary.Enum, schema.Enum);

                    Write(JsonSchemaVocabulary.Items, schema.Items, true);
                    Write(JsonSchemaVocabulary.MinItems, schema.MinItems);
                    Write(JsonSchemaVocabulary.MaxItems, schema.MaxItems);
                    Write(JsonSchemaVocabulary.UniqueItems, schema.UniqueItems);
                    Write(JsonSchemaVocabulary.AdditionalItems, schema.AdditionalItems, true);
                    if (Current != JsonSchemaVersion.Draft4)
                    {
                        Write(JsonSchemaVocabulary.Contains, schema.Contains);
                    }
                    Write(JsonSchemaVocabulary.AllOf, schema.AllOf);
                    Write(JsonSchemaVocabulary.OneOf, schema.OneOf);
                    Write(JsonSchemaVocabulary.AnyOf, schema.AnyOf);
                    Write(JsonSchemaVocabulary.Not, schema.Not, Current != JsonSchemaVersion.Draft4);

                    Write(JsonSchemaVocabulary.Properties, schema.Properties);
                    Write(JsonSchemaVocabulary.MinProperties, schema.MinProperties);
                    Write(JsonSchemaVocabulary.MaxProperties, schema.MaxProperties);
                    Write(JsonSchemaVocabulary.Required, schema.Required);
                    Write(JsonSchemaVocabulary.AdditionalProperties, schema.AdditionalProperties, true);
                    Write(JsonSchemaVocabulary.Dependencies, schema.Dependencies);
                    if (Current != JsonSchemaVersion.Draft4)
                    {
                        Write(JsonSchemaVocabulary.PropertyNames, schema.PropertyNames, true);
                    }

                    Write(JsonSchemaVocabulary.Minimum, schema.Minimum);
                    Write(JsonSchemaVocabulary.Maximum, schema.Maximum);
                    Write(JsonSchemaVocabulary.Default, schema.Default);
                    Write(JsonSchemaVocabulary.Format, schema.Format);
                    Write(JsonSchemaVocabulary.MultipleOf, schema.MultipleOf);
                    if (Current != JsonSchemaVersion.Draft4)
                    {
                        Write(JsonSchemaVocabulary.Const, schema.Const);
                        if (Current != JsonSchemaVersion.Draft6)
                        {
                            if (schema.ReadOnly.HasValue)
                            {
                                Write(JsonSchemaVocabulary.ReadOnly, schema.ReadOnly);
                            }
                            else
                            {
                                Write(JsonSchemaVocabulary.WriteOnly, schema.WriteOnly);
                            }
                        }
                    }

                    Write(JsonSchemaVocabulary.MinLength, schema.MinLength);
                    Write(JsonSchemaVocabulary.MaxLength, schema.MaxLength);
                }
                finally
                {
                    Write(Current != JsonSchemaVersion.Draft202012 ?
                        JsonSchemaVocabulary.Definitions :
                        JsonSchemaVocabulary.Defs, schema.Definitions);
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
                Write(schemas[0], Current != JsonSchemaVersion.Draft4);
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
                if (name.SequenceCompareTo(JsonSchemaVocabulary.Minimum) == 0)
                {
                    if (Current != JsonSchemaVersion.Draft4)
                    {
                        name = JsonSchemaVocabulary.ExclusiveMinimum;
                    }
                    else
                    {
                        Write(JsonSchemaVocabulary.ExclusiveMinimum, limit.Exclusive);
                    }
                }
                else if (name.SequenceCompareTo(JsonSchemaVocabulary.Maximum) == 0)
                {
                    if (Current != JsonSchemaVersion.Draft4)
                    {
                        name = JsonSchemaVocabulary.ExclusiveMaximum;
                    }
                    else
                    {
                        Write(JsonSchemaVocabulary.ExclusiveMaximum, limit.Exclusive);
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
        /// Write uri or fragment
        /// </summary>
        /// <param name="name"></param>
        /// <param name="uriOrFragment"></param>
        private void Write(ReadOnlySpan<byte> name, UriOrFragment? uriOrFragment)
        {
            if (uriOrFragment == null)
            {
                return;
            }
            if (uriOrFragment.Fragment == "#")
            {
                _writer.WriteString(name, "#");
            }
            else if (uriOrFragment.Namespace != null)
            {
                _writer.WriteString(name, uriOrFragment.ToString());
            }
            else if (Current != JsonSchemaVersion.Draft202012)
            {
                _writer.WriteString(name, "#/definitions/" + uriOrFragment.Fragment);
            }
            else
            {
                _writer.WriteString(name, "#/$defs/" + uriOrFragment.Fragment);
            }
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
