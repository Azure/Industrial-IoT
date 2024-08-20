/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
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

#nullable enable

namespace Asset
{
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Buffers.Binary;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    internal static class ModbusProtocol
    {
        /// <summary>
        /// Encapsulate MODBUS Application Protocol header
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <param name="UnitId"></param>
        /// <param name="ProtocolId"></param>
        internal record struct Mbap(ushort TransactionId, byte UnitId, ushort ProtocolId = 0)
        {
            public const int Length = 7;
        }

        /// <summary>
        /// Write a request to read modbus data
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="function"></param>
        /// <param name="mbap"></param>
        /// <param name="startingAddress"></param>
        /// <param name="quantity"></param>
        /// <exception cref="NotSupportedException"></exception>
        public static void EncodeReadRequest(this IBufferWriter<byte> writer,
            ModbusFunction function, Mbap mbap, ushort startingAddress, ushort quantity)
        {
            switch (function)
            {
                case ModbusFunction.ReadCoil:
                    writer.EncodeReadCoilsRequest(mbap, startingAddress, quantity);
                    break;
                case ModbusFunction.ReadDiscreteInput:
                    writer.EncodeReadDiscreteInputRequest(mbap, startingAddress, quantity);
                    break;
                case ModbusFunction.ReadHoldingRegisters:
                    writer.EncodeReadHoldingRegistersRequest(mbap, startingAddress, quantity);
                    break;
                case ModbusFunction.ReadInputRegisters:
                    writer.EncodeReadInputRegistersRequest(mbap, startingAddress, quantity);
                    break;
                default:
                    throw new NotSupportedException("Not a read request");
            }
        }

        /// <summary>
        /// Write a request to write modbus data
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="function"></param>
        /// <param name="mbap"></param>
        /// <param name="startingAddress"></param>
        /// <param name="quantity"></param>
        /// <param name="registersOrCoils"></param>
        /// <exception cref="NotSupportedException"></exception>
        public static void WriteWriteRequest(this IBufferWriter<byte> writer,
            ModbusFunction function, Mbap mbap, ushort startingAddress, ushort quantity,
            ReadOnlySpan<byte> registersOrCoils)
        {
            switch (function)
            {
                case ModbusFunction.WriteSingleCoil:
                    writer.EncodeWriteSingleCoilRequest(mbap, startingAddress,
                        registersOrCoils);
                    break;
                case ModbusFunction.WriteMultipleCoils:
                    writer.EncodeWriteMultipleCoilsRequest(mbap, startingAddress, quantity,
                        registersOrCoils);
                    break;
                case ModbusFunction.WriteSingleHoldingRegister:
                    writer.EncodeWriteSingleRegisterRequest(mbap, startingAddress,
                        registersOrCoils);
                    break;
                case ModbusFunction.WriteMultipleHoldingRegisters:
                    writer.EncodeWriteMultipleRegistersRequest(mbap, startingAddress, quantity,
                        registersOrCoils);
                    break;
                default:
                    throw new NotSupportedException("Not a write request");
            }
        }

        /// <summary>
        /// Try to read the read response
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="function"></param>
        /// <param name="response"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static bool TryReadReadResponse(this IBufferWriter<byte> writer,
            ModbusFunction function, ref ReadOnlySequence<byte> response, ref ServiceResult error)
        {
            switch (function)
            {
                case ModbusFunction.ReadCoil:
                    return writer.TryReadReadCoilsResponse(ref response, ref error);
                case ModbusFunction.ReadDiscreteInput:
                    return writer.TryReadReadDiscreteInputResponse(ref response, ref error);
                case ModbusFunction.ReadHoldingRegisters:
                    return writer.TryReadReadReadHoldingRegistersResponse(ref response, ref error);
                case ModbusFunction.ReadInputRegisters:
                    return writer.TryReadReadInputRegistersResponse(ref response, ref error);
                default:
                    throw new NotSupportedException("Not a read request");
            }
        }

