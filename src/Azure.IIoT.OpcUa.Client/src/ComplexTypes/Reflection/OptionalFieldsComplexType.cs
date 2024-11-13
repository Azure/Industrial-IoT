/* ========================================================================
 * Copyright (c) 2005-2022 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client.ComplexTypes.Reflection
{
    using System;
    using System.Text;

    /// <summary>
    /// A complex type with optional fields.
    /// </summary>
    public class OptionalFieldsComplexType : BaseComplexType
    {
        /// <inheritdoc/>
        public override StructureType StructureType
            => StructureType.StructureWithOptionalFields;

        /// <summary>
        /// The encoding mask for the optional fields.
        /// </summary>
        public uint EncodingMask { get; private set; }

        /// <summary>
        /// Initializes the object with default values.
        /// </summary>
        public OptionalFieldsComplexType()
        {
            EncodingMask = 0;

            InitializePropertyAttributes();
        }

        /// <summary>
        /// Initializes the object with a <paramref name="typeId"/>.
        /// </summary>
        /// <param name="typeId">The type to copy and create an instance from</param>
        public OptionalFieldsComplexType(ExpandedNodeId typeId)
            : base(typeId)
        {
            EncodingMask = 0;

            InitializePropertyAttributes();
        }

        /// <inheritdoc/>
        public override object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Makes a deep copy of the object.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public new object MemberwiseClone()
        {
            var clone = (OptionalFieldsComplexType)base.MemberwiseClone();
            clone.EncodingMask = EncodingMask;
            return clone;
        }

        /// <inheritdoc/>
        public override void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(XmlNamespace);

            if (encoder.UseReversibleEncoding)
            {
                encoder.WriteUInt32("EncodingMask", EncodingMask);
            }

            foreach (var property in GetPropertyEnumerator())
            {
                if (property.IsOptional &&
                    (property.OptionalFieldMask & EncodingMask) == 0)
                {
                    continue;
                }

                EncodeProperty(encoder, property);
            }
            encoder.PopNamespace();
        }

        /// <inheritdoc/>
        public override void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(XmlNamespace);

            EncodingMask = decoder.ReadUInt32("EncodingMask");

            foreach (var property in GetPropertyEnumerator())
            {
                if (property.IsOptional &&
                    (property.OptionalFieldMask & EncodingMask) == 0)
                {
                    continue;
                }

                DecodeProperty(decoder, property);
            }
            decoder.PopNamespace();
        }

        /// <inheritdoc/>
        public override bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }
            if (encodeable is not OptionalFieldsComplexType valueBaseType)
            {
                return false;
            }
            if (EncodingMask != valueBaseType.EncodingMask)
            {
                return false;
            }
            var valueType = valueBaseType.GetType();
            if (GetType() != valueType)
            {
                return false;
            }
            foreach (var property in GetPropertyEnumerator())
            {
                if (property.IsOptional &&
                    (property.OptionalFieldMask & EncodingMask) == 0)
                {
                    continue;
                }

                if (!Utils.IsEqual(property.GetValue(this),
                    property.GetValue(valueBaseType)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public override string ToString(string? format, IFormatProvider? formatProvider)
        {
            if (format != null)
            {
                throw new FormatException($"Invalid format string: '{format}'.");
            }
            var body = new StringBuilder();
            foreach (var property in GetPropertyEnumerator())
            {
                if (property.IsOptional &&
                    (property.OptionalFieldMask & EncodingMask) == 0)
                {
                    continue;
                }
                AppendPropertyValue(formatProvider, body,
                    property.GetValue(this), property.ValueRank);
            }
            if (body.Length > 0)
            {
                body.Append('}');
                return body.ToString();
            }
            if (!NodeId.IsNull(TypeId))
            {
                return string.Format(formatProvider, "{{{0}}}", TypeId);
            }
            return "(null)";
        }

#if INDEXED
        /// <inheritdoc/>
        public override object this[int index]
        {
            get
            {
                var property = _propertyList[index];
                if (property.IsOptional &&
                    (property.OptionalFieldMask & _encodingMask) == 0)
                {
                    return null;
                }
                return property.GetValue(this);
            }
            set
            {
                var property = _propertyList[index];
                property.SetValue(this, value);
                if (property.IsOptional)
                {
                    if (value == null)
                    {
                        _encodingMask &= ~property.OptionalFieldMask;
                    }
                    else
                    {
                        _encodingMask |= property.OptionalFieldMask;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override object this[string name]
        {
            get
            {
                ComplexTypePropertyInfo property;
                if (_propertyDict.TryGetValue(name, out property))
                {
                    if (property.IsOptional &&
                        (property.OptionalFieldMask & _encodingMask) == 0)
                    {
                        return null;
                    }
                    return property.GetValue(this);
                }
                throw new KeyNotFoundException();
            }
            set
            {
                ComplexTypePropertyInfo property;
                if (_propertyDict.TryGetValue(name, out property))
                {
                    property.SetValue(this, value);
                    if (value == null)
                    {
                        _encodingMask &= ~property.OptionalFieldMask;
                    }
                    else
                    {
                        _encodingMask |= property.OptionalFieldMask;
                    }
                }
                else
                {
                    throw new KeyNotFoundException();
                }
            }
        }
#endif

        /// <summary>
        /// Initializes the property attributes.
        /// </summary>
        private void InitializePropertyAttributes()
        {
            // build optional field mask attribute
            uint optionalFieldMask = 1;
            foreach (var property in GetPropertyEnumerator())
            {
                property.OptionalFieldMask = 0;
                if (property.IsOptional)
                {
                    property.OptionalFieldMask = optionalFieldMask;
                    optionalFieldMask <<= 1;
                }
            }
        }
    }
}
