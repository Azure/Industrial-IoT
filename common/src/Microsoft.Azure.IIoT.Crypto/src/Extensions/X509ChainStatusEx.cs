// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace System.Security.Cryptography.X509Certificates {
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Status extensions
    /// </summary>
    public static class X509ChainStatusEx {

        /// <summary>
        /// Convert to string
        /// </summary>
        /// <param name="status"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string AsString(this IEnumerable<X509ChainStatus> status,
            string message = null) {
            var buffer = new StringBuilder();
            if (message != null) {
                buffer.AppendLine(message);
            }
            if (status != null) {
                foreach (var stat in status) {
                    buffer.Append("   ").AppendLine(stat.StatusInformation);
                }
            }
            return buffer.ToString();
        }
    }
}
