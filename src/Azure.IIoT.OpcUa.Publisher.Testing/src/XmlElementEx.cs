// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Xml
{
    using System.Runtime.Serialization;

    /// <summary>
    /// XML helper extensions.
    /// </summary>
    public static class XmlElementEx
    {
        /// <summary>
        /// Serialize object to XML element.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static XmlElement? SerializeObject(object o)
        {
            var doc = new XmlDocument();
            var nav = doc.CreateNavigator();
            if (nav == null)
            {
                return null;
            }
            using (var writer = nav.AppendChild())
            {
                new DataContractSerializer(o.GetType()).WriteObject(writer, o);
            }
            return doc.DocumentElement;
        }
    }
}
