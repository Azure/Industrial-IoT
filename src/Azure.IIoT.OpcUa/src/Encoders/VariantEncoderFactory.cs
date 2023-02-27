// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Furly.Extensions.Serializers;
    using Opc.Ua;

    /// <summary>
    /// Json variant codec
    /// </summary>
    public class VariantEncoderFactory : IVariantEncoderFactory
    {
        /// <summary>
        /// Create encoder
        /// </summary>
        /// <param name="serializer"></param>
        public VariantEncoderFactory(IJsonSerializer serializer)
        {
            _serializer = serializer ?? throw new System.ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public IVariantEncoder Create(IServiceMessageContext context)
        {
            return new JsonVariantEncoder(context, _serializer);
        }

        private readonly IJsonSerializer _serializer;
    }
}
