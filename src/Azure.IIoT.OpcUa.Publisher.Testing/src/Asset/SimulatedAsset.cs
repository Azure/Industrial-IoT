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
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    public sealed class SimulatedAsset : IAsset
    {
        public ServiceResult Read(AssetTag tag, ref object? value)
        {
            ArgumentNullException.ThrowIfNull(tag.Address);
            value = _values.AddOrUpdate(tag.Address, 0.0f, (_, v) => v + 1.0f);
            return ServiceResult.Good;
        }

        public ServiceResult Write(AssetTag tag, ref object value)
        {
            ArgumentNullException.ThrowIfNull(tag.Address);
            var update = value as float?;
            _values.AddOrUpdate(tag.Address, update ?? 0.0f, (_, v) => update ?? 0.0f);
            return ServiceResult.Good;
        }

        public void Dispose()
        {
            // Nothing to do
        }

        private readonly ConcurrentDictionary<Uri, float> _values = new ();
    }
}
