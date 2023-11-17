// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Utils
{
    using System;
    using System.IO;

    /// <summary>
    /// Base class that provides a temporary file that will be cleaned up on disposal.
    /// </summary>
    public class TempFileProviderBase : IDisposable
    {
        protected readonly string _tempFile;

        public TempFileProviderBase()
        {
            _tempFile = Path.GetTempFileName();
        }

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                // Remove temporary published nodes file if one was created.
                if (File.Exists(_tempFile))
                {
                    File.Delete(_tempFile);
                }
            }
            catch
            {
                // Nothign to do.
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
