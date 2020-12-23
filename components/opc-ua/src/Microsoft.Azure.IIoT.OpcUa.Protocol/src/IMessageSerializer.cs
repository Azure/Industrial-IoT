// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Opc.Ua;
    using System.IO;

    /// <summary>
    /// Message serializer services
    /// </summary>
    public interface IMessageSerializer {

        /// <summary>
        /// Decode bytes into message
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        IEncodeable Decode(string contentType,
            Stream stream);

        /// <summary>
        /// Encode message to bytes
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        void Encode(string contentType,
            Stream stream, IEncodeable message);
    }
}
