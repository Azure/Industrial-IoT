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

namespace Opc.Ua.Client.ComplexTypes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface to access properties of a complex type.
    /// </summary>
    public interface IComplexTypeProperties
    {
        /// <summary>
        /// Get count of properties.
        /// </summary>
        int GetPropertyCount();

        /// <summary>
        /// Get ordered list of property names.
        /// </summary>
        IList<String> GetPropertyNames();

        /// <summary>
        /// Get ordered list of property types.
        /// </summary>
        IList<Type> GetPropertyTypes();

        /// <summary>
        /// Access property values by index.
        /// </summary>
        /// <param name="index"></param>
        object this[int index] { get; set; }

        /// <summary>
        /// Access property values by name.
        /// </summary>
        /// <param name="name"></param>
        object this[string name] { get; set; }

        /// <summary>
        /// Ordered enumerator for properties.
        /// </summary>
        IEnumerable<ComplexTypePropertyInfo> GetPropertyEnumerator();
    }
}
