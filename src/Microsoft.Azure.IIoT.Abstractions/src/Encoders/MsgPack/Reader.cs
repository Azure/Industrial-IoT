// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MsgPack {
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Adorns a stream and reads msgpack types from it as per
    /// https://github.com/msgpack/msgpack/blob/master/spec.md
    /// </summary>
    public class Reader : IDisposable {

        /// <summary>
        /// Last unsigned value read.
        /// </summary>
        public ulong Value {
            get; private set;
        }

        /// <summary>
        /// Last signed value.
        /// </summary>
        public long Signed {
            get {
                switch (_type) {
                    case Types.NegativeFixNum:
                        return (long)(Value | 0xfffffffffffffff0UL);
                    case Types.Int8:
                        return (long)(Value | 0xffffffffffffff00UL);
                    case Types.Int16:
                        return (long)(Value | 0xffffffffffff0000UL);
                    case Types.Int32:
                        return (long)(Value | 0xffffffff00000000UL);
                    case Types.Int64:
                    default:
                        return (long)Value;
                }
            }
        }

        /// <summary>
        /// Last boolean read.
        /// </summary>
        public bool Boolean => Value != 0;

        /// <summary>
        /// Last floating point value read.
        /// </summary>
        public double Double {
            get; private set;
        }

        /// <summary>
        /// Or whether the value was nil
        /// </summary>
        public bool IsNil => _type == Types.Nil;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="owner"></param>
        public Reader(Stream strm, bool owner = false) {
            _strm = strm;
            _owner = owner;
        }

        /// <summary>
        /// Whether coming is an array
        /// </summary>
        public bool IsArray => _type == Types.FixArray ||
            _type == Types.Array16 ||
            _type == Types.Array32;

        /// <summary>
        /// Type safe read of array length
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<int> ReadArrayLengthAsync(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsArray) {
                throw new FormatException("Not an array or object");
            }
            return (int)Value;
        }

        /// <summary>
        /// Whether coming is a map
        /// </summary>
        public bool IsMap => _type == Types.FixMap ||
            _type == Types.Map16 ||
            _type == Types.Map32;

        /// <summary>
        /// Type safe read of map length
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<int> ReadMapLengthAsync(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsMap) {
                throw new FormatException("Not a map or object");
            }
            return (int)Value;
        }

        /// <summary>
        /// Finds first object header - array format is the only supported format.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<int> ReadObjectHeaderAsync(CancellationToken ct) {
            var fields = 0;
            while (true) {
                while (true) {
                    await ReadAsync(ct).ConfigureAwait(false);
                    if (IsArray) {
                        break;
                    }
                    if (_strict) {
                        throw new FormatException(
                            "Expected a length header for object header");
                    }
                    continue;
                }
                fields = (int)Value;
                if (fields == 0) {
                    if (_strict) {
                        throw new FormatException(
                            "0 sized array is not an object");
                    }
                    continue;
                }
                // Read null member value
                await ReadAsync(ct).ConfigureAwait(false);
                --fields;
                if (IsNil) {
                    break; // Done...
                }
                if (_strict) {
                    throw new FormatException(
                        "Missing nil field in object header.");
                }
                continue;
            }
            // Caught up to first object in stream, strict reads going forward
            _strict = true;
            return fields;
        }

        /// <summary>
        /// Whether coming is double
        /// </summary>
        public bool IsDouble => _type == Types.Double ||
                    _type == Types.Float;

        /// <summary>
        /// Typesafe read of double
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<double> ReadDoubleAsync(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsDouble) {
                throw new FormatException("Not float");
            }
            return Double;
        }

        /// <summary>
        /// Typesafe read of float
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<float> ReadFloatAsync(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsDouble) {
                throw new FormatException("Not float");
            }
            if (Double < float.MinValue || Double > float.MaxValue) {
                throw new FormatException("float overflow");
            }
            return (float)Double;
        }

        /// <summary>
        /// Last read value is boolean
        /// </summary>
        public bool IsBoolean =>
            _type == Types.True || _type == Types.False;

        /// <summary>
        /// Type safe read of bool
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<bool> ReadBoolAsync(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsBoolean) {
                throw new FormatException();
            }
            return Boolean;
        }

        /// <summary>
        /// Whether next value is unsigned
        /// </summary>
        public bool IsUnsigned => _type == Types.PositiveFixNum ||
            _type == Types.Uint8 || _type == Types.UInt16 ||
            _type == Types.UInt32 || _type == Types.UInt64;

        /// <summary>
        /// Type safe read of byte
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<byte> ReadUInt8Async(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsUnsigned) {
                throw new FormatException("Not unsigned integer");
            }
            if (Value > byte.MaxValue) {
                throw new FormatException("byte overflow");
            }
            return (byte)Value;
        }

        /// <summary>
        /// Type safe read of ushort
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<ushort> ReadUInt16Async(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsUnsigned) {
                throw new FormatException("Not unsigned integer");
            }
            if (Value > ushort.MaxValue) {
                throw new FormatException("ushort overflow");
            }
            return (ushort)Value;
        }

        /// <summary>
        /// Type safe read of char
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<char> ReadCharAsync(CancellationToken ct) =>
            (char)await ReadUInt16Async(ct).ConfigureAwait(false);


        /// <summary>
        /// Typesafe read of uint
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<uint> ReadUInt32Async(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsUnsigned) {
                throw new FormatException("Not unsigned integer");
            }
            if (Value > uint.MaxValue) {
                throw new FormatException("uint overflow");
            }
            return (uint)Value;
        }

        /// <summary>
        /// Typesafe read of ulong
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<ulong> ReadUInt64Async(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsUnsigned) {
                throw new FormatException("Not unsigned integer");
            }
            return (ulong)Value;
        }

        /// <summary>
        /// Whether value is signed
        /// </summary>
        public bool IsSigned => _type == Types.NegativeFixNum ||
                    _type == Types.PositiveFixNum ||
                    _type == Types.Int8 ||
                    _type == Types.Int16 ||
                    _type == Types.Int32 ||
                    _type == Types.Int64;

        /// <summary>
        /// Type safe read of sbyte
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<sbyte> ReadInt8Async(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsSigned && !IsUnsigned) {
                throw new FormatException("Not integer");
            }
            var signed = Signed;
            if (signed < short.MinValue || signed > short.MaxValue) {
                throw new FormatException("sbyte overflow");
            }
            return (sbyte)signed;
        }

        /// <summary>
        /// Type safe read of short
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<short> ReadInt16Async(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsSigned && !IsUnsigned) {
                throw new FormatException("Not integer");
            }
            var signed = Signed;
            if (signed < short.MinValue || signed > short.MaxValue) {
                throw new FormatException("short overflow");
            }
            return (short)signed;
        }

        /// <summary>
        /// Typesafe read of int
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<int> ReadInt32Async(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsSigned && !IsUnsigned) {
                throw new FormatException("Not integer");
            }
            long signed = (short)Signed;
            if (signed < short.MinValue || signed > short.MaxValue) {
                throw new FormatException("sbyte overflow");
            }
            return (int)signed;
        }

        /// <summary>
        /// Typesafe read of long
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<long> ReadInt64Async(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsSigned && !IsUnsigned) {
                throw new FormatException("Not integer");
            }
            return Signed;
        }

        /// <summary>
        /// Whether following is string
        /// </summary>
        public bool IsBin => _type == Types.Bin8 ||
                    _type == Types.Bin16 ||
                    _type == Types.Bin32;

        /// <summary>
        /// Read buffer
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<byte[]> ReadBinAsync(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsBin) {
                throw new FormatException("Not raw");
            }
            var len = (int)Value;
            var tmp = new byte[len];
            if (Value == 0) {
                return tmp;
            }
            await ReadAsync(tmp, len, ct).ConfigureAwait(false);
            return tmp;
        }

        /// <summary>
        /// Whether following is string
        /// </summary>
        public bool IsStr => _type == Types.FixStr ||
                    _type == Types.Str8 ||
                    _type == Types.Str16 ||
                    _type == Types.Str32;

        /// <summary>
        /// Read string
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<string> ReadStringAsync(CancellationToken ct) {
            await ReadAsync(ct).ConfigureAwait(false);
            if (!IsStr) {
                throw new FormatException("Not a string");
            }
            if (Value == 0) {
                return string.Empty;
            }
            var len = (int)Value;
            byte[] tmp;
            if (len < _buffer.Length) {
                tmp = _buffer;
            }
            else {
                tmp = new byte[len];
            }
            await ReadAsync(tmp, len, ct).ConfigureAwait(false);
            return _encoding.GetString(tmp, 0, len);
        }


        /// <summary>
        /// Reads header and value if not raw or array/map
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task ReadAsync(CancellationToken ct) {
            // Read header
            await ReadAsync(_buffer, 1, ct).ConfigureAwait(false);
            if (_buffer[0] >= 0x00 && _buffer[0] <= 0x7f) {
                _type = Types.PositiveFixNum;
                Value = (uint)_buffer[0] & 0x7f;
                return;
            }

            if (_buffer[0] >= 0xe0 && _buffer[0] <= 0xff) {
                _type = Types.NegativeFixNum;
                Value = (uint)(_buffer[0] & 0x1f) - 0x20;
                return;
            }

            if (_buffer[0] >= 0xa0 && _buffer[0] <= 0xbf) {
                _type = Types.FixStr;
                Value = (uint)(_buffer[0] & 0x1f);
                return;
            }

            if (_buffer[0] >= 0x90 && _buffer[0] <= 0x9f) {
                _type = Types.FixArray;
                Value = (uint)(_buffer[0] & 0xf);
                return;
            }

            if (_buffer[0] >= 0x80 && _buffer[0] <= 0x8f) {
                _type = Types.FixMap;
                Value = (uint)(_buffer[0] & 0xf);
                return;
            }

            _type = (Types)_buffer[0];
            switch (_type) { // big-endian
                case Types.Float:
                    await ReadAsync(_buffer, 4, ct).ConfigureAwait(false);
                    if (BitConverter.IsLittleEndian) {
                            _buffer[ 4] = _buffer[ 3];
                            _buffer[ 5] = _buffer[ 2];
                            _buffer[ 6] = _buffer[ 1];
                            _buffer[ 7] = _buffer[ 0];
                        Double =
                            BitConverter.ToSingle(_buffer, 4);
                    }
                    else {
                        Double =
                            BitConverter.ToSingle(_buffer, 0);
                    }
                    break;
                case Types.Double:
                    await ReadAsync(_buffer, 8, ct).ConfigureAwait(false);
                    if (BitConverter.IsLittleEndian) {
                            _buffer[ 8] = _buffer[ 7];
                            _buffer[ 9] = _buffer[ 6];
                            _buffer[10] = _buffer[ 5];
                            _buffer[11] = _buffer[ 4];
                            _buffer[12] = _buffer[ 3];
                            _buffer[13] = _buffer[ 2];
                            _buffer[14] = _buffer[ 1];
                            _buffer[15] = _buffer[ 0];
                        Double =
                            BitConverter.ToDouble(_buffer, 8);
                    }
                    else {
                        Double =
                            BitConverter.ToDouble(_buffer, 0);
                    }
                    break;
                case Types.Nil:
                    break;
                case Types.False:
                    Value = 0;
                    break;
                case Types.True:
                    Value = 1;
                    break;
                case Types.Int8:
                case Types.Bin8:
                case Types.Str8:
                case Types.Uint8:
                    await ReadAsync(_buffer, 1, ct).ConfigureAwait(false);
                    Value = _buffer[0];
                    break;
                case Types.Bin16:
                case Types.Str16:
                case Types.Array16:
                case Types.Map16:
                case Types.Int16:
                case Types.UInt16:
                    await ReadAsync(_buffer, 2, ct).ConfigureAwait(false);
                    if (BitConverter.IsLittleEndian) {
                        Value =
                            ((uint)_buffer[0] << 8) |
                            ((uint)_buffer[1]);
                    }
                    else {
                        Value =
                            ((uint)_buffer[1] << 8) |
                            ((uint)_buffer[0]);
                    }
                    break;
                case Types.Bin32:
                case Types.Str32:
                case Types.Array32:
                case Types.Map32:
                case Types.Int32:
                case Types.UInt32:
                    await ReadAsync(_buffer, 4, ct).ConfigureAwait(false);
                    if (BitConverter.IsLittleEndian) {
                        Value =
                            ((uint)_buffer[0] << 24) |
                            ((uint)_buffer[1] << 16) |
                            ((uint)_buffer[2] << 8) |
                            ((uint)_buffer[3]);
                    }
                    else {
                        Value =
                            ((uint)_buffer[3] << 24) |
                            ((uint)_buffer[2] << 16) |
                            ((uint)_buffer[1] << 8) |
                            ((uint)_buffer[0]);
                    }
                    break;
                case Types.Int64:
                case Types.UInt64:
                    await ReadAsync(_buffer, 8, ct).ConfigureAwait(false);
                    if (BitConverter.IsLittleEndian) {
                        Value =
                            ((ulong)_buffer[0] << 56) |
                            ((ulong)_buffer[1] << 48) |
                            ((ulong)_buffer[2] << 40) |
                            ((ulong)_buffer[3] << 32) |
                            ((ulong)_buffer[4] << 24) |
                            ((ulong)_buffer[5] << 16) |
                            ((ulong)_buffer[6] << 8) |
                            ((ulong)_buffer[7]);
                    }
                    else {
                        Value =
                            ((ulong)_buffer[7] << 56) |
                            ((ulong)_buffer[6] << 48) |
                            ((ulong)_buffer[5] << 40) |
                            ((ulong)_buffer[4] << 32) |
                            ((ulong)_buffer[3] << 24) |
                            ((ulong)_buffer[2] << 16) |
                            ((ulong)_buffer[1] << 8) |
                            ((ulong)_buffer[0]);
                    }
                    break;
            }
        }

        /// <summary>
        /// Helper to read len bytes from stream
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="len"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task ReadAsync(byte[] buf, int len, CancellationToken ct) {
            var read = 0;
            while (read != len) {
                read += await _strm.ReadAsync(
                    buf, read, len - read, ct).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (_owner) {
                ((IDisposable)_strm).Dispose();
            }
        }

        private Types _type;
        private Stream _strm;
        private bool _owner;
        private bool _strict = false;
        private Encoding _encoding = Encoding.UTF8;
        private byte[] _buffer = new byte[64];
    }
}
