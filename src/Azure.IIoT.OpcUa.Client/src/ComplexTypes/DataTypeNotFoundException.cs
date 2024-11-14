// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.ComplexTypes
{
    using System;

    /// <summary>
    /// Exception is thrown if the data type is not found.
    /// </summary>
    [Serializable]
    public class DataTypeNotFoundException : Exception
    {
        /// <summary>
        /// The nodeId of the data type.
        /// </summary>
        public ExpandedNodeIdCollection NodeIds { get; }

        /// <summary>
        /// Create the exception.
        /// </summary>
        /// <param name="nodeIds">The collection of nodeId of the data types not found.</param>
        public DataTypeNotFoundException(ExpandedNodeIdCollection nodeIds)
        {
            NodeIds = nodeIds;
        }

        /// <summary>
        /// Create the exception.
        /// </summary>
        /// <param name="nodeIds">The collection of nodeId of the data types not found.</param>
        /// <param name="message">The exception message.</param>
        public DataTypeNotFoundException(ExpandedNodeIdCollection nodeIds, string message)
            : base(message)
        {
            NodeIds = nodeIds;
        }

        /// <summary>
        /// Create the exception.
        /// </summary>
        /// <param name="nodeIds">The collection of nodeId of the data types not found.</param>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The inner exception.</param>
        public DataTypeNotFoundException(ExpandedNodeIdCollection nodeIds, string message, Exception inner)
            : base(message, inner)
        {
            NodeIds = nodeIds;
        }
    }
}
