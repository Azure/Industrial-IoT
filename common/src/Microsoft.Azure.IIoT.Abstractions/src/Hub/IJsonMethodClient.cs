// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Json payload method call client as implemented by
    /// IoTHub service clients and IoTEdge Module clients.
    /// </summary>
    public interface IJsonMethodClient {

        /// <summary>
        /// Max payload string size in bytes.
        /// </summary>
        int MaxMethodPayloadCharacterCount { get; }

        /// <summary>
        /// Call a method on a module or device identity with
        /// json payload.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="method"></param>
        /// <param name="json"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        /// <returns>response json payload</returns>
        Task<string> CallMethodAsync(string deviceId, string moduleId,
            string method, string json, TimeSpan? timeout = null,
            CancellationToken ct = default);
    }
}
