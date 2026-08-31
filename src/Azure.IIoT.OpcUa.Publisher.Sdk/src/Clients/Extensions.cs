// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using System;

    /// <summary>
    /// Helper extensions shared by clients
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Deserialize the response or throw if failed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer">Options with serializer configuration.</param>
        /// <returns></returns>
        /// <exception cref="MethodCallException"></exception>
        public static T DeserializeResponse<T>(this ReadOnlyMemory<byte> buffer)
        {
            return Json.Deserialize<T>(buffer) ?? throw new MethodCallException("Bad response");
        }
    }
}
