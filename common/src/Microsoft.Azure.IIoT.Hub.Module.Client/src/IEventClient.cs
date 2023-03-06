// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client
{
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.Devices.Client;
    using System.Threading.Tasks;

    /// <summary>
    /// The messaging client
    /// </summary>
    public interface IEventClient
    {
        /// <summary>
        /// Maximum size body of message client can process
        /// </summary>
        int MaxEventPayloadSizeInBytes { get; }

        /// <summary>
        /// Create a message to send
        /// </summary>
        /// <returns></returns>
        IEvent CreateEvent();
    }
}
