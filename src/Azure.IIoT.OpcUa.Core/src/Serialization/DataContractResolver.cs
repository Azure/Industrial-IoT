// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization.Metadata;

    /// <summary>
    /// A <see cref="JsonTypeInfo"/> contract modifier that reproduces the wire
    /// format of the former reflection based whole-object DataContract converter
    /// but expressed as metadata customization on the (potentially source
    /// generated) type info. Applying this modifier to a resolver lets the
    /// <see cref="System.Runtime.Serialization.DataContractAttribute"/> /
    /// <see cref="DataMemberAttribute"/> annotated API models serialize with the
    /// exact same property names, ordering and default-omission semantics without
    /// the whole-object reflection converter, which is the prerequisite for
    /// Native-AOT / trim safe (source generated) serialization.
    /// </summary>
    internal static class DataContractResolver
    {
        /// <summary>
        /// The modifier delegate to register on a resolver.
        /// </summary>
        /// <param name="typeInfo"></param>
        [UnconditionalSuppressMessage("Trimming", "IL2075",
            Justification = "DataMember/DataContract attributes are read from " +
                "members already rooted by the (source generated) type info; no " +
                "dynamic code is generated, so this is Native-AOT safe.")]
        public static void Modify(JsonTypeInfo typeInfo)
        {
            ArgumentNullException.ThrowIfNull(typeInfo);
            if (typeInfo.Kind != JsonTypeInfoKind.Object)
            {
                return;
            }
            if (!IsDataContractObject(typeInfo.Type))
            {
                return;
            }

            // Drop properties that the DataContractObjectConverter would not have
            // written / read: only writable, non special-name properties carrying
            // a DataMemberAttribute participate.
            for (var i = typeInfo.Properties.Count - 1; i >= 0; i--)
            {
                var property = typeInfo.Properties[i];
                var dma = GetDataMember(property);
                if (dma == null)
                {
                    typeInfo.Properties.RemoveAt(i);
                    continue;
                }

                // Preserve the exact DataMember name verbatim (the reflection
                // converter writes dma.Name ?? memberName without applying the
                // camelCase naming policy).
                if (property.AttributeProvider is MemberInfo member)
                {
                    property.Name = dma.Name ?? member.Name;
                }

                // The whole-object reflection converter never enforced required
                // members on read (unknown / missing members were ignored for
                // Newtonsoft compatibility); keep that lenient behavior so the
                // wire format round-trips exactly as before.
                property.IsRequired = false;

                // EmitDefaultValue == false omits the property when its value
                // equals the type default (default(T) for value types, null for
                // reference types), matching DataContractObjectConverter.
                if (!dma.EmitDefaultValue)
                {
                    var defaultValue = GetDefault(property.PropertyType);
                    property.ShouldSerialize = (_, value) =>
                        !IsDefault(value, defaultValue);
                }
            }
        }

        /// <summary>
        /// Mirror of <c>DataContractObjectConverter.CanConvert</c>: the type must
        /// carry a data contract, expose a parameterless constructor and declare
        /// at least one writable data member property.
        /// </summary>
        /// <param name="type"></param>
        [UnconditionalSuppressMessage("Trimming", "IL2070",
            Justification = "Constructor/property metadata is inspected on a type " +
                "rooted by the type info; no dynamic code is generated.")]
        private static bool IsDataContractObject(Type type)
        {
            if (type.GetCustomAttribute<DataContractAttribute>(true) == null)
            {
                return false;
            }
            var constructors = type.GetConstructors();
            if (constructors.Length != 0 &&
                !constructors.Any(c => c.GetParameters().Length == 0))
            {
                return false;
            }
            return type.GetProperties()
                .Any(p => p.CanWrite && !p.IsSpecialName &&
                    p.GetCustomAttribute<DataMemberAttribute>() != null);
        }

        private static DataMemberAttribute? GetDataMember(JsonPropertyInfo property)
        {
            if (property.AttributeProvider is not MemberInfo member)
            {
                return null;
            }
            if (member is not PropertyInfo pi || !pi.CanWrite || pi.IsSpecialName)
            {
                return null;
            }
            return pi.GetCustomAttribute<DataMemberAttribute>();
        }

        [UnconditionalSuppressMessage("Trimming", "IL2067",
            Justification = "Value types always expose an implicit parameterless " +
                "constructor; creating their default instance is Native-AOT safe.")]
        private static object? GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static bool IsDefault(object? value, object? defaultValue)
        {
            if (value == defaultValue)
            {
                return true;
            }
            if (value is null || defaultValue is null)
            {
                return false;
            }
            return value.Equals(defaultValue);
        }
    }
}
