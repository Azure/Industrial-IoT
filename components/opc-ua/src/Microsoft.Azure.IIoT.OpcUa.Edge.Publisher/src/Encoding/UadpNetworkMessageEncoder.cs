// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Encoding {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Binary message encoder
    /// </summary>
    public class UadpNetworkMessageEncoder : IMessageEncoder {

        /// <inheritdoc/>
        public Task<EncodedMessage> Encode(IMessageData message) {
            // TODO
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IMessageData> Decode(string encodedMessage) {
            // TODO
            throw new NotImplementedException();
        }
    }
}