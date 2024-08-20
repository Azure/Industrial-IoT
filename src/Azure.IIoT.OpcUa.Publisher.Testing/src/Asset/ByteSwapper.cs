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
    static class ByteSwapper
    {
        public static byte[] Swap(byte[] value, bool swapPerRegister = false)
        {
            if (value.Length == 2)
            {
                var swappedBytes = new byte[2];
                swappedBytes[0] = value[1];
                swappedBytes[1] = value[0];
                return swappedBytes;
            }

            if (value.Length == 4)
            {
                if (swapPerRegister)
                {
                    var swappedBytes = new byte[4];
                    swappedBytes[2] = value[3];
                    swappedBytes[3] = value[2];
                    swappedBytes[0] = value[1];
                    swappedBytes[1] = value[0];
                    return swappedBytes;
                }
                else
                {
                    var swappedBytes = new byte[4];
                    swappedBytes[0] = value[3];
                    swappedBytes[1] = value[2];
                    swappedBytes[2] = value[1];
                    swappedBytes[3] = value[0];
                    return swappedBytes;
                }
            }

            if (value.Length == 8)
            {
                if (swapPerRegister)
                {
                    var swappedBytes = new byte[8];
                    swappedBytes[6] = value[7];
                    swappedBytes[7] = value[6];
                    swappedBytes[4] = value[5];
                    swappedBytes[5] = value[4];
                    swappedBytes[2] = value[3];
                    swappedBytes[3] = value[2];
                    swappedBytes[0] = value[1];
                    swappedBytes[1] = value[0];
                    return swappedBytes;
                }
                else
                {
                    var swappedBytes = new byte[8];
                    swappedBytes[0] = value[7];
                    swappedBytes[1] = value[6];
                    swappedBytes[2] = value[5];
                    swappedBytes[3] = value[4];
                    swappedBytes[4] = value[3];
                    swappedBytes[5] = value[2];
                    swappedBytes[6] = value[1];
                    swappedBytes[7] = value[0];
                    return swappedBytes;
                }
            }

            // don't swap anything my default
            return value;
        }

        public static ushort Swap(ushort value)
        {
            return (ushort)(((value & 0x00FF) << 8) |
                            ((value & 0xFF00) >> 8));
        }

        public static uint Swap(uint value)
        {
            return ((value & 0x000000FF) << 24) |
                   ((value & 0x0000FF00) << 8) |
                   ((value & 0x00FF0000) >> 8) |
                   ((value & 0xFF000000) >> 24);
        }

        public static ulong Swap(ulong value)
        {
            return ((value & 0x00000000000000FFUL) << 56) |
                   ((value & 0x000000000000FF00UL) << 40) |
                   ((value & 0x0000000000FF0000UL) << 24) |
                   ((value & 0x00000000FF000000UL) << 8) |
                   ((value & 0x000000FF00000000UL) >> 8) |
                   ((value & 0x0000FF0000000000UL) >> 24) |
                   ((value & 0x00FF000000000000UL) >> 40) |
                   ((value & 0xFF00000000000000UL) >> 56);
        }
    }
}
