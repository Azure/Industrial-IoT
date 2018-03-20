// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {

    public static class DateTimeEx {

        /// <summary>
        /// Convert time to iso string
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public static string ToIso8601String(this DateTime datetime) {
            return datetime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK");
        }
    }
}
