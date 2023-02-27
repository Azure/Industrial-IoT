// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    using System.IO;
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
        public static XmlElement AsXmlElement(this IEncodeable encodeable,
            IServiceMessageContext context)
        {
            var encoder = new XmlEncoder(context);
            encoder.WriteExtensionObjectBody(encodeable);
            var document = new XmlDocument
            {
                InnerXml = encoder.Close()
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
            using (var stream = new MemoryStream())
            {
                using (var encoder = new BinaryEncoder(stream, context))
                {
                    encodeable.Encode(encoder);
                }
                return stream.ToArray();
            }
        }
    }
}
