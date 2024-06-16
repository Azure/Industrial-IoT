// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Json
{
    using System;

    /// <summary>
    /// Schema vocabulary
    /// </summary>
    internal static class JsonSchemaVocabulary
    {
        public static ReadOnlySpan<byte> Schema => "$schema"u8;
        public static ReadOnlySpan<byte> Id => "$id"u8;
        public static ReadOnlySpan<byte> IdPreDraft6 => "id"u8;
        public static ReadOnlySpan<byte> Defs => "$defs"u8;
        public static ReadOnlySpan<byte> Definitions => "definitions"u8;
        public static ReadOnlySpan<byte> Ref => "$ref"u8;
        public static ReadOnlySpan<byte> Title => "title"u8;
        public static ReadOnlySpan<byte> Type => "type"u8;
        public static ReadOnlySpan<byte> Enum => "enum"u8;
        public static ReadOnlySpan<byte> Description => "description"u8;
        public static ReadOnlySpan<byte> Examples => "examples"u8;
        public static ReadOnlySpan<byte> Comment => "$comment"u8;
        public static ReadOnlySpan<byte> Items => "items"u8;
        public static ReadOnlySpan<byte> Format => "format"u8;
        public static ReadOnlySpan<byte> Const => "const"u8;
        public static ReadOnlySpan<byte> ReadOnly => "readOnly"u8;
        public static ReadOnlySpan<byte> WriteOnly => "writeOnly"u8;
        public static ReadOnlySpan<byte> Default => "default"u8;
        public static ReadOnlySpan<byte> Minimum => "minimum"u8;
        public static ReadOnlySpan<byte> Maximum => "maximum"u8;
        public static ReadOnlySpan<byte> MultipleOf => "multipleOf"u8;
        public static ReadOnlySpan<byte> ExclusiveMinimum => "exclusiveMinimum"u8;
        public static ReadOnlySpan<byte> ExclusiveMaximum => "exclusiveMaximum"u8;
        public static ReadOnlySpan<byte> Required => "required"u8;
        public static ReadOnlySpan<byte> Properties => "properties"u8;
        public static ReadOnlySpan<byte> MinProperties => "minProperties"u8;
        public static ReadOnlySpan<byte> MaxProperties => "maxProperties"u8;
        public static ReadOnlySpan<byte> Dependencies => "dependencies"u8;
        public static ReadOnlySpan<byte> PropertyNames => "propertyNames"u8;
        public static ReadOnlySpan<byte> AdditionalProperties => "additionalProperties"u8;
        public static ReadOnlySpan<byte> MinLength => "minLength"u8;
        public static ReadOnlySpan<byte> MaxLength => "maxLength"u8;
        public static ReadOnlySpan<byte> MinItems => "minItems"u8;
        public static ReadOnlySpan<byte> MaxItems => "maxItems"u8;
        public static ReadOnlySpan<byte> UniqueItems => "uniqueItems"u8;
        public static ReadOnlySpan<byte> AdditionalItems => "additionalItems"u8;
        public static ReadOnlySpan<byte> Contains => "contains"u8;
        public static ReadOnlySpan<byte> AllOf => "allOf"u8;
        public static ReadOnlySpan<byte> OneOf => "oneOf"u8;
        public static ReadOnlySpan<byte> AnyOf => "anyOf"u8;
        public static ReadOnlySpan<byte> Not => "not"u8;
    }
}
