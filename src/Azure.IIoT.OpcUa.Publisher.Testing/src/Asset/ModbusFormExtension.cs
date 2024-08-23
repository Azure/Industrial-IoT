// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Asset
{
    using Opc.Ua;
    using System;
    using System.Text;

    public static class ModbusFormExtension
    {
        /// <summary>
        /// Get datatype id
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        public static NodeId GetDataTypeId(this ModbusForm form)
        {
            switch (form.PayloadType)
            {
                case ModbusType.Xsdfloat:
                    return DataTypeIds.Float;
                case ModbusType.Xsdinteger:
                    return DataTypeIds.Integer;
                case ModbusType.Xsdboolean:
                    return DataTypeIds.Boolean;
                case ModbusType.Xsdstring:
                    return DataTypeIds.String;
                case ModbusType.Xsddecimal:
                    return DataTypeIds.Decimal;
                case ModbusType.Xsdbyte:
                    return DataTypeIds.SByte;
                case ModbusType.Xsdshort:
                    return DataTypeIds.Int16;
                case ModbusType.Xsdint:
                    return DataTypeIds.Int32;
                case ModbusType.Xsdlong:
                    return DataTypeIds.Int64;
                case ModbusType.XsdunsignedByte:
                    return DataTypeIds.Byte;
                case ModbusType.XsdunsignedShort:
                    return DataTypeIds.UInt16;
                case ModbusType.XsdunsignedInt:
                    return DataTypeIds.UInt32;
                case ModbusType.XsdunsignedLong:
                    return DataTypeIds.UInt64;
                case ModbusType.Xsddouble:
                    return DataTypeIds.Double;
                case ModbusType.XsdhexBinary:
                    return DataTypeIds.ByteString;
                default:
                    throw ServiceResultException.Create(StatusCodes.BadNotReadable,
                        "Invalid data type");
            }
        }

        /// <summary>
        /// Convert value to buffer (little endian) to write
        /// </summary>
        /// <param name="form"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        public static ReadOnlyMemory<byte> ToBuffer(this ModbusForm form, object value)
        {
            switch (form.PayloadType)
            {
                case ModbusType.Xsdfloat:
                    return BitConverter.GetBytes((float)value);
                case ModbusType.Xsdinteger:
                    return BitConverter.GetBytes((int)value);
                case ModbusType.Xsdboolean:
                    return BitConverter.GetBytes((bool)value);
                case ModbusType.Xsdstring:
                    return Encoding.UTF8.GetBytes((string)value);
                case ModbusType.Xsddecimal:
                    return GetBytes((decimal)value);
                case ModbusType.Xsdbyte:
                    return new[] { (byte)(sbyte)value };
                case ModbusType.Xsdshort:
                    return BitConverter.GetBytes((short)value);
                case ModbusType.Xsdint:
                    return BitConverter.GetBytes((int)value);
                case ModbusType.Xsdlong:
                    return BitConverter.GetBytes((long)value);
                case ModbusType.XsdunsignedByte:
                    return new[] { (byte)value };
                case ModbusType.XsdunsignedShort:
                    return BitConverter.GetBytes((ushort)value);
                case ModbusType.XsdunsignedInt:
                    return BitConverter.GetBytes((uint)value);
                case ModbusType.XsdunsignedLong:
                    return BitConverter.GetBytes((ulong)value);
                case ModbusType.Xsddouble:
                    return BitConverter.GetBytes((double)value);
                case ModbusType.XsdhexBinary:
                    return (byte[])value;
                default:
                    throw ServiceResultException.Create(StatusCodes.BadNotReadable,
                        "Invalid data type");
            }
        }

        /// <summary>
        /// Convert (little endian) value to object based on modbus type
        /// </summary>
        /// <param name="form"></param>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ServiceResult ToObject(this ModbusForm form,
            ReadOnlyMemory<byte> buffer, ref object? value)
        {
            switch (form.PayloadType)
            {
                case ModbusType.Xsdfloat:
                    value = BitConverter.ToSingle(buffer.Span);
                    break;
                case ModbusType.Xsdinteger:
                    value = BitConverter.ToInt32(buffer.Span);
                    break;
                case ModbusType.Xsdboolean:
                    value = BitConverter.ToBoolean(buffer.Span);
                    break;
                case ModbusType.Xsdstring:
                    value = Encoding.UTF8.GetString(buffer.Span);
                    break;
                case ModbusType.Xsddecimal:
                    value = ToDecimal(buffer.Span);
                    break;
                case ModbusType.Xsdbyte:
                    value = (sbyte)buffer.Span[0];
                    break;
                case ModbusType.Xsdshort:
                    value = BitConverter.ToInt16(buffer.Span);
                    break;
                case ModbusType.Xsdint:
                    value = BitConverter.ToInt32(buffer.Span);
                    break;
                case ModbusType.Xsdlong:
                    value = BitConverter.ToInt64(buffer.Span);
                    break;
                case ModbusType.XsdunsignedByte:
                    value = buffer.Span[0];
                    break;
                case ModbusType.XsdunsignedShort:
                    value = BitConverter.ToUInt16(buffer.Span);
                    break;
                case ModbusType.XsdunsignedInt:
                    value = BitConverter.ToUInt32(buffer.Span);
                    break;
                case ModbusType.XsdunsignedLong:
                    value = BitConverter.ToUInt64(buffer.Span);
                    break;
                case ModbusType.Xsddouble:
                    value = BitConverter.ToDouble(buffer.Span);
                    break;
                case ModbusType.XsdhexBinary:
                    value = buffer.ToArray();
                    break;
                default:
                    return ServiceResult.Create(StatusCodes.BadNotReadable,
                        "Invalid data type");
            }
            return ServiceResult.Good;
        }

        private static decimal ToDecimal(ReadOnlySpan<byte> bytes)
        {
            int[] bits = new int[4];
            bits[0] = bytes[0] | (bytes[1] << 8) | (bytes[2] << 0x10) | (bytes[3] << 0x18); //lo
            bits[1] = bytes[4] | (bytes[5] << 8) | (bytes[6] << 0x10) | (bytes[7] << 0x18); //mid
            bits[2] = bytes[8] | (bytes[9] << 8) | (bytes[10] << 0x10) | (bytes[11] << 0x18); //hi
            bits[3] = bytes[12] | (bytes[13] << 8) | (bytes[14] << 0x10) | (bytes[15] << 0x18); //flags
            return new decimal(bits);
        }

        private static byte[] GetBytes(decimal d)
        {
            byte[] bytes = new byte[16];

            int[] bits = decimal.GetBits(d);
            int lo = bits[0];
            int mid = bits[1];
            int hi = bits[2];
            int flags = bits[3];

            bytes[0] = (byte)lo;
            bytes[1] = (byte)(lo >> 8);
            bytes[2] = (byte)(lo >> 0x10);
            bytes[3] = (byte)(lo >> 0x18);
            bytes[4] = (byte)mid;
            bytes[5] = (byte)(mid >> 8);
            bytes[6] = (byte)(mid >> 0x10);
            bytes[7] = (byte)(mid >> 0x18);
            bytes[8] = (byte)hi;
            bytes[9] = (byte)(hi >> 8);
            bytes[10] = (byte)(hi >> 0x10);
            bytes[11] = (byte)(hi >> 0x18);
            bytes[12] = (byte)flags;
            bytes[13] = (byte)(flags >> 8);
            bytes[14] = (byte)(flags >> 0x10);
            bytes[15] = (byte)(flags >> 0x18);

            return bytes;
        }
    }
}
