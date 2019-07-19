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

namespace Opc.Ua.Types.Schema {
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// Namespace extension
    /// </summary>
    public partial class ImportDirective {

        /// <summary>
        /// The target version of the import
        /// </summary>
        [XmlIgnore]
        public string TargetVersion { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (obj is ImportDirective ns) {
                return
                    TargetVersion == ns.TargetVersion &&
                    Namespace == ns.Namespace;
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = 834833178;
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(TargetVersion);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Namespace);
            return hashCode;
        }

        /// <inheritdoc/>
        public ImportDirective Copy() {
            return (ImportDirective)MemberwiseClone();
        }

        /// <inheritdoc/>
        public static bool operator ==(ImportDirective imp1, ImportDirective imp2) => EqualityComparer<ImportDirective>.Default.Equals(imp1, imp2);

        /// <inheritdoc/>
        public static bool operator !=(ImportDirective imp1, ImportDirective imp2) => !(imp1 == imp2);
    }
}
