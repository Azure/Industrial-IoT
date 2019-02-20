// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Mock {
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using System;

    /// <summary>
    /// Connection
    /// </summary>
    public interface IIoTHubConnection {

        /// <summary>
        /// Send event to hub
        /// </summary>
        /// <param name="message"></param>
        void SendEvent(Message message);

        /// <summary>
        /// Get twin from hub
        /// </summary>
        /// <returns></returns>
        Twin GetTwin();

        /// <summary>
        /// Update reported properties
        /// </summary>
        /// <param name="reportedProperties"></param>
        void UpdateReportedProperties(TwinCollection reportedProperties);

        /// <summary>
        /// Send blob
        /// </summary>
        /// <param name="blobName"></param>
        /// <param name="arraySegment"></param>
        void SendBlob(string blobName, ArraySegment<byte> arraySegment);

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="methodRequest"></param>
        /// <returns></returns>
        MethodResponse Call(string deviceId, string moduleId,
            MethodRequest methodRequest);

        /// <summary>
        /// Close connection.
        /// </summary>
        void Close();
    }
}
