// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Encoder extensions
    /// </summary>
    internal static class EncoderExtensions
    {
        /// <summary>
        /// Test for
        /// </summary>
        /// <param name="decoder"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool HasField(this JsonDecoderEx decoder, string property)
        {
            if (!decoder.TryGetToken(null, out var token))
            {
                return false;
            }
            if (token is JObject o && o.TryGetValue(property,
                    StringComparison.InvariantCultureIgnoreCase, out _))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Test for array
        /// </summary>
        /// <param name="decoder"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool IsArray(this JsonDecoderEx decoder, string? property)
        {
            if (!decoder.TryGetToken(property, out var token))
            {
                return false;
            }
            return token is JArray;
        }

        /// <summary>
        /// Test for array
        /// </summary>
        /// <param name="decoder"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool IsObject(this JsonDecoderEx decoder, string? property)
        {
            if (!decoder.TryGetToken(property, out var token))
            {
                return false;
            }
            return token is JObject;
        }
    }
}
