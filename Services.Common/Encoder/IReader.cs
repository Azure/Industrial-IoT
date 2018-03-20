// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Encoder {
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Reader interface
    /// </summary>
    public interface IReader {

        /// <summary>
        /// Begin reading from graph
        /// </summary>
        /// <param name="sourceIri"></param>
        Task BeginAsync(string sourceIri);

        /// <summary>
        /// Find the subject using the subject iri.
        /// </summary>
        /// <returns>null if subject does not exist</returns>
        Task<object> GetSubjectAsync(string subjectIri);

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
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DateTime?> ReadDateTimeAsync(CancellationToken ct);

        /// <summary>
        /// Returns all timespan values (duration)
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TimeSpan?> ReadTimeSpanAsync(CancellationToken ct);

        /// <summary>
        /// Read bools
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool?> ReadBooleanAsync(CancellationToken ct);

        /// <summary>
        /// Read bytes
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<byte?> ReadByteAsync(CancellationToken ct);

        /// <summary>
        /// Read chars
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<char?> ReadCharAsync(CancellationToken ct);

        /// <summary>
        /// Read decimals
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<decimal?> ReadDecimalAsync(CancellationToken ct);

        /// <summary>
        /// Read doubles
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<double?> ReadDoubleAsync(CancellationToken ct);

        /// <summary>
        /// Read floats
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<float?> ReadFloatAsync(CancellationToken ct);

        /// <summary>
        /// Read ints
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<int?> ReadInt32Async(CancellationToken ct);

        /// <summary>
        /// Read longs
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<long?> ReadInt64Async(CancellationToken ct);

        /// <summary>
        /// Read shorts
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<short?> ReadInt16Async(CancellationToken ct);

        /// <summary>
        /// Read signed bytes
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<sbyte?> ReadSByteAsync(CancellationToken ct);

        /// <summary>
        /// Read unsigned ints
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<uint?> ReadUInt32Async(CancellationToken ct);

        /// <summary>
        /// Read unsigned longs
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ulong?> ReadUInt64Async(CancellationToken ct);

        /// <summary>
        /// Read unsigned shorts
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ushort?> ReadUInt16Async(CancellationToken ct);

        /// <summary>
        /// Read guids
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Guid?> ReadGuidAsync(CancellationToken ct);

        /// <summary>
        /// read object token
        /// </summary>
        /// <returns></returns>
        Task<object> ReadObjectAsync(CancellationToken ct);

        /// <summary>
        /// Read buffer
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<byte[]> ReadBufferAsync(CancellationToken ct);

        /// <summary>
        /// Read strings
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<string> ReadStringAsync(CancellationToken ct);

        /// <summary>
        /// Reads strings with locale
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Tuple<string, string>> ReadStringWithLocaleAsync(
            CancellationToken ct);

        /// <summary>
        /// Read uri links, either resources, or pure uris
        /// </summary>
        /// <param name="isResource"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Uri> ReadUriAsync(bool isResource, CancellationToken ct);

        /// <summary>
        /// Read enums of specified enum type
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Enum> ReadEnumAsync(Type enumType, CancellationToken ct);

        /// <summary>
        /// Read xml elements
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<XmlElement> ReadXmlElementAsync(CancellationToken ct);

        /// <summary>
        /// Read json tokens
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<JToken> ReadJsonTokenAsync(CancellationToken ct);

        /// <summary>
        /// Reads ordered lists of elements using a reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ct"></param>
        /// <param name="reader"></param>
        /// <returns>whether value was written</returns>
        Task<IList<T>> ReadListAsync<T>(
            Func<CancellationToken, Task<T>> reader, CancellationToken ct);

        /// <summary>
        /// Reads multiple items with same predicate using
        /// a reader.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ct"></param>
        /// <param name="reader"></param>
        /// <returns>whether value was written</returns>
        Task<IEnumerable<T>> ReadAsync<T>(
            Func<CancellationToken, Task<T>> reader, CancellationToken ct);

        /// <summary>
        /// End reading
        /// </summary>
        Task EndAsync();
    }
}
