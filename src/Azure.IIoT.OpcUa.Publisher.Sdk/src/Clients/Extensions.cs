// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Clients
{
    using Furly.Exceptions;
    using Furly.Extensions.Serializers;
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
        /// <param name="serializer">Register serializers.</param>
        /// <param name="buffer">Options with serializer configuration.</param>
        /// <returns></returns>
        /// <exception cref="MethodCallException"></exception>
        public static T DeserializeResponse<T>(this ISerializer serializer,
            ReadOnlyMemory<byte> buffer)
        {
            var response = serializer.Deserialize<T>(buffer);
            if (response is null)
            {
                throw new MethodCallException("Bad response");
            }
            return response;
        }
    }
}
