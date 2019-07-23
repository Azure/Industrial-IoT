// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {

    /// <summary>
    /// Document in the document database
    /// </summary>
    public interface IDocumentInfo<T> {

        /// <summary>
        /// Id of the resource
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Value
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Partition key of the value
        /// </summary>
        string PartitionKey { get; }

        /// <summary>
        /// Etag of the document
        /// </summary>
        string Etag { get; }
    }
}
