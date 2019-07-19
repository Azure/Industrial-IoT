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

namespace Opc.Ua.Types {
    using System.IO;
    using System;
    using Opc.Ua.Types.Resolver;

    /// <summary>
    /// Type dictionary loader
    /// </summary>
    public static class Types {

        /// <summary>
        /// Standard types
        /// </summary>
        public static ITypeDictionary Standard { get; } = LoadStandardTypes();

        /// <summary>
        /// Load dictionary
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="resolver"></param>
        public static ITypeDictionary Load(Stream stream, ITypeResolver resolver = null) {
            return new DataTypeDictionary(stream.DeserializeFromXml<Schema.TypeDictionary>(),
                resolver ?? new CompositeDictionaryResolver());
        }

        /// <summary>
        /// Load dictionary
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="resolver"></param>
        public static ITypeDictionary Load(string fileName, ITypeResolver resolver = null) {
            using (var stream = File.OpenRead(fileName)) {
                return Load(stream, resolver);
            }
        }

        /// <summary>
        /// Load the standard types from the included resources
        /// </summary>
        /// <returns></returns>
        private static ITypeDictionary LoadStandardTypes() {
            var assembly = typeof(DataTypeDictionary).Assembly;
            var prefix = assembly.GetName().Name;
            var builtIn = new DataTypeDictionary(assembly
                .DeserializeFromXmlManifestResource<Schema.TypeDictionary>(
                    prefix + ".Stack.Types.BuiltIn.BuiltInTypes.xml"), null);
            return new DataTypeDictionary(assembly
                .DeserializeFromXmlManifestResource<Schema.TypeDictionary>(
                    prefix + ".Stack.Types.BuiltIn.UA Core Services.xml"), builtIn);
        }
    }
}
