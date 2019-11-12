// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {

    /// <summary>
    /// Allows a module to control the hosting
    /// process in a controlled manner.
    /// </summary>
    public interface IProcessControl {

        /// <summary>
        /// Reset the host process
        /// </summary>
        void Reset();

        /// <summary>
        /// Exit the process
        /// </summary>
        void Exit(int exitCode);
    }
}
