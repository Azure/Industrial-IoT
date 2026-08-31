// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc
{
    /// <summary>
    /// Special method names
    /// </summary>
    internal static class MethodNames
    {
        /// <summary>
        /// Used by clients to call chunk
        /// server method.
        /// </summary>
        public const string Call = "$call";

        /// <summary>
        /// Used by clients to call http
        /// tunnel server.
        /// </summary>
        public const string Response = "$response";
    }
}
