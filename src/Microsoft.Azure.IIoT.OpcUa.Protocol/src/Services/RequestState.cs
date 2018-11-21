// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Opc.Ua;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;

    /// <summary>
    /// Simple implementation of request manager
    /// </summary>
    public class RequestState : IDisposable {

        /// <summary>
        /// Frees any unmanaged resources.
        /// </summary>
        public void Dispose() {
            var operations = _requests.Values.ToList();
            _requests.Clear();
            foreach (var operation in operations) {
                operation.SetStatusCode(StatusCodes.BadSessionClosed);
            }
        }

        /// <summary>
        /// Called when a new request arrives.
        /// </summary>
        /// <param name="context"></param>
        public void RequestReceived(RequestContextModel context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            _requests.TryAdd(context.RequestId, context);
        }

        /// <summary>
        /// Called when a request completes (normally or abnormally).
        /// </summary>
        public void RequestCompleted(RequestContextModel context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            // remove the request.
            _requests.TryRemove(context.RequestId, out context);
        }

        /// <summary>
        /// Called when the client wishes to cancel one or more requests.
        /// </summary>
        public void CancelRequests(uint requestHandle, out uint cancelCount) {
            cancelCount = 0;
            // flag requests as cancelled.
            foreach (var request in _requests.Values.ToList()) {
                if (request.ClientHandle == requestHandle) {
                    request.SetStatusCode(StatusCodes.BadRequestCancelledByRequest);
                    cancelCount++;
                }
            }
        }

        private readonly ConcurrentDictionary<uint, RequestContextModel> _requests =
            new ConcurrentDictionary<uint, RequestContextModel>();
    }
}
