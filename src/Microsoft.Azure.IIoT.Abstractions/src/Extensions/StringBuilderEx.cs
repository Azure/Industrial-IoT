// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Text {

    /// <summary>
    /// String builder extensions
    /// </summary>
    public static class StringBuilderEx {

        /// <summary>
        /// Append byte buffer
        /// </summary>
        /// <param name="stringBuilder"></param>
        /// <param name="bytes"></param>
        /// <param name="size"></param>
        public static void Append(this StringBuilder stringBuilder, byte[] bytes, int size) {
            var truncate = bytes.Length > size;
            var length = truncate ? size : bytes.Length;
            var ascii = true;
            for (var i = 0; i < length; i++) {
                if (bytes[i] <= 32 || bytes[i] > 127) {
                    ascii = false;
                    break;
                }
            }
            var content = ascii ? Encoding.ASCII.GetString(bytes, 0, length) :
                BitConverter.ToString(bytes, 0, length);
            length = content.IndexOf('\n');
            if (length > 0) {
                stringBuilder.Append(content, 0, length - 1);
            }
            else {
                stringBuilder.Append(content);
            }
        }
    }
}
