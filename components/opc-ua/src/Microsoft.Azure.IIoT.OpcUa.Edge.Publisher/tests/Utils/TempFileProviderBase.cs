// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Utils {

    using System;
    using System.IO;

    /// <summary>
    /// Base class that provides a temporary file that will be cleaned up on disposal.
    /// </summary>
    public class TempFileProviderBase : IDisposable {

        protected readonly string _tempFile;

        public TempFileProviderBase() {
            _tempFile = Path.GetTempFileName();
        }

        public void Dispose() {
            try {
                // Remove temporary published nodes file if one was created.
                if (File.Exists(_tempFile)) {
                    File.Delete(_tempFile);
                }
            }
            catch (Exception) {
                // Nothign to do.
            }
        }
    }
}
