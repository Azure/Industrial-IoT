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

namespace Opc.Ua.Types.Resolver {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Uses the file system and internal resources to resolve imports
    /// </summary>
    public sealed class FileSystemDictionaryResolver : ITypeResolver {

        /// <summary>
        /// Create resolver with paths to use for loading
        /// </summary>
        /// <param name="paths"></param>
        public FileSystemDictionaryResolver(List<string> paths = null) {
            _paths = paths?.Select(p => new FileInfo(p).DirectoryName).ToList() ??
                new List<string> { Directory.GetCurrentDirectory() };
        }

        /// <inheritdoc/>
        public Schema.DataType TryResolve(Schema.ImportDirective import,
            XmlQualifiedName typeName) {
            if (import == null) {
                throw new ArgumentNullException(nameof(import));
            }
            if (typeName.IsNullOrEmpty()) {
                throw new ArgumentNullException(nameof(typeName));
            }
            if (!_cache.TryGetValue(import, out var types)) {
                import = import.Copy();
                types = Load(import);
                if (types == null) {
                    // Try any version
                    import.TargetVersion = null;
                    types = Load(import);
                    if (types == null) {
                        return null;
                    }
                }
                _cache.Add(import, types);
            }
            return types.TryResolve(import, typeName);
        }

        /// <summary>
        /// Try loading
        /// </summary>
        /// <param name="import"></param>
        /// <param name="fileName"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        private bool TryLoad(Schema.ImportDirective import, string fileName,
            out ITypeResolver types) {
            types = null;
            if (!File.Exists(fileName)) {
                return false;
            }
            try {
                var dictionary = Types.Load(fileName, this);

                // TODO: This could be made more lightweight by loading just
                // the dictionary first and then loading all.

                // verify namespace.
                if (!string.IsNullOrEmpty(import.Namespace) &&
                    import.Namespace != dictionary.TargetNamespace) {
                    return false;
                }
                // verify version.
                if (!string.IsNullOrEmpty(import.TargetVersion) &&
                    import.TargetVersion != dictionary.TargetVersion) {
                    return false;
                }

                types = dictionary as ITypeResolver;
                return types != null;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Try to load the design from file system
        /// </summary>
        /// <param name="import"></param>
        /// <returns></returns>
        private ITypeResolver Load(Schema.ImportDirective import) {
            if (Path.IsPathRooted(import.Location)) {
                if (TryLoad(import, import.Location, out var model)) {
                    return model;
                }
            }
            foreach (var path in _paths) {
                var filePath = Path.Combine(path, import.Location);
                if (TryLoad(import, filePath, out var model)) {
                    return model;
                }
            }
            return null;
        }

        private readonly List<string> _paths;
        private readonly Dictionary<Schema.ImportDirective, ITypeResolver> _cache =
            new Dictionary<Schema.ImportDirective, ITypeResolver>();
    }
}
