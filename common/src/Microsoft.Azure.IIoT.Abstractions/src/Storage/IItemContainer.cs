// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {

    /// <summary>
    /// A container of items
    /// </summary>
    public interface IItemContainer {

        /// <summary>
        /// Name of the container
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Presents the items as documents
        /// </summary>
        /// <exception cref="System.NotSupportedException" />
        /// <returns></returns>
        IDocuments AsDocuments();
    }
}
