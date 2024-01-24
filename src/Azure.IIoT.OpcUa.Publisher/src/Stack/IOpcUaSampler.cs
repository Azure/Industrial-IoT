// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Opc.Ua;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents value changes sampled
    /// </summary>
    /// <param name="Value"></param>
    /// <param name="SequenceNumber"></param>
    public record struct DataValueChange(DataValue Value, uint SequenceNumber);

    /// <summary>
    /// Creates a sampler that allows sampling node values
    /// </summary>
    public interface IOpcUaSampler
    {
        /// <summary>
        /// Called when a value changes
        /// </summary>
        event EventHandler<DataValueChange>? OnValueChange;

        /// <summary>
        /// Close the sampler
        /// </summary>
        /// <returns></returns>
        ValueTask CloseAsync();
    }
}
