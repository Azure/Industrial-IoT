// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Xml {
    using System.Xml.Serialization;

    public static class XmlElementEx {

        /// <summary>
        /// Serialize object to xml element
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static XmlElement SerializeObject(object o) {
            var doc = new XmlDocument();
            using (var writer = doc.CreateNavigator().AppendChild()) {
                new XmlSerializer(o.GetType()).Serialize(writer, o);
            }
            return doc.DocumentElement;
        }

        /// <summary>
        /// Deserialize object to xml element
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static T ToObject<T>(this XmlElement element) {
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(new XmlNodeReader(element));
        }
    }
}
