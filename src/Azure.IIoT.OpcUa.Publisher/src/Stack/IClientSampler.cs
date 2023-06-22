// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Opc.Ua;
    using System;

    /// <summary>
    /// Client sampler
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IClientSampler<T>
    {
        /// <summary>
        /// Registers a callback that will trigger at the specified
        /// sampling rate and executing the read operation.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="samplingRate"></param>
        /// <param name="nodeToRead"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        IAsyncDisposable Sample(T connection, TimeSpan samplingRate,
            ReadValueId nodeToRead, Action<uint, DataValue> callback);
    }
}
