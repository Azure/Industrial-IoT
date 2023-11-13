// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    /// <summary>
    /// Process control provider
    /// </summary>
    public interface IProcessControl
    {
        /// <summary>
        /// Shutdown publisher
        /// </summary>
        /// <param name="failFast"></param>
        /// <returns>false if shutdown failed</returns>
        bool Shutdown(bool failFast);
    }
}
