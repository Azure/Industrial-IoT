// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {

    /// <summary>
    /// Security mode enumeration extensions
    /// </summary>
    public static class SecurityModeEx {

        /// <summary>
        /// Match security mode to filter
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static bool MatchesFilter(this SecurityMode mode, SecurityMode filter) {
            if (mode == SecurityMode.Best) {
                mode = SecurityMode.SignAndEncrypt;
            }
            if (filter == SecurityMode.Best) {
                filter = SecurityMode.SignAndEncrypt;
            }
            if (filter == mode) {
                return true;
            }
            if (filter == SecurityMode.Sign) {
                if (mode == SecurityMode.SignAndEncrypt) {
                    return true;
                }
            }
            if (filter == SecurityMode.None) {
                return true;
            }
            return false;
        }
    }
}
