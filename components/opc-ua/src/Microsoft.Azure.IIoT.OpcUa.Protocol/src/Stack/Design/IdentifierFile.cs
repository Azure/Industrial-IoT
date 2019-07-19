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

namespace Opc.Ua.Design {
    using Opc.Ua.Design.Schema;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    /// <summary>
    /// Generates files used to describe data types.
    /// </summary>
    internal sealed class IdentifierFile : INodeIdAssigner {

        /// <inheritdoc/>
        public string Namespace { get; }

        /// <inheritdoc/>
        public string Version { get; }

        /// <summary>
        /// Create identifier file
        /// </summary>
        public IdentifierFile(Stream stream, string ns, string version) {
            _identifiers = ParseCsvFile(stream);
            _assigned = new HashSet<object>(_identifiers.Values);
            if (_assigned.Count != _identifiers.Count) {
                throw new FormatException("Duplicate identifiers in identifier file");
            }
            Namespace = ns;
            Version = version;
        }

        /// <inheritdoc/>
        public object TryAssignId(Namespace ns, XmlQualifiedName symbolicId) {
            //
            // We can only try and assign if we own the namespace.
            //
            if (Namespace == ns.Value && (Version == null || Version == ns.Version)) {
                if (_identifiers.TryGetValue(symbolicId.Name, out var id)) {
                    return id;
                }
                return FindUnusedId();
            }
            return null;
        }


        /// <summary>
        /// Select an unused id
        /// </summary>
        /// <returns></returns>
        private uint FindUnusedId() {
            var id = ushort.MaxValue;
            while (_assigned.Contains(id)) {
                --id;
                if (id == 0) {
                    break;
                }
            }
            _assigned.Add(id);
            return id;
        }

        /// <summary>
        /// Parse csv identifier file
        /// </summary>
        /// <param name="istrm"></param>
        /// <returns></returns>
        private static Dictionary<string, object> ParseCsvFile(Stream istrm) {
            var identifiers = new Dictionary<string, object>();
            var reader = new StreamReader(istrm);
            try {
                while (!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line) ||
                        line.StartsWith("#", StringComparison.Ordinal)) {
                        continue;
                    }
                    var index = line.IndexOf(',');
                    if (index == -1) {
                        continue;
                    }
                    // remove the node class if it is present.
                    var lastIndex = line.LastIndexOf(',');
                    if (lastIndex != -1 && index != lastIndex) {
                        line = line.Substring(0, lastIndex);
                    }
                    try {
                        var name = line.Substring(0, index).Trim();
                        var id = line.Substring(index + 1).Trim();
                        if (id.StartsWith("\"", StringComparison.Ordinal)) {
                            identifiers[name] = id.Substring(1, id.Length - 2);
                        }
                        else {
                            var numericId = Convert.ToUInt32(id);
                            identifiers[name] = numericId;
                        }
                    }
                    catch (Exception) {
                        continue;
                    }
                }
            }
            finally {
                reader.Close();
            }
            return identifiers;
        }

        private readonly Dictionary<string, object> _identifiers =
            new Dictionary<string, object>();
        private readonly HashSet<object> _assigned;
    }
}
