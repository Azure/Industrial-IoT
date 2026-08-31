// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Router
{
    using System;

    /// <summary>
    /// Ignore method or property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method,
        AllowMultiple = true)]
    public sealed class IgnoreAttribute : Attribute;
}
