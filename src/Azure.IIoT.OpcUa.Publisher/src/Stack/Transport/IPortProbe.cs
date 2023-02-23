﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Transport
{
    /// <summary>
    /// Port probe factory
    /// </summary>
    public interface IPortProbe
    {
        /// <summary>
        /// Create async probe handler
        /// </summary>
        /// <returns></returns>
        IAsyncProbe Create();
    }
}
