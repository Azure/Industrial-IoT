// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    /// <summary>
    /// Json Schema object - constraints for a schema with the defined
    /// types. see https://www.learnjsonschema.com/2020-12/
    /// </summary>
    public record class JsonSchema
    {
        /// <summary>
        /// Allows everything, serializes to true
        /// </summary>
        public bool? Allowed { get; set; }

        /// <summary>
        /// The absolute id of the schema
        /// </summary>
        public UriOrFragment? Id { get; set; }

        /// <summary>
        /// Schema version
        /// </summary>
        public string? SchemaVersion { get; set; }

        /// <summary>
        /// Title annotation
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Description annotation
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Comment annotation
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Examples
        /// </summary>
        public IReadOnlyList<string>? Examples { get; set; }

        /// <summary>
        /// Schema types
        /// </summary>
        public IReadOnlyList<SchemaType> Types { get; set; }
            = Array.Empty<SchemaType>();

        /// <summary>
        /// Enum values
        /// </summary>
        public IReadOnlyList<Const>? Enum { get; set; }

        /// <summary>
        /// Gets or sets the maximum valid number of properties.
        /// </summary>
        public int? MaxProperties { get; set; }

        /// <summary>
        /// Gets or sets the maximum valid number of properties.
        /// </summary>
        public int? MinProperties { get; set; }

        /// <summary>
        /// ReadOnly  annotation
        /// </summary>
        public bool? ReadOnly { get; set; }

        /// <summary>
        /// WriteOnly annotation
        /// </summary>
        public bool? WriteOnly { get; set; }

        /// <summary>
        /// The schemas for the fields of the object
        /// </summary>
        public IReadOnlyDictionary<string, JsonSchema>? Properties { get; set; }

        /// <summary>
        /// The schemas for the fields of the object
        /// </summary>
        public JsonSchema? PropertyNames { get; set; }

        /// <summary>
        /// Required properties
        /// </summary>
        public IReadOnlyList<string>? Required { get; set; }

        /// <summary>
        /// Required properties
        /// </summary>
        public IReadOnlyList<string>? Contains { get; set; }

        /// <summary>
        /// Gets or sets a dictionary that maps property names to the conditions
        /// that an instance containing those property names must satisfy.
        /// </summary>
        public IReadOnlyDictionary<string, Dependency>? Dependencies { get; set; }

        /// <summary>
        /// Additional properties (allowed or schema)
        /// </summary>
        public JsonSchema? AdditionalProperties { get; set; }

        /// <summary>
        /// Gets or sets a set of schemas against all of which
        /// the instance must validate successfully.
        /// </summary>
        public IReadOnlyList<JsonSchema>? AllOf { get; set; }

        /// <summary>
        /// Gets or sets a set of schemas against any of which
        /// the instance must validate successfully.
        /// </summary>
        public IReadOnlyList<JsonSchema>? AnyOf { get; set; }

        /// <summary>
        /// Gets or sets a set of schemas against exactly one of
        /// which the instance must validate successfully.
        /// </summary>
        public IReadOnlyList<JsonSchema>? OneOf { get; set; }

        /// <summary>
        /// Gets or sets a schemas against which the instance
        /// must not validate successfully.
        /// </summary>
        public JsonSchema? Not { get; set; }

        /// <summary>
        /// Array items allowed
        /// </summary>
        public IReadOnlyList<JsonSchema>? Items { get; set; }

        /// <summary>
        /// Additional items
        /// </summary>
        public JsonSchema? AdditionalItems { get; set; }

        /// <summary>
        /// Gets or sets the maximum length of a string
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets the minimum length of a string
        /// </summary>
        public int? MinLength { get; set; }

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        public Const? Default { get; set; }

        /// <summary>
        /// Gets or sets a const value.
        /// </summary>
        public Const? Const { get; set; }

        /// <summary>
        /// Gets or sets a regular expression which string must match
        /// </summary>
        public string? Pattern { get; set; }

        /// <summary>
        /// Gets or sets a value of which a numeric schema
        /// instance must be a multiple.
        /// </summary>
        public Const? MultipleOf { get; set; }

        /// <summary>
        /// Gets or sets the maximum valid value of integer
        /// or number schema.
        /// </summary>
        public Limit? Maximum { get; set; }

        /// <summary>
        /// Gets or sets the minimum valid value.
        /// </summary>
        public Limit? Minimum { get; set; }

        /// <summary>
        /// Gets or sets the minimum valid number of elements
        /// in an array.
        /// </summary>
        public int? MinItems { get; set; }

        /// <summary>
        /// Gets or sets the maximum valid elements in an array.
        /// </summary>
        public int? MaxItems { get; set; }

        /// <summary>
        /// Gets or sets a value that specifies elements must be unique.
        /// </summary>
        public bool? UniqueItems { get; set; }

        /// <summary>
        /// Gets or sets the URI of a schema that is incorporated
        /// by reference into the current schema.
        /// </summary>
        public UriOrFragment? Reference { get; set; }

        /// <summary>
        /// Gets or sets a string specifying the required format
        /// of a string-valued property.
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Gets or sets a dictionary mapping schema names
        /// to sub-schemas which can be referenced by properties
        /// defined elsewhere in the current schema.
        /// </summary>
        public IReadOnlyDictionary<string, JsonSchema>? Definitions { get; set; }

        /// <summary>
        /// Get Schema type
        /// </summary>
        /// <returns></returns>
        public SchemaType Type
        {
            get => Types?.Count > 0 ?
                Types[0] : SchemaType.None;
            set => Types = value == SchemaType.None ?
                Array.Empty<SchemaType>() : new[] { value };
        }
    }

    /// <summary>
    /// Represents the valid values for the "type" keyword in a JSON schema.
    /// </summary>
    public enum SchemaType
    {
        /// <summary>
        /// Invalid
        /// </summary>
        None,

#pragma warning disable CA1720 // Identifier contains type name
        /// <summary>
        /// Array
        /// </summary>
        Array,

        /// <summary>
        /// Boolean
        /// </summary>
        Boolean,

        /// <summary>
        /// Integer
        /// </summary>
        Integer,

        /// <summary>
        /// Number
        /// </summary>
        Number,

        /// <summary>
        /// Null
        /// </summary>
        Null,

        /// <summary>
        /// Object
        /// </summary>
        Object,

        /// <summary>
        /// String
        /// </summary>
        String,
#pragma warning restore CA1720 // Identifier contains type name
    }

    /// <summary>
    /// Const base class
    /// </summary>
    public abstract record Const
    {
        /// <summary>
        /// Write value to writer
        /// </summary>
        /// <param name="writer"></param>
        internal abstract void Write(Utf8JsonWriter writer);

        /// <summary>
        /// Create const
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Const From<T>(T value)
        {
            return new Const<T>(value);
        }
    }

    /// <summary>
    /// Limit value
    /// </summary>
    /// <param name="Value"></param>
    /// <param name="Exclusive"></param>
    public record class Limit(Const Value, bool Exclusive = false)
    {
        /// <summary>
        /// Create limit
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="exclusive"></param>
        /// <returns></returns>
        public static Limit From<T>(T value, bool exclusive = false)
        {
            return new Limit(Const.From(value), exclusive);
        }
    }

    /// <summary>
    /// Const value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="Value"></param>
    public record class Const<T>(T Value) : Const
    {
        /// <inheritdoc/>
        internal override void Write(Utf8JsonWriter writer)
        {
            JsonSerializer.Serialize(writer, Value);
        }
    }

    /// <summary>
    /// Describes that conditions that must be satisfied when an object contains
    /// a property with the specified name.
    /// </summary>
    public record class Dependency
    {
        /// <summary>
        /// Gets the schema against which an instance must validate successfully if it
        /// has a property of the name associated with this dependency.
        /// </summary>
        public JsonSchema? SchemaDependency { get; }

        /// <summary>
        /// Gets the set of property names which an instance must also have if it has a
        /// property of the name associated with this dependency.
        /// </summary>
        public IReadOnlyList<string>? PropertyDependencies { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dependency"/> class from the
        /// specified schema dependency.
        /// </summary>
        /// <param name="schemaDependency">
        /// The schema against which an instance must validate successfully if it
        /// has a property of the name associated with this dependency.
        /// </param>
        public Dependency(JsonSchema schemaDependency)
        {
            SchemaDependency = schemaDependency;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dependency"/> class from the
        /// specified property dependencies.
        /// </summary>
        /// <param name="propertyDependencies">
        /// The set of property names which an instance must also have if it has a
        /// property of the name associated with this dependency.
        /// </param>
        public Dependency(IList<string> propertyDependencies)
        {
            PropertyDependencies = propertyDependencies.ToList();
        }
    }

    /// <summary>
    /// Represents a value that is either a URI reference according to
    /// RFC 2396, or a bare fragment.
    /// </summary>
    public record class UriOrFragment
    {
        /// <summary>
        /// Value
        /// </summary>
        public string Fragment { get; }

        /// <summary>
        /// Value
        /// </summary>
        public string? Namespace { get; }

        /// <summary>
        /// Self reference
        /// </summary>
        public static readonly UriOrFragment Self = new("#");

        /// <summary>
        /// Create a fragment
        /// </summary>
        /// <param name="fragment"></param>
        /// <param name="namespace"></param>
        public UriOrFragment(string fragment, string? @namespace = null)
        {
            Fragment = fragment;
            Namespace = @namespace;
        }

        /// <summary>
        /// Value
        /// </summary>
        public override string ToString()
        {
            if (Namespace != null)
            {
                return Namespace + "#" + Fragment.UrlEncode();
            }
            return Fragment.UrlEncode();
        }
    }
}
