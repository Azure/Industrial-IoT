// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Utils
{
    using System.IO;
    using System.Text;

    /// <summary>
    /// Utilities for tests.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Get content of a file as string.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetFileContent(string path)
        {
            using var payloadReader = new StreamReader(path);
            return payloadReader.ReadToEnd();
        }

        /// <summary>
        /// Copy content of source file to destination file.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public static void CopyContent(string sourcePath, string destinationPath)
        {
            var content = GetFileContent(sourcePath);

            using var fileStream = new FileStream(destinationPath,
                FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            fileStream.Write(Encoding.UTF8.GetBytes(content));
        }
    }
}
