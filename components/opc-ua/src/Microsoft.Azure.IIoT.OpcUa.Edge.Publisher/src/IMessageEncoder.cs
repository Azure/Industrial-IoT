// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System.Threading.Tasks;

    /// <summary>
    /// Encoder to encode or decode opc ua telemetry messages
    /// </summary>
    public interface IMessageEncoder {

        /// <summary>
        /// Encodes the message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<EncodedMessage> Encode(IMessageData message);

        /// <summary>
        /// Decodes the message
        /// </summary>
        /// <param name="encodedMessage"></param>
        /// <returns></returns>
        Task<IMessageData> Decode(string encodedMessage);
    }
}