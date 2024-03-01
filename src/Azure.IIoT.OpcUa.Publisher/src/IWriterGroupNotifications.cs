// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Receives change notifications
    /// </summary>
    public interface IWriterGroupNotifications
    {
        /// <summary>
        /// Recevie change
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <returns></returns>
        public ValueTask OnUpdatedAsync(WriterGroupModel writerGroup);

        /// <summary>
        /// Recevie change
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <returns></returns>
        public ValueTask OnRemovedAsync(WriterGroupModel writerGroup);
    }
}
