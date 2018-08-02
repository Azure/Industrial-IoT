// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework {
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IEventEmitter {

        /// <summary>
        /// Device id events are emitted on
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// Module id events are emitted on
        /// </summary>
        string ModuleId { get; }

        /// <summary>
        /// Site events are emitted from
        /// </summary>
        string SiteId { get; }

        /// <summary>
        /// Send event
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        Task SendAsync(byte[] data, string contentType);

        /// <summary>
        /// Send batch of events
        /// </summary>
        /// <param name="batch"></param>
        /// <returns></returns>
        Task SendAsync(IEnumerable<byte[]> batch,
            string contentType);

        /// <summary>
        /// Send property changed notification
        /// </summary>
        /// <param name="propertyId">property id</param>
        /// <param name="value">property value</param>
        /// <returns></returns>
        Task SendAsync(string propertyId, dynamic value);

        /// <summary>
        /// Send property changed notifications
        /// </summary>
        /// <param name="properties">property id</param>
        /// <returns></returns>
        Task SendAsync(IEnumerable<KeyValuePair<string,
            dynamic>> properties);
    }
}
