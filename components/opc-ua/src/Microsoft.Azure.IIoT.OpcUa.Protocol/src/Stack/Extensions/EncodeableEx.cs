// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using System.IO;
    using System.Xml;

    /// <summary>
    /// Encodeable extensions
    /// </summary>
    public static class EncodeableEx {

        /// <summary>
        /// Convert encodeable to xml
        /// </summary>
        /// <param name="encodeable"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static XmlElement AsXmlElement(this IEncodeable encodeable,
            IServiceMessageContext context) {
#pragma warning disable IDE0067 // Dispose objects before losing scope
            var encoder = new XmlEncoder(context);
#pragma warning restore IDE0067 // Dispose objects before losing scope
            encoder.WriteExtensionObjectBody(encodeable);
            var document = new XmlDocument {
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
            IServiceMessageContext context) {
            using (var stream = new MemoryStream()) {
                using (var encoder = new BinaryEncoder(stream, context)) {
                    encodeable.Encode(encoder);
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Convert xml to encodeable
        /// </summary>
        /// <param name="xmlElement"></param>
        /// <param name="typeId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static IEncodeable ToEncodeable(this XmlElement xmlElement,
            ExpandedNodeId typeId, IServiceMessageContext context) {
            using (var decoder = new XmlDecoder(xmlElement, context)) {
                var body = decoder.ReadExtensionObjectBody(typeId);
                return body as IEncodeable;
            }
        }

        /// <summary>
        /// Convert buffer to encodeable
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="typeId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static IEncodeable ToEncodeable(this byte[] buffer,
            ExpandedNodeId typeId, IServiceMessageContext context) {
            var systemType = TypeInfo.GetSystemType(typeId.ToNodeId(context.NamespaceUris),
                context.Factory);
            if (systemType == null) {
                return null;
            }
            using (var decoder = new BinaryDecoder(buffer, context)) {
                return decoder.ReadEncodeable(null, systemType);
            }
        }
    }
}
