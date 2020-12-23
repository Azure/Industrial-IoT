// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Globalization;
    using System.Xml;

    /// <summary>
    /// Encoder extensions
    /// </summary>
    public static class EncoderEx {
        /// <summary>
        /// Writes the value of the variant in raw format without meta information.
        /// </summary>
        /// <param name="encoder">The encoder to use.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteRaw(this IEncoder encoder, string fieldName, Variant value) {
            encoder.WriteRaw(fieldName, value.Value, value.TypeInfo);
        }

        /// <summary>
        /// Writes the value of the DataValue in raw format without meta information.
        /// </summary>
        /// <param name="encoder">The encoder to use.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteRaw(this IEncoder encoder, string fieldName, DataValue value) {
            encoder.WriteRaw(fieldName, value.WrappedValue);
        }

        /// <summary>
        /// Writes the given value in raw format without meta information.
        /// </summary>
        /// <param name="encoder">The encoder to use.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="typeInfo">The type of the value.</param>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="ServiceResultException"></exception>
        public static void WriteRaw(this IEncoder encoder, string fieldName, object value, TypeInfo typeInfo) {
            if (value != null) {
                if (typeInfo.ValueRank < 0) {
                    switch (typeInfo.BuiltInType) {
                        case BuiltInType.Boolean:
                            encoder.WriteBoolean(fieldName, (bool)value);
                            return;
                        case BuiltInType.SByte:
                            encoder.WriteSByte(fieldName, (sbyte)value);
                            return;
                        case BuiltInType.Byte:
                            encoder.WriteByte(fieldName, (byte)value);
                            return;
                        case BuiltInType.Int16:
                            encoder.WriteInt16(fieldName, (short)value);
                            return;
                        case BuiltInType.UInt16:
                            encoder.WriteUInt16(fieldName, (ushort)value);
                            return;
                        case BuiltInType.Int32:
                            encoder.WriteInt32(fieldName, (int)value);
                            return;
                        case BuiltInType.UInt32:
                            encoder.WriteUInt32(fieldName, (uint)value);
                            return;
                        case BuiltInType.Int64:
                            encoder.WriteInt64(fieldName, (long)value);
                            return;
                        case BuiltInType.UInt64:
                            encoder.WriteUInt64(fieldName, (ulong)value);
                            return;
                        case BuiltInType.Float:
                            encoder.WriteFloat(fieldName, (float)value);
                            return;
                        case BuiltInType.Double:
                            encoder.WriteDouble(fieldName, (double)value);
                            return;
                        case BuiltInType.String:
                            encoder.WriteString(fieldName, (string)value);
                            return;
                        case BuiltInType.DateTime:
                            encoder.WriteDateTime(fieldName, (DateTime)value);
                            return;
                        case BuiltInType.Guid:
                            encoder.WriteGuid(fieldName, (Uuid)value);
                            return;
                        case BuiltInType.ByteString:
                            encoder.WriteByteString(fieldName, (byte[])value);
                            return;
                        case BuiltInType.XmlElement:
                            encoder.WriteXmlElement(fieldName, (XmlElement)value);
                            return;
                        case BuiltInType.NodeId:
                            encoder.WriteNodeId(fieldName, (NodeId)value);
                            return;
                        case BuiltInType.ExpandedNodeId:
                            encoder.WriteExpandedNodeId(fieldName, (ExpandedNodeId)value);
                            return;
                        case BuiltInType.StatusCode:
                            encoder.WriteStatusCode(fieldName, (StatusCode)value);
                            return;
                        case BuiltInType.QualifiedName:
                            encoder.WriteQualifiedName(fieldName, (QualifiedName)value);
                            return;
                        case BuiltInType.LocalizedText:
                            encoder.WriteLocalizedText(fieldName, (LocalizedText)value);
                            return;
                        case BuiltInType.ExtensionObject:
                            encoder.WriteExtensionObject(fieldName, (ExtensionObject)value);
                            return;
                        case BuiltInType.DataValue:
                            encoder.WriteDataValue(fieldName, (DataValue)value);
                            return;
                        case BuiltInType.Enumeration:
                            encoder.WriteInt32(fieldName, (int)value);
                            return;
                    }
                }
                else if (typeInfo.ValueRank <= 1) {
                    switch (typeInfo.BuiltInType) {
                        case BuiltInType.Boolean:
                            encoder.WriteBooleanArray(fieldName, (bool[])value);
                            return;
                        case BuiltInType.SByte:
                            encoder.WriteSByteArray(fieldName, (sbyte[])value);
                            return;
                        case BuiltInType.Byte:
                            encoder.WriteByteArray(fieldName, (byte[])value);
                            return;
                        case BuiltInType.Int16:
                            encoder.WriteInt16Array(fieldName, (short[])value);
                            return;
                        case BuiltInType.UInt16:
                            encoder.WriteUInt16Array(fieldName, (ushort[])value);
                            return;
                        case BuiltInType.Int32:
                            encoder.WriteInt32Array(fieldName, (int[])value);
                            return;
                        case BuiltInType.UInt32:
                            encoder.WriteUInt32Array(fieldName, (uint[])value);
                            return;
                        case BuiltInType.Int64:
                            encoder.WriteInt64Array(fieldName, (long[])value);
                            return;
                        case BuiltInType.UInt64:
                            encoder.WriteUInt64Array(fieldName, (ulong[])value);
                            return;
                        case BuiltInType.Float:
                            encoder.WriteFloatArray(fieldName, (float[])value);
                            return;
                        case BuiltInType.Double:
                            encoder.WriteDoubleArray(fieldName, (double[])value);
                            return;
                        case BuiltInType.String:
                            encoder.WriteStringArray(fieldName, (string[])value);
                            return;
                        case BuiltInType.DateTime:
                            encoder.WriteDateTimeArray(fieldName, (DateTime[])value);
                            return;
                        case BuiltInType.Guid:
                            encoder.WriteGuidArray(fieldName, (Uuid[])value);
                            return;
                        case BuiltInType.ByteString:
                            encoder.WriteByteStringArray(fieldName, (byte[][])value);
                            return;
                        case BuiltInType.XmlElement:
                            encoder.WriteXmlElementArray(fieldName, (XmlElement[])value);
                            return;
                        case BuiltInType.NodeId:
                            encoder.WriteNodeIdArray(fieldName, (NodeId[])value);
                            return;
                        case BuiltInType.ExpandedNodeId:
                            encoder.WriteExpandedNodeIdArray(fieldName, (ExpandedNodeId[])value);
                            return;
                        case BuiltInType.StatusCode:
                            encoder.WriteStatusCodeArray(fieldName, (StatusCode[])value);
                            return;
                        case BuiltInType.QualifiedName:
                            encoder.WriteQualifiedNameArray(fieldName, (QualifiedName[])value);
                            return;
                        case BuiltInType.LocalizedText:
                            encoder.WriteLocalizedTextArray(fieldName, (LocalizedText[])value);
                            return;
                        case BuiltInType.ExtensionObject:
                            encoder.WriteExtensionObjectArray(fieldName, (ExtensionObject[])value);
                            return;
                        case BuiltInType.DataValue:
                            encoder.WriteDataValueArray(fieldName, (DataValue[])value);
                            return;
                        case BuiltInType.Variant:
                            if (value is Variant[] variantArray) {
                                encoder.WriteVariantArray(fieldName, variantArray);
                                return;
                            }
                            if (value is object[] objArray) {
                                throw new NotImplementedException();
                            }
                            throw ServiceResultException.Create(2147876864U, "Unexpected type encountered while encoding an array of Variants: {0}", (object)value.GetType());
                        case BuiltInType.Enumeration:
                            var enumArray = value as Enum[];
                            var strArray = new string[enumArray.Length];

                            for (var index = 0; index < enumArray.Length; ++index) {
                                var str = enumArray[index].ToString() + "_" + enumArray[index].As<int>().ToString(CultureInfo.InvariantCulture);
                                strArray[index] = str;
                            }
                            encoder.WriteStringArray(null, strArray);
                            return;
                    }
                }
                else if (typeInfo.ValueRank > 1) {
                    encoder.WriteMatrix(fieldName, (Matrix)value);
                    return;
                }
                throw new ServiceResultException(2147876864U, Utils.Format("Type '{0}' is not allowed in an Variant.", (object)value.GetType().FullName));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="fieldName"></param>
        /// <param name="matrix"></param>
        public static void WriteMatrix(this IEncoder encoder, string fieldName, Matrix matrix) {
            encoder.WriteEncodeable(fieldName, new EncodeableMatrix(matrix));
        }


        /// <summary>
        /// Write typed enumerated
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="encoder"></param>
        /// <param name="field"></param>
        /// <param name="enums"></param>
        /// <returns></returns>
        public static void WriteEnumeratedArray<T>(this IEncoder encoder, string field, T[] enums)
            where T : Enum {
            encoder.WriteEnumeratedArray(field, enums, typeof(T));
        }

        /// <summary>
        /// Write encodeables
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="encoder"></param>
        /// <param name="field"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static void WriteEncodeableArray<T>(this IEncoder encoder, string field,
            IEnumerable<T> values) where T : IEncodeable {
            encoder.WriteEncodeableArray(field, values.Cast<IEncodeable>().ToArray(), typeof(T));
        }

        /// <summary>
        /// Write encodeable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="encoder"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static void WriteEncodeable<T>(this IEncoder encoder, string field, T value)
            where T : IEncodeable {
            encoder.WriteEncodeable(field, value, typeof(T));
        }

        /// <summary>
        /// Wrapper class to write a matrix as an IEncodable
        /// </summary>
        private class EncodeableMatrix : IEncodeable {
            private readonly Matrix _matrix;

            public EncodeableMatrix(Matrix matrix) {
                _matrix = matrix;
            }

            public void Encode(IEncoder encoder) {
                encoder.WriteVariant("Matrix", new Variant(_matrix.Elements, new TypeInfo(_matrix.TypeInfo.BuiltInType, 1)));
                encoder.WriteInt32Array("Dimensions", _matrix.Dimensions);
            }

            public void Decode(IDecoder decoder) {
                throw new NotImplementedException();
            }

            public bool IsEqual(IEncodeable encodeable) {
                throw new NotImplementedException();
            }

            public ExpandedNodeId TypeId { get; }
            public ExpandedNodeId BinaryEncodingId { get; }
            public ExpandedNodeId XmlEncodingId { get; }
        }
    }
}
