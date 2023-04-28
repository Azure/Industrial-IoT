// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.Clients
{
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.MessagePack;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Helper extensions shared by clients
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Resolve the right serializer from options configuration.
        /// </summary>
        /// <param name="serializers">Registered serializers.</param>
        /// <param name="options">Options with serializer configuration.</param>
        /// <returns></returns>
        public static ISerializer? Resolve(this IEnumerable<ISerializer> serializers,
            ServiceSdkOptions options)
        {
            if (options?.UseMessagePackProtocol ?? false)
            {
                return serializers.OfType<MessagePackSerializer>().FirstOrDefault();
            }

            return serializers.OfType<IJsonSerializer>().FirstOrDefault();
        }
    }
}
