// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Xml {
    using System.Runtime.Serialization;

    /// <summary>
    /// Xml element extensions
    /// </summary>
    public static class XmlElementEx {

        /// <summary>
        /// Serialize object to xml element
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static XmlElement SerializeObject(object o) {
            var doc = new XmlDocument();
            using (var writer = doc.CreateNavigator().AppendChild()) {
                new DataContractSerializer(o.GetType()).WriteObject(writer, o);
            }
            return doc.DocumentElement;
        }

        /// <summary>
        /// Deserialize object to xml element
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static T ToObject<T>(this XmlElement element) {
            var serializer = new DataContractSerializer(typeof(T));
            using (var reader = new XmlNodeReader(element)) {
                return (T)serializer.ReadObject(reader);
            }
        }
    }
}
