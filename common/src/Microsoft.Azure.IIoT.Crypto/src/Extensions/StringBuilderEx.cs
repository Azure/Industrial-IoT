// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Text {

    /// <summary>
    /// String builder extensions
    /// </summary>
    internal static class StringBuilderEx {

        /// <summary>
        /// Add seperator
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="multiLine"></param>
        /// <returns></returns>
        internal static StringBuilder AddSeperator(this StringBuilder buffer,
            bool multiLine) {
            if (buffer.Length > 0) {
                if (multiLine) {
                    return buffer.Append("\r\n");
                }
                return buffer.Append(", ");
            }
            return buffer;
        }
    }
}
