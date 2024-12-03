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
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Stores the metadata for a node representing a folder on a file system.
    /// </summary>
    public class ArchiveFolder
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="uniquePath"></param>
        /// <param name="folder"></param>
        public ArchiveFolder(string uniquePath, DirectoryInfo folder)
        {
            UniquePath = uniquePath;
            DirectoryInfo = folder;
        }

        /// <summary>
        /// Returns the child folders.
        /// </summary>
        public ArchiveFolder[] GetChildFolders()
        {
            var folders = new List<ArchiveFolder>();

            if (!DirectoryInfo.Exists)
            {
                return [.. folders];
            }

            foreach (var directory in DirectoryInfo.GetDirectories())
            {
                var buffer = new StringBuilder(UniquePath);
                buffer.Append('/')
                    .Append(directory.Name);
                folders.Add(new ArchiveFolder(buffer.ToString(), directory));
            }

            return [.. folders];
        }

        /// <summary>
        /// Returns the child folders.
        /// </summary>
        public ArchiveItem[] GetItems()
        {
            var items = new List<ArchiveItem>();

            if (!DirectoryInfo.Exists)
            {
                return [.. items];
            }

            foreach (var file in DirectoryInfo.GetFiles("*.csv"))
            {
                var buffer = new StringBuilder(UniquePath);
                buffer.Append('/')
                    .Append(file.Name);
                items.Add(new ArchiveItem(buffer.ToString(), file));
            }

            return [.. items];
        }

        /// <summary>
        /// Returns the parent folder.
        /// </summary>
        public ArchiveFolder GetParentFolder()
        {
            var parentPath = string.Empty;

            if (!DirectoryInfo.Exists)
            {
                return null;
            }

            var index = UniquePath.LastIndexOf('/');

            if (index > 0)
            {
                parentPath = UniquePath[..index];
            }

            return new ArchiveFolder(parentPath, DirectoryInfo.Parent);
        }

        /// <summary>
        /// The unique path to the folder in the archive.
        /// </summary>
        public string UniquePath { get; }

        /// <summary>
        /// A name for the folder.
        /// </summary>
        public string Name => DirectoryInfo.Name;

        /// <summary>
        /// The physical folder in the archive.
        /// </summary>
        public DirectoryInfo DirectoryInfo { get; }
    }
}
