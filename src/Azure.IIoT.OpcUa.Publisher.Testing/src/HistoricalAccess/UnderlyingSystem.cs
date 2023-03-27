/* ========================================================================
 * Copyright (c) 2005-2017 The OPC Foundation, Inc. All rights reserved.
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

namespace HistoricalAccess
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Provides access to the system which stores the data.
    /// </summary>
    public class UnderlyingSystem
    {
        /// <summary>
        /// Constructs a new system.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="timeService"></param>
        public UnderlyingSystem(HistoricalAccessServerConfiguration configuration, ushort namespaceIndex, TimeService timeService)
        {
            _configuration = configuration;
            _namespaceIndex = namespaceIndex;
            _timeService = timeService;
        }

        /// <summary>
        /// Returns a folder object for the specified node.
        /// </summary>
        /// <param name="rootId"></param>
        public ArchiveFolderState GetFolderState(string rootId)
        {
            var path = new StringBuilder()
                .Append(_configuration.ArchiveRoot)
                .Append('/')
                .Append(rootId);

            var folder = new ArchiveFolder(rootId, new DirectoryInfo(path.ToString()));
            return new ArchiveFolderState(folder, _namespaceIndex);
        }

        /// <summary>
        /// Returns a item object for the specified node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parsedNodeId"></param>
        public ArchiveItemState GetItemState(ISystemContext context, ParsedNodeId parsedNodeId)
        {
            if (parsedNodeId.RootType != NodeTypes.Item)
            {
                return null;
            }

            var path = new StringBuilder()
                .Append(_configuration.ArchiveRoot)
                .Append('/')
                .Append(parsedNodeId.RootId);

            var item = new ArchiveItem(parsedNodeId.RootId, new FileInfo(path.ToString()));

            return new ArchiveItemState(context, item, _namespaceIndex, _timeService);
        }

        private readonly ushort _namespaceIndex;
        private readonly TimeService _timeService;
        private readonly HistoricalAccessServerConfiguration _configuration;
    }
}
