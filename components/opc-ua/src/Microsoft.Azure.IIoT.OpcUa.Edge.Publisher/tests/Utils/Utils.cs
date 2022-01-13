// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Utilities for tests.
    /// </summary>
    public class Utils {

        /// <summary>
        /// Get content of a file as string.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetFileContent(string path) {
            using var payloadReader = new StreamReader(path);
            return payloadReader.ReadToEnd();
        }

        /// <summary>
        /// Copy content of source file to destination file.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        public static void CopyContent(string sourcePath, string destinationPath) {
            string content = GetFileContent(sourcePath);

            using (var fileStream = new FileStream(destinationPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite)) {
                fileStream.Write(Encoding.UTF8.GetBytes(content));
            }
        }
    }
}
