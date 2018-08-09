// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Encoder {
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Xml;

    /// <summary>
    /// Reader interface
    /// </summary>
    public interface IReader {

        /// <summary>
        /// Begin reading from graph
        /// </summary>
        /// <param name="sourceIri"></param>
        void Begin(string sourceIri);

        /// <summary>
        /// Find the subject using the subject iri.
        /// </summary>
        /// <returns>null if subject does not exist</returns>
        object GetSubject(string subjectIri);

        /// <summary>
        /// Pushes a new subject onto the stack to read.
        /// </summary>
        /// <param name="subjectIri"></param>
        void PushSubject(object subjectIri);

        /// <summary>
        /// Sets the predicate iri of the property to read
        /// next
        /// </summary>
        /// <param name="propertyIri"></param>
        void SelectProperty(string propertyIri);

        /// <summary>
        /// Complete current subject
        /// </summary>
        void PopSubject();

        /// <summary>
        /// Read date time offsets UTC
        /// </summary>
        /// <returns></returns>
        DateTime? ReadDateTime();

        /// <summary>
        /// Returns all timespan values (duration)
        /// </summary>
        /// <returns></returns>
        TimeSpan? ReadTimeSpan();

        /// <summary>
        /// Read bools
        /// </summary>
        /// <returns></returns>
        bool? ReadBoolean();

        /// <summary>
        /// Read bytes
        /// </summary>
        /// <returns></returns>
        byte? ReadByte();

        /// <summary>
        /// Read chars
        /// </summary>
        /// <returns></returns>
        char? ReadChar();

        /// <summary>
        /// Read decimals
        /// </summary>
        /// <returns></returns>
        decimal? ReadDecimal();

        /// <summary>
        /// Read doubles
        /// </summary>
        /// <returns></returns>
        double? ReadDouble();

        /// <summary>
        /// Read floats
        /// </summary>
        /// <returns></returns>
        float? ReadFloat();

        /// <summary>
        /// Read ints
        /// </summary>
        /// <returns></returns>
        int? ReadInt32();

        /// <summary>
        /// Read longs
        /// </summary>
        /// <returns></returns>
        long? ReadInt64();

        /// <summary>
        /// Read shorts
        /// </summary>
        /// <returns></returns>
        short? ReadInt16();

        /// <summary>
        /// Read signed bytes
        /// </summary>
        /// <returns></returns>
        sbyte? ReadSByte();

        /// <summary>
        /// Read unsigned ints
        /// </summary>
        /// <returns></returns>
        uint? ReadUInt32();

        /// <summary>
        /// Read unsigned longs
        /// </summary>
        /// <returns></returns>
        ulong? ReadUInt64();

        /// <summary>
        /// Read unsigned shorts
        /// </summary>
        /// <returns></returns>
        ushort? ReadUInt16();

        /// <summary>
        /// Read guids
        /// </summary>
        /// <returns></returns>
        Guid? ReadGuid();

        /// <summary>
        /// </summary>
        /// <returns></returns>
        object ReadObject();

        /// <summary>
        /// Read buffer
        /// </summary>
        /// <returns></returns>
        byte[] ReadBuffer();

        /// <summary>
        /// Read strings
        /// </summary>
        /// <returns></returns>
        string ReadString();

        /// <summary>
        /// Reads strings with locale
        /// </summary>
        /// <returns></returns>
        string ReadString(out string locale);

        /// <summary>
        /// Read uri links, either resources, or pure uris
        /// </summary>
        /// <param name="isResource"></param>
        /// <returns></returns>
        Uri ReadUri(bool isResource);

        /// <summary>
        /// Read enums of specified enum type
        /// </summary>
        /// <returns></returns>
        Enum ReadEnum(Type enumType);

        /// <summary>
        /// Read xml elements
        /// </summary>
        /// <returns></returns>
        XmlElement ReadXmlElement();

        /// <summary>
        /// Read json tokens
        /// </summary>
        /// <returns></returns>
        JToken ReadJsonToken();

        /// <summary>
        /// Reads ordered lists of elements using a reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns>whether value was written</returns>
        IList<T> ReadArray<T>(Func<T> reader);

        /// <summary>
        /// End reading
        /// </summary>
        void End();
    }
}
