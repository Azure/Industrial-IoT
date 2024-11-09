/* ========================================================================
 * Copyright (c) 2005-2022 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client.ComplexTypes
{
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Context for complex type sub system
    /// </summary>
    public interface IComplexTypeContext
    {
        /// <summary>
        /// Logger of the session
        /// </summary>
        ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the table of namespace uris known to the server.
        /// </summary>
        NamespaceTable NamespaceUris { get; }

        /// <summary>
        /// Gets the factory used to create encodeable objects that the server understands.
        /// </summary>
        IEncodeableFactory Factory { get; }

        /// <summary>
        /// Gets the cache of nodes fetched from the server.
        /// </summary>
        INodeCache NodeCache { get; }

        /// <summary>
        /// Reads the value for a node.
        /// </summary>
        /// <param name="nodeId">The node Id.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        Task<DataValue> ReadValueAsync(NodeId nodeId, CancellationToken ct = default);

        /// <summary>
        /// Reads the values for a node collection. Returns diagnostic errors.
        /// </summary>
        /// <param name="nodeIds">The node Id.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        Task<(DataValueCollection, IList<ServiceResult>)> ReadValuesAsync(
            IList<NodeId> nodeIds, CancellationToken ct = default);
    }
}
