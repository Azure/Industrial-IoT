// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Serializers {
    using global::MessagePack;

    /// <summary>
    /// Message pack serializer options provider
    /// </summary>
    public interface IMessagePackSerializerOptionsProvider {

#if MessagePack2
        /// <summary>
        /// Serializer options
        /// </summary>
        MessagePackSerializerOptions Options { get; }
#endif
    }
}
