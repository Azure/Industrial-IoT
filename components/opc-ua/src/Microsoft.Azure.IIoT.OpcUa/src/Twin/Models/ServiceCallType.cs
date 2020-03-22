// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    /// <summary>
    /// Type of service call
    /// </summary>
    public enum ServiceCallType {

        /// <summary>
        /// Browse service
        /// </summary>
        Browse,

        /// <summary>
        /// Browse next
        /// </summary>
        BrowseNext,

        /// <summary>
        /// Browse by path
        /// </summary>
        BrowsePath,

        /// <summary>
        /// Attribute read
        /// </summary>
        AttributeRead,

        /// <summary>
        /// Attribute write
        /// </summary>
        AttributeWrite,

        /// <summary>
        /// Value read
        /// </summary>
        ValueRead,

        /// <summary>
        /// Value Write
        /// </summary>
        ValueWrite,

        /// <summary>
        /// Method meta data
        /// </summary>
        MethodMetadata,

        /// <summary>
        /// Method call
        /// </summary>
        MethodCall,

        /// <summary>
        /// Publish
        /// </summary>
        PublishStart,

        /// <summary>
        /// Unpublish
        /// </summary>
        PublishStop,

        /// <summary>
        /// History read
        /// </summary>
        HistoryRead,

        /// <summary>
        /// History update
        /// </summary>
        HistoryUpdate
    }
}
