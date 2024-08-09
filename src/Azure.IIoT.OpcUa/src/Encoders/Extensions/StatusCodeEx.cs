// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua
{
    using Azure.IIoT.OpcUa.Encoders.Utils;

    /// <summary>
    /// Status code extensions
    /// </summary>
    public static class StatusCodeEx
    {
        /// <summary>
        /// Get symbolic name - fast
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string AsString(this StatusCode code)
        {
            if (!TypeMaps.StatusCodes.Value.TryGetBrowseName(code.CodeBits, out var name))
            {
                return string.Empty;
            }
            return name;
        }

        /// <summary>
        /// Get symbolic name - fast
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string? AsString(this StatusCode? code)
        {
            return code?.AsString();
        }
    }
}
