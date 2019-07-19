// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Xml {

    /// <summary>
    /// Helper extensions for qualified name
    /// </summary>
    public static class XmlQualifiedNameEx {

        /// <summary>
        /// Returns whether the name is null or empty string
        /// </summary>
        /// <param name="qname"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this XmlQualifiedName qname) {
            return qname == null || string.IsNullOrEmpty(qname.Name);
        }

        /// <summary>
        /// Checks whether the qname is valid
        /// </summary>
        /// <param name="qname"></param>
        /// <returns></returns>
        public static bool IsValid(this XmlQualifiedName qname) {
            return !qname.IsNullOrEmpty() && !string.IsNullOrEmpty(qname.Namespace) &&
                IsValidName(qname.Name);
        }

        /// <summary>
        /// Checks if a string is a valid part of a qname.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static bool IsValidName(string name) {
            if (string.IsNullOrEmpty(name)) {
                return false;
            }
            if (!char.IsLetter(name[0]) && name[0] != '_') {
                return false;
            }
            foreach (var c in name) {
                if (char.IsLetter(c) || char.IsDigit(c)) {
                    continue;
                }
                if (c == '.' || c == '-' || c == '_') {
                    continue;
                }
                return false;
            }
            return true;
        }
    }
}
