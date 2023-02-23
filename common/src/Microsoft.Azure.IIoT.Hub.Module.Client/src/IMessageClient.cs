// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client
{
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.IIoT.Messaging;
    using System.Threading.Tasks;

    /// <summary>
    /// The messaging client
    /// </summary>
    public interface IMessageClient
    {
        /// <summary>
        /// Maximum size body of message client can process
        /// </summary>
        int MaxEventBufferSize { get; }

        /// <summary>
        /// Create a message to send
        /// </summary>
        /// <returns></returns>
        ITelemetryEvent CreateTelemetryEvent();

        /// <summary>
        /// Sends messages
        /// </summary>
        /// <param name="message">The message containing the event.</param>
        /// <returns></returns>
        Task SendEventAsync(ITelemetryEvent message);

        /// <summary>
        /// Registers a new delegate that is called for a method that
        /// doesn't have a delegate registered for its name.
        /// If a default delegate is already registered it will replace
        /// with the new delegate.
        /// </summary>
        /// <param name="methodHandler">The delegate to be used when
        /// a method is called by the cloud service and there is no
        /// delegate registered for that method name.</param>
        Task SetMethodHandlerAsync(MethodCallback methodHandler);
    }
}
