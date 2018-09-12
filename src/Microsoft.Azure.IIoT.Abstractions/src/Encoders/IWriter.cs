// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Encoders {
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Xml;

    /// <summary>
    /// Serialization Writer
    /// </summary>
    public interface IWriter {

        /// <summary>
        /// Begin writing
        /// </summary>
        /// <param name="sourceIri"></param>
        void Begin(string sourceIri);

        /// <summary>
        /// Write an object and all object members
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="objectIri"></param>
        /// <param name="typeIri"></param>
        void BeginObject(string propertyIri, string objectIri,
            string typeIri);

        /// <summary>
        /// Writes a null
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <returns>whether value was written</returns>
        void WriteNil(string propertyIri);

        /// <summary>
        /// Writes a boolean value
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, bool value);

        /// <summary>
        /// Writes a byte value
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, byte value);

        /// <summary>
        /// Writes a binary value
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, byte[] value);

        /// <summary>
        /// Writes a time value
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, DateTime value);

        /// <summary>
        /// Writes a duration value
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, TimeSpan value);

        /// <summary>
        /// Writes a double
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, double value);

        /// <summary>
        /// Writes a uri value
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <param name="isResource"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, Uri value, bool isResource);

        /// <summary>
        /// Writes float
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, float value);

        /// <summary>
        /// Writes a decimal
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, decimal value);

        /// <summary>
        /// Writes a guid
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, Guid value);

        /// <summary>
        /// Writes a 32 bit integer
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, int value);

        /// <summary>
        /// Writes a signed 64 bit integer
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, long value);

        /// <summary>
        /// Writes a signed byte
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, sbyte value);

        /// <summary>
        /// Writes a short
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, short value);

        /// <summary>
        /// Writes a string
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, string value);

        /// <summary>
        /// Writes a string
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <param name="language"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, string value, string language);

        /// <summary>
        /// Writes a 32 bit unsigned integer
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, uint value);

        /// <summary>
        /// Writes a 64 bit unsigned integer
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, ulong value);

        /// <summary>
        /// Writes an enumeration value
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, Enum value);

        /// <summary>
        /// Writes a unsigned short
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void Write(string propertyIri, ushort value);

        /// <summary>
        /// Writes an xml element
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void WriteXml(string propertyIri, XmlElement value);

        /// <summary>
        /// Writes json token
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        void WriteJson(string propertyIri, JToken value);

        /// <summary>
        /// Writes a list of elements using write function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyIri"></param>
        /// <param name="values"></param>
        /// <param name="writer"></param>
        /// <returns>whether value was written</returns>
        void Write<T>(string propertyIri, IEnumerable<T> values,
            Action<string, T> writer);

        /// <summary>
        /// Complete current object
        /// </summary>
        void EndObject();

        /// <summary>
        /// End writing
        /// </summary>
        void End();
    }
}
