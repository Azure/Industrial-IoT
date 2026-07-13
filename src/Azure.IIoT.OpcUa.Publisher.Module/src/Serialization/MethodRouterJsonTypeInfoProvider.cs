// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Serialization
{
    using Azure.IIoT.OpcUa.Core.Rpc.Router;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Text.Json.Serialization.Metadata;

    /// <summary>
    /// Composes the source-generated Publisher and Module contract contexts for
    /// direct-method descriptors without using the shared reflection fallback.
    /// </summary>
    internal sealed class MethodRouterJsonTypeInfoProvider :
        IMethodRouterJsonTypeInfoProvider
    {
        /// <inheritdoc/>
        public JsonTypeInfo? GetTypeInfo(Type type)
        {
            return Json.Options.TypeInfoResolver?.GetTypeInfo(type, Json.Options);
        }
    }
}
