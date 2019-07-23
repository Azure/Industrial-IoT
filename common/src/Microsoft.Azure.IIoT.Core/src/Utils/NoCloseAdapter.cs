// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using System.IO;

    /// <summary>
    /// Stream adapter that prohibits closing
    /// and disposing of the underlying stream.
    /// </summary>
    public class NoCloseAdapter : StreamAdapter {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="inner"></param>
        public NoCloseAdapter(Stream inner) :
            base(inner) { }

        /// <inheritdoc/>
        public override void Dispose() {
        }

        /// <inheritdoc/>
        public override void Close() {
        }
    }
}

