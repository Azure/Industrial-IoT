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

namespace Opc.Ua.Design.Resolver {
    using Opc.Ua.Design.Schema;
    using Opc.Ua.Types.Resolver;
    using Opc.Ua.Types.Schema;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Resolves namespaces using file system or resources
    /// </summary>
    public sealed class FileSystemModelResolver : INodeResolver, INodeIdAssigner,
        ITypeResolver {

        /// <summary>
        /// Create resolver with paths to use for loading
        /// </summary>
        /// <param name="paths"></param>
        public FileSystemModelResolver(List<string> paths = null) {
            _typeResolver = new FileSystemDictionaryResolver(paths);
            _paths = paths?.Select(p => new FileInfo(p).DirectoryName).ToList() ??
                new List<string> { Directory.GetCurrentDirectory() };
        }

        /// <inheritdoc/>
        public object TryAssignId(Namespace ns, XmlQualifiedName symbolicId) {
            if (ns == null) {
                throw new ArgumentNullException(nameof(ns));
            }
            if (symbolicId.IsNullOrEmpty()) {
                throw new ArgumentNullException(nameof(symbolicId));
            }
            if (!_assigners.TryGetValue(ns, out var assigner)) {
                assigner = LoadIdentifiers(ns) as INodeIdAssigner;
                if (assigner == null) {
                    return null;
                }
            }
            return assigner.TryAssignId(ns, symbolicId);
        }

        /// <inheritdoc/>
        public NodeDesign TryResolve(Namespace ns, XmlQualifiedName symbolicId) {
            if (ns == null) {
                throw new ArgumentNullException(nameof(ns));
            }
            if (symbolicId.IsNullOrEmpty()) {
                throw new ArgumentNullException(nameof(symbolicId));
            }
            if (!_resolvers.TryGetValue(ns, out var resolver)) {
                resolver = LoadDesign(ns) as INodeResolver;
                if (resolver == null) {
                    return null;
                }
                _resolvers.Add(ns, resolver);
                if (!_assigners.ContainsKey(ns)) {
                    if (LoadIdentifiers(ns) is INodeIdAssigner assigner) {
                        _assigners.Add(ns, assigner);
                    }
                }
            }
            return resolver.TryResolve(ns, symbolicId);
        }

        /// <inheritdoc/>
        public DataType TryResolve(ImportDirective import,
            XmlQualifiedName typeName) {
            return _typeResolver.TryResolve(import, typeName);
        }

        /// <summary>
        /// Try loading model or types from file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private bool TryLoad(string fileName, out IModelDesign model) {
            model = null;
            if (!File.Exists(fileName)) {
                return false;
            }
            try {
                model = Model.Load(fileName, this);
                return true;
            }
            catch {
                try {
                    model = Model.LoadTypeDictionary(fileName, this);
                    return true;
                }
                catch {
                    return false;
                }
            }
        }

        /// <summary>
        /// Load from file and verify namespace
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="filePath"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private bool TryLoadDesign(Namespace ns, string filePath,
            out IModelDesign model) {
            if (!TryLoad(filePath, out model)) {
                return false;
            }
            // verify namespace.
            if (!string.IsNullOrEmpty(ns.Value) &&
                ns.Value != model.Namespace) {
                return false;
            }
            // verify version.
            if (!string.IsNullOrEmpty(ns.Version) &&
                ns.Version != model.Version) {
                return false;
            }
            if (!(model is INodeResolver)) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Load from file
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="fileName"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool TryLoadIdentifiers(Namespace ns, string fileName,
            out IdentifierFile file) {

            if (!File.Exists(fileName)) {
                file = null;
                return false;
            }
            try {
                using (var stream = File.OpenRead(fileName)) {
                    file = new IdentifierFile(stream, ns.Value, ns.Version);
                    return true;
                }
            }
            catch {
                file = null;
                return false;
            }
        }

        /// <summary>
        /// Try to load the design from file system
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        private IModelDesign LoadDesign(Namespace ns) {
            foreach (var filePath in GetPaths(ns, "xml")) {
                if (TryLoadDesign(ns, filePath, out var model)) {
                    return model;
                }
            }
            return null;
        }

        /// <summary>
        /// Try to load the identifier file from file system
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        private IdentifierFile LoadIdentifiers(Namespace ns) {
            foreach (var filePath in GetPaths(ns, "csv")) {
                if (TryLoadIdentifiers(ns, filePath, out var file)) {
                    return file;
                }
            }
            return null;
        }

        /// <summary>
        /// Try to load the design from file system
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        private IEnumerable<string> GetPaths(Namespace ns, string extension) {
            if (Path.IsPathRooted(ns.FilePath)) {
                yield return Path.ChangeExtension(ns.FilePath, extension);
            }
            foreach (var path in _paths) {
                yield return Path.ChangeExtension(Path.Combine(path, ns.FilePath),
                    extension);
            }
            // Try original file names...
            if (!ns.FilePath.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase)) {
                if (Path.IsPathRooted(ns.FilePath)) {
                    yield return ns.FilePath;
                }
                foreach (var path in _paths) {
                    yield return Path.Combine(path, ns.FilePath);
                }
            }
        }

        private readonly Dictionary<Namespace, INodeResolver> _resolvers =
            new Dictionary<Namespace, INodeResolver>();
        private readonly Dictionary<Namespace, INodeIdAssigner> _assigners =
            new Dictionary<Namespace, INodeIdAssigner>();
        private readonly FileSystemDictionaryResolver _typeResolver;
        private readonly List<string> _paths;
    }
}
