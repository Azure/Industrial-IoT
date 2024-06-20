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
    /// Represents changes in the address space
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="Source"></param>
    /// <param name="PathFromRoot"></param>
    /// <param name="PreviousItem"></param>
    /// <param name="ChangedItem"></param>
    /// <param name="SequenceNumber"></param>
    /// <param name="Timestamp"></param>
    public record struct Change<T>(NodeId Source, RelativePath PathFromRoot,
        T? PreviousItem, T? ChangedItem, uint SequenceNumber,
        DateTimeOffset Timestamp) where T : class;

    /// <summary>
    /// This is an abstraction over a continous monitored address space
    /// inside a server.
    /// </summary>
    public interface IOpcUaBrowser
    {
        /// <summary>
        /// Called when a node changes
        /// </summary>
        event EventHandler<Change<Node>>? OnNodeChange;

        /// <summary>
        /// Called when a reference changes
        /// </summary>
        event EventHandler<Change<ReferenceDescription>>? OnReferenceChange;

        /// <summary>
        /// Trigger a rebrowsing of the address space
        /// </summary>
        void Rebrowse();

        /// <summary>
        /// Close the browser
        /// </summary>
        /// <returns></returns>
        ValueTask CloseAsync();
    }
}
