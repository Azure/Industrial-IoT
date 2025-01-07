// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    using Azure.IIoT.OpcUa.Encoders;
    using System.IO;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Encodeable extensions
    /// </summary>
    public static class EncodeableEx
    {
        /// <summary>
        /// Convert encodeable to xml
        /// </summary>
        /// <param name="encodeable"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static XmlElement? AsXmlElement(this IEncodeable encodeable,
            IServiceMessageContext context)
        {
            // Bug in stack, do not dispose as we close below
#pragma warning disable CA2000 // Dispose objects before losing scope
            var encoder = new XmlEncoder(context);
#pragma warning restore CA2000 // Dispose objects before losing scope
            encoder.WriteExtensionObjectBody(encodeable);
            var document = new XmlDocument
            {
                InnerXml = encoder.CloseAndReturnText()
            };
            return document.DocumentElement;
        }

        /// <summary>
        /// Convert encodeable to binary
        /// </summary>
        /// <param name="encodeable"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static byte[] AsBinary(this IEncodeable encodeable,
            IServiceMessageContext context)
        {
            using var stream = new MemoryStream();
            using (var encoder = new BinaryEncoder(stream, context, true))
            {
                encodeable.Encode(encoder);
            }
            return stream.ToArray();
        }

        /// <summary>
        /// Convert encodeable to json
        /// </summary>
        /// <param name="encodeable"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string AsJson(this IEncodeable encodeable,
            IServiceMessageContext context)
        {
            using var stream = new MemoryStream();
            using (var encoder = new JsonEncoderEx(stream, context, leaveOpen: true))
            {
                encodeable.Encode(encoder);
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
