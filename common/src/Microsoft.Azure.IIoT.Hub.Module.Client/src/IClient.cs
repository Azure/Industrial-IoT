// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The messaging client
    /// </summary>
    public interface IClient : IDisposable {

        /// <summary>
        /// Maximum size message client can process
        /// </summary>
        int MaxMessageSize { get; }

        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <param name="message">The message containing the event.</param>
        Task SendEventAsync(Message message);

        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <param name="outputName">The output target for sending the given message.</param>
        /// <param name="message">The message containing the event.</param>
        /// <returns></returns>
        Task SendEventAsync(string outputName, Message message);

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        Task SendEventBatchAsync(IEnumerable<Message> messages);

        /// <summary>
        /// Sends a batch of events to device hub on a specific output
        /// </summary>
        Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages);

        /// <summary>
        /// Registers a new delegate that is called for a method that
        /// doesn't have a delegate registered for its name.
        /// If a default delegate is already registered it will replace
        /// with the new delegate.
        /// </summary>
        /// <param name="methodHandler">The delegate to be used when
        /// a method is called by the cloud service and there is no
        /// delegate registered for that method name.</param>
        /// <param name="userContext">Generic parameter to be interpreted
        /// by the client code.</param>
        Task SetMethodDefaultHandlerAsync(
            MethodCallback methodHandler, object userContext);

        /// <summary>
        /// Registers a new delegate for the named method. If a delegate
        /// is already associated with the named method, it will be replaced
        /// with the new delegate.
        /// <param name="methodName">The name of the method to associate
        /// with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a
        /// method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted
        /// by the client code.</param>
        /// </summary>
        Task SetMethodHandlerAsync(string methodName,
            MethodCallback methodHandler, object userContext);

        /// <summary>
        /// Retrieve a device twin object for the current device.
        /// </summary>
        /// <returns>The device twin object for the current
        /// device</returns>
        Task<Twin> GetTwinAsync();

        /// <summary>
        /// Set a callback that will be called whenever the client
        /// receives a state update (desired or reported) from the service.
        /// This has the side-effect of subscribing to the PATCH topic on
        /// the service.
        /// </summary>
        /// <param name="callback">Callback to call after the state
        /// update has been received and applied</param>
        /// <param name="userContext">Context object that will be
        /// passed into callback</param>
        Task SetDesiredPropertyUpdateCallbackAsync(
            DesiredPropertyUpdateCallback callback, object userContext);

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties
        /// to push</param>
        Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties);

        /// <summary>
        /// Upload to blob
        /// </summary>
        /// <param name="blobName"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        Task UploadToBlobAsync(string blobName, Stream source);

        /// <summary>
        /// Interactively invokes a method on module
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="methodRequest"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
            MethodRequest methodRequest,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Interactively invokes a method on a device.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="methodRequest"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MethodResponse> InvokeMethodAsync(string deviceId,
            MethodRequest methodRequest,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Close the DeviceClient instance
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
    }

    /// <summary>
    /// Stream callback definition
    /// </summary>
    /// <param name="userContext"></param>
    public delegate void StreamCallback(object userContext);
}
