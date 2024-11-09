/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
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

namespace Opc.Ua.Client
{
    using System;

    /// <summary>
    /// Flags indicating what has changed in a publish state change.
    /// </summary>
    [Flags]
    public enum PublishStateChangedMask
    {
        /// <summary>
        /// The publish state has not changed.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// The publishing stopped.
        /// </summary>
        Stopped = 0x01,

        /// <summary>
        /// The publishing recovered.
        /// </summary>
        Recovered = 0x02,

        /// <summary>
        /// A keep alive message was received.
        /// </summary>
        KeepAlive = 0x04,

        /// <summary>
        /// A republish for a missing message was issued.
        /// </summary>
        Republish = 0x08,

        /// <summary>
        /// The publishing was transferred to another node.
        /// </summary>
        Transferred = 0x10,

        /// <summary>
        /// The publishing was timed out
        /// </summary>
        Timeout = 0x20,
    }
}