        /// <summary>
        /// Try to read the write response
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="function"></param>
        /// <param name="response"></param>
        /// <param name="outputAddress"></param>
        /// <param name="quantity"></param>
        /// <param name="registersOrCoils"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static bool TryReadWriteResponse(this IBufferWriter<byte> writer,
            ModbusFunction function, ref ReadOnlySequence<byte> response,
            ushort outputAddress, ushort quantity, ReadOnlySpan<byte> registersOrCoils,
            ref ServiceResult error)
        {
            switch (function)
            {
                case ModbusFunction.WriteSingleCoil:
                    return writer.TryReadWriteSingleCoilResponse(ref response,
                        outputAddress, registersOrCoils, ref error);
                case ModbusFunction.WriteMultipleCoils:
                    return writer.TryReadWriteMultipleCoilsResponse(ref response,
                        outputAddress, quantity, ref error);
                case ModbusFunction.WriteSingleHoldingRegister:
                    return writer.TryReadWriteSingleRegisterResponse(ref response,
                        outputAddress, registersOrCoils, ref error);
                case ModbusFunction.WriteMultipleHoldingRegisters:
                    return writer.TryReadWriteMultipleRegistersResponse(ref response,
                        outputAddress, quantity, ref error);
                default:
                    throw new NotSupportedException("Not a read request");
            }
        }

        /// <summary>
        /// Encode read coils request
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="mbap"></param>
        /// <param name="startingAddress"></param>
        /// <param name="quantityOfCoils"></param>
        public static void EncodeReadCoilsRequest(this IBufferWriter<byte> writer,
            Mbap mbap, ushort startingAddress, ushort quantityOfCoils)
        {
            ArgumentOutOfRangeException.ThrowIfZero(quantityOfCoils);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(quantityOfCoils, 2000);
            EncodeSimpleRequest(ModbusFunction.ReadCoil, mbap,
                startingAddress, quantityOfCoils, writer);
        }

        /// <summary>
        /// Try read coils response
        /// </summary>
        /// <param name="output"></param>
        /// <param name="response"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static bool TryReadReadCoilsResponse(this IBufferWriter<byte> output,
            ref ReadOnlySequence<byte> response, ref ServiceResult error)
        {
            return TryReadBitResponse(ModbusFunction.ReadCoil, ref response,
                output, ref error);
        }

        /// <summary>
        /// Encode writing a single coil request
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="mbap"></param>
        /// <param name="outputAddress"></param>
        /// <param name="outputValue"></param>
        public static void EncodeWriteSingleCoilRequest(this IBufferWriter<byte> writer,
            Mbap mbap, ushort outputAddress, ReadOnlySpan<byte> outputValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(outputValue.Length, 1);
            var onOff = outputValue[0] != 0 ? (ushort)0xFF00 : (ushort)0x0;
            EncodeSimpleRequest(ModbusFunction.WriteSingleCoil, mbap,
                outputAddress, onOff, writer);
        }

        public static bool TryReadWriteSingleCoilResponse(this IBufferWriter<byte> writer,
            ref ReadOnlySequence<byte> reader, ushort outputAddress, ReadOnlySpan<byte> outputValue,
            ref ServiceResult error)
        {
            Debug.Assert(writer != null);
            ArgumentOutOfRangeException.ThrowIfLessThan(outputValue.Length, 1);
            var onOff = outputValue[0] != 0 ? (ushort)0xFF00 : (ushort)0x0;
            return TryReadEchoResponse(ModbusFunction.WriteSingleCoil, ref reader,
                outputAddress, onOff, ref error);
        }

        public static void EncodeWriteMultipleCoilsRequest(this IBufferWriter<byte> writer,
            Mbap mbap, ushort outputAddress, ushort quantityOfOutputs, ReadOnlySpan<byte> coilStates)
        {
            ArgumentOutOfRangeException.ThrowIfZero(quantityOfOutputs);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(quantityOfOutputs, 0x7b0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(coilStates.Length, byte.MaxValue);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(quantityOfOutputs, coilStates.Length * 8);

            EncodeWriteBitRequest(ModbusFunction.WriteMultipleCoils, mbap,
                outputAddress, quantityOfOutputs, coilStates, writer);
        }

        public static bool TryReadWriteMultipleCoilsResponse(this IBufferWriter<byte> writer,
            ref ReadOnlySequence<byte> response, ushort outputAddress, ushort quantityOfOutputs,
            ref ServiceResult error)
        {
            Debug.Assert(writer != null);
            return TryReadEchoResponse(ModbusFunction.WriteMultipleCoils, ref response,
                outputAddress, quantityOfOutputs, ref error);
        }

        /// <summary>
        /// Encode write single register request
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="mbap"></param>
        /// <param name="outputAddress"></param>
        /// <param name="outputValue"></param>
        public static void EncodeWriteSingleRegisterRequest(this IBufferWriter<byte> writer,
            Mbap mbap, ushort outputAddress, ReadOnlySpan<byte> outputValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(outputValue.Length, 2);
            var value = BitConverter.ToUInt16(outputValue);
            EncodeSimpleRequest(ModbusFunction.WriteSingleHoldingRegister, mbap,
                outputAddress, value, writer);
        }

        /// <summary>
        /// Try read write single register response
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="response"></param>
        /// <param name="outputAddress"></param>
        /// <param name="outputValue"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static bool TryReadWriteSingleRegisterResponse(this IBufferWriter<byte> writer,
            ref ReadOnlySequence<byte> response, ushort outputAddress, ReadOnlySpan<byte> outputValue,
            ref ServiceResult error)
        {
            Debug.Assert(writer != null);
            var value = BitConverter.ToUInt16(outputValue);
            return TryReadEchoResponse(ModbusFunction.WriteSingleHoldingRegister, ref response,
                outputAddress, value, ref error);
        }

        /// <summary>
        /// Encode write multi register request
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="mbap"></param>
        /// <param name="outputAddress"></param>
        /// <param name="quantityOfRegisters"></param>
        /// <param name="registers"></param>
        public static void EncodeWriteMultipleRegistersRequest(this IBufferWriter<byte> writer,
            Mbap mbap, ushort outputAddress, ushort quantityOfRegisters, ReadOnlySpan<byte> registers)
        {
            ArgumentOutOfRangeException.ThrowIfZero(quantityOfRegisters);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(quantityOfRegisters, 123);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(quantityOfRegisters, registers.Length / 2);

            EncodeWriteWordRequest(ModbusFunction.WriteMultipleHoldingRegisters, mbap,
                outputAddress, quantityOfRegisters, registers, writer);
        }

        /// <summary>
        /// Try read multi register response
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="response"></param>
        /// <param name="outputAddress"></param>
        /// <param name="quantityOfRegisters"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static bool TryReadWriteMultipleRegistersResponse(this IBufferWriter<byte> writer,
            ref ReadOnlySequence<byte> response, ushort outputAddress, ushort quantityOfRegisters,
            ref ServiceResult error)
        {
            Debug.Assert(writer != null);
            return TryReadEchoResponse(ModbusFunction.WriteMultipleHoldingRegisters, ref response,
                outputAddress, quantityOfRegisters, ref error);
        }

        /// <summary>
        /// Encode read discrete input request
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="mbap"></param>
        /// <param name="startingAddress"></param>
        /// <param name="quantityOfInputs"></param>
        public static void EncodeReadDiscreteInputRequest(this IBufferWriter<byte> writer,
            Mbap mbap, ushort startingAddress, ushort quantityOfInputs)
        {
            ArgumentOutOfRangeException.ThrowIfZero(quantityOfInputs);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(quantityOfInputs, 2000);
            EncodeSimpleRequest(ModbusFunction.ReadDiscreteInput, mbap,
                startingAddress, quantityOfInputs, writer);
        }

        /// <summary>
        /// Try read response from discrete input
        /// </summary>
        /// <param name="inputStatus"></param>
        /// <param name="response"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static bool TryReadReadDiscreteInputResponse(this IBufferWriter<byte> inputStatus,
            ref ReadOnlySequence<byte> response, ref ServiceResult error)
        {
            return TryReadBitResponse(ModbusFunction.ReadDiscreteInput, ref response,
                inputStatus, ref error);
        }

        /// <summary>
        /// Encode read holding registers request
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="mbap"></param>
        /// <param name="startingAddress"></param>
        /// <param name="quantityOfRegisters"></param>
        public static void EncodeReadHoldingRegistersRequest(this IBufferWriter<byte> writer,
            Mbap mbap, ushort startingAddress, ushort quantityOfRegisters)
        {
            ArgumentOutOfRangeException.ThrowIfZero(quantityOfRegisters);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(quantityOfRegisters, 125);
            EncodeSimpleRequest(ModbusFunction.ReadHoldingRegisters, mbap,
                startingAddress, quantityOfRegisters, writer);
        }

        /// <summary>
        /// Try read holding registers response
        /// </summary>
        /// <param name="registerValues"></param>
        /// <param name="response"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static bool TryReadReadReadHoldingRegistersResponse(this IBufferWriter<byte> registerValues,
            ref ReadOnlySequence<byte> response, ref ServiceResult error)
        {
            return TryReadRegisterResponse(ModbusFunction.ReadHoldingRegisters,
                ref response, registerValues, ref error);
        }

        /// <summary>
        /// Encode read input registers request
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="mbap"></param>
        /// <param name="startingAddress"></param>
        /// <param name="quantityOfInputRegisters"></param>
        public static void EncodeReadInputRegistersRequest(this IBufferWriter<byte> writer,
            Mbap mbap, ushort startingAddress, ushort quantityOfInputRegisters)
        {
            ArgumentOutOfRangeException.ThrowIfZero(quantityOfInputRegisters);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(quantityOfInputRegisters, 0x7D);
            EncodeSimpleRequest(ModbusFunction.ReadInputRegisters, mbap,
                startingAddress, quantityOfInputRegisters, writer);
        }

        /// <summary>
        /// Try read input registers response
        /// </summary>
        /// <param name="registerValues"></param>
        /// <param name="response"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static bool TryReadReadInputRegistersResponse(this IBufferWriter<byte> registerValues,
            ref ReadOnlySequence<byte> response, ref ServiceResult error)
        {
            return TryReadRegisterResponse(ModbusFunction.ReadInputRegisters,
                ref response, registerValues, ref error);
        }

        /// <summary>
        /// Write a modbus request to read a quantity of coils or registers
        /// </summary>
        /// <param name="function"></param>
        /// <param name="mbap"></param>
        /// <param name="startingAddress"></param>
        /// <param name="quantityOrOutputValue"></param>
        /// <param name="writer"></param>
        private static void EncodeSimpleRequest(ModbusFunction function,
            Mbap mbap, ushort startingAddress, ushort quantityOrOutputValue,
            IBufferWriter<byte> writer)
        {
            const int length = Mbap.Length + 5;
            Span<byte> adu = writer.GetSpan(length);

            var request = adu.Slice(Mbap.Length, 5);
            WriteMbap(mbap, request.Length, adu);
            WriteRequestHeader(function, startingAddress, quantityOrOutputValue,
                request);

            writer.Advance(length);
        }

        /// <summary>
        /// Write a modbus request with a buffer and buffer count
        /// </summary>
        /// <param name="function"></param>
        /// <param name="mbap"></param>
        /// <param name="startingAddress"></param>
        /// <param name="quantity"></param>
        /// <param name="bitBuffer"></param>
        /// <param name="writer"></param>
        private static void EncodeWriteBitRequest(ModbusFunction function,
            Mbap mbap, ushort startingAddress, ushort quantity,
            ReadOnlySpan<byte> bitBuffer, IBufferWriter<byte> writer)
        {
            var length = Mbap.Length + 6 + bitBuffer.Length;
            Span<byte> adu = writer.GetSpan(length);

            var request = adu.Slice(Mbap.Length, length - Mbap.Length);
            WriteRequestHeader(function, startingAddress, quantity, request);
            request[5] = (byte)bitBuffer.Length;
            bitBuffer.CopyTo(request[6..]);
            WriteMbap(mbap, request.Length, adu);
            writer.Advance(length);
        }

        /// <summary>
        /// Write a modbus request with a buffer and buffer count
        /// </summary>
        /// <param name="function"></param>
        /// <param name="mbap"></param>
        /// <param name="startingAddress"></param>
        /// <param name="quantity"></param>
        /// <param name="words"></param>
        /// <param name="writer"></param>
        private static void EncodeWriteWordRequest(ModbusFunction function,
            Mbap mbap, ushort startingAddress, ushort quantity, ReadOnlySpan<byte> words,
            IBufferWriter<byte> writer)
        {
            var length = Mbap.Length + 6 + words.Length;
            Span<byte> adu = writer.GetSpan(length);

            var request = adu.Slice(Mbap.Length, length - Mbap.Length);
            WriteRequestHeader(function, startingAddress, quantity, request);
            request[5] = (byte)words.Length;

            CopyWords(words, request[6..]);
            WriteMbap(mbap, request.Length, adu);
            writer.Advance(length);
        }

        /// <summary>
        /// Read and validate echo response
        /// </summary>
        /// <param name="function"></param>
        /// <param name="response"></param>
        /// <param name="outputAddress"></param>
        /// <param name="outputValue"></param>
        /// <param name="error"></param>
        /// <exception cref="ServiceResultException"></exception>
        private static bool TryReadEchoResponse(ModbusFunction function,
            ref ReadOnlySequence<byte> response, ushort outputAddress, ushort outputValue,
            ref ServiceResult error)
        {
            var reader = new SequenceReader<byte>(response);
            if (!TryReadFunctionOrError(ref reader, function, ref error) ||
                !reader.TryReadBigEndian(out short address) ||
                !reader.TryReadBigEndian(out short value))
            {
                return false;
            }
            if (outputAddress != address || value != outputValue)
            {
                // Should not happen - reconnect
                throw new ServiceResultException(
                    "Unexpected address or value returned in echo response");
            }
            return true;
        }

        /// <summary>
        /// Read coils or input states from response
        /// </summary>
        /// <param name="function"></param>
        /// <param name="response"></param>
        /// <param name="inputStatus"></param>
        /// <param name="error"></param>
        /// <exception cref="ServiceResultException"></exception>
        private static bool TryReadBitResponse(ModbusFunction function,
            ref ReadOnlySequence<byte> response, IBufferWriter<byte> inputStatus,
            ref ServiceResult error)
        {
            var reader = new SequenceReader<byte>(response);
            if (!TryReadFunctionOrError(ref reader, function, ref error) ||
                !reader.TryReadBigEndian(out short byteCount))
            {
                return false;
            }
            if (reader.UnreadSpan.Length < byteCount)
            {
                return false;
            }
            inputStatus.Write(reader.UnreadSpan.Slice(byteCount));
            reader.Advance(byteCount);
            return true;
        }

        /// <summary>
        /// Read register values from response
        /// </summary>
        /// <param name="function"></param>
        /// <param name="response"></param>
        /// <param name="registerValues"></param>
        /// <param name="error"></param>
        /// <exception cref="ServiceResultException"></exception>
        private static bool TryReadRegisterResponse(ModbusFunction function,
            ref ReadOnlySequence<byte> response, IBufferWriter<byte> registerValues,
            ref ServiceResult error)
        {
            var reader = new SequenceReader<byte>(response);
            return
                TryReadFunctionOrError(ref reader, function, ref error) &&
                reader.TryReadBigEndian(out short byteCount) &&
                TryCopyWords(ref reader, registerValues, byteCount);
        }

        /// <summary>
        /// Copy from reader to span
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="registerValues"></param>
        /// <param name="byteCount"></param>
        /// <returns></returns>
        private static bool TryCopyWords(ref SequenceReader<byte> reader,
            IBufferWriter<byte> registerValues, short byteCount)
        {
            var span = registerValues.GetSpan(byteCount);
            Span<byte> tmp = stackalloc byte[2];
            for (var i = 0; i < byteCount; i += 2)
            {
                if (!reader.TryRead(out tmp[0]) || !reader.TryRead(out tmp[1]))
                {
                    return false;
                }
                if (BitConverter.IsLittleEndian)
                {
                    // Convert from big endian by swapping
                    span[i + 1] = tmp[0];
                    span[i] = tmp[1];
                }
                else
                {
                    span[i] = tmp[0];
                    span[1 + 1] = tmp[1];
                }
            }
            registerValues.Advance(byteCount);
            return true;
        }

        /// <summary>
        /// Copy words from span to span
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="span"></param>
        private static void CopyWords(ReadOnlySpan<byte> reader,
            Span<byte> span)
        {
            Debug.Assert(span.Length == reader.Length);
            if (BitConverter.IsLittleEndian)
            {
                // Convert from big endian by swapping
                for (var i = 0; i < reader.Length; i += 2)
                {
                    span[i + 1] = reader[i];
                    span[i] = reader[i + 1];
                }
            }
            else
            {
                reader.CopyTo(span);
            }
        }

        private static void WriteRequestHeader(ModbusFunction function,
            ushort startingAddress, ushort quantity, Span<byte> buffer)
        {
            buffer[0] = (byte)function;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(1, 2), startingAddress);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(3, 2), quantity);
        }

        private static void WriteMbap(Mbap mbap, int length, Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= Mbap.Length);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(0, 2), mbap.TransactionId);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(2, 2), mbap.ProtocolId);
            // length is the number of bytes following this field which includes unitid
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(4, 2), (ushort)(length + 1));
            buffer[6] = mbap.UnitId;
        }

        private static bool TryReadFunctionOrError(ref SequenceReader<byte> reader,
            ModbusFunction function, ref ServiceResult error)
        {
            if (reader.TryRead(out var functionCode))
            {
                if (functionCode == (byte)function)
                {
                    return true;
                }
                else if ((functionCode & ~0x80) == (byte)function)
                {
                    if (reader.TryRead(out var errorCode))
                    {
                        error = ToServiceResult(errorCode);
                        return true;
                    }
                }
                else
                {
                    ThrowBadResponse();
                }
            }
            return false;

            [DoesNotReturn]
            static void ThrowBadResponse()
            {
                throw new ServiceResultException("Bad response");
            }

            static ServiceResult ToServiceResult(byte errorCode)
            {
                switch (errorCode)
                {
                    case 1: return new ServiceResult(StatusCodes.Bad, "Illegal function");
                    case 2: return new ServiceResult(StatusCodes.Bad, "Illegal data address");
                    case 3: return new ServiceResult(StatusCodes.Bad, "Illegal data value");
                    case 4: return new ServiceResult(StatusCodes.Bad, "Server failure");
                    case 5: return new ServiceResult(StatusCodes.Bad, "Acknowledge");
                    case 6: return new ServiceResult(StatusCodes.Bad, "Server busy");
                    case 7: return new ServiceResult(StatusCodes.Bad, "Negative acknowledge");
                    case 8: return new ServiceResult(StatusCodes.Bad, "Memory parity error");
                    case 10: return new ServiceResult(StatusCodes.Bad, "Gateway path unavailable");
                    case 11: return new ServiceResult(StatusCodes.Bad, "Target unit failed to respond");
                    default: return new ServiceResult(StatusCodes.Bad, "Unknown error");
                }
            }
        }
    }
}
