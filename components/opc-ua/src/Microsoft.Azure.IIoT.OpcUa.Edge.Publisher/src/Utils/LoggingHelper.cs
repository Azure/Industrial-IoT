// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Utils
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Helper class for logging
    /// </summary>
    public static class LoggingHelper {
        /// <summary>
        /// Extracts host in the format "server[:port]" from a uri for simpler logging.
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <returns></returns>
        public static string ExtractHost(string endpointUrl) {
            string pattern = @":\/\/([^\/_]+)";
            var match = Regex.Match(endpointUrl, pattern);

            return match?.Groups[1]?.Value;
        }
    }
}