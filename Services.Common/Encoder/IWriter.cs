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
    /// Serialization Writer
    /// </summary>
    public interface IWriter {

        /// <summary>
        /// Begin writing
        /// </summary>
        /// <param name="sourceIri"></param>
        Task BeginAsync(string sourceIri);

        /// <summary>
        /// WriteAsync an object and all object members
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="objectIri"></param>
        /// <param name="typeIri"></param>
        Task BeginObjectAsync(string propertyIri, string objectIri,
            string typeIri, CancellationToken ct);

        /// <summary>
        /// Writes a null
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <returns>whether value was written</returns>
        Task WriteNilAsync(string propertyIri,
            CancellationToken ct);

        /// <summary>
        /// Writes a boolean value
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, bool value,
            CancellationToken ct);

        /// <summary>
        /// Writes a byte value
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, byte value,
            CancellationToken ct);

        /// <summary>
        /// Writes a binary value
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, byte[] value,
            CancellationToken ct);

        /// <summary>
        /// Writes a time value
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, DateTime value,
            CancellationToken ct);

        /// <summary>
        /// Writes a duration value
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, TimeSpan value,
            CancellationToken ct);

        /// <summary>
        /// Writes a double
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, double value,
            CancellationToken ct);

        /// <summary>
        /// Writes a uri value
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <param name="isResource"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, Uri value, bool isResource,
            CancellationToken ct);

        /// <summary>
        /// Writes float
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, float value,
            CancellationToken ct);

        /// <summary>
        /// Writes a decimal
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, decimal value,
            CancellationToken ct);

        /// <summary>
        /// Writes a guid
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, Guid value,
            CancellationToken ct);

        /// <summary>
        /// Writes a 32 bit integer
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, int value,
            CancellationToken ct);

        /// <summary>
        /// Writes a signed 64 bit integer
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, long value,
            CancellationToken ct);

        /// <summary>
        /// Writes a signed byte
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, sbyte value,
            CancellationToken ct);

        /// <summary>
        /// Writes a short
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, short value,
            CancellationToken ct);

        /// <summary>
        /// Writes a string
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, string value,
            CancellationToken ct);

        /// <summary>
        /// Writes a string
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <param name="language"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, string value, string language,
            CancellationToken ct);

        /// <summary>
        /// Writes a 32 bit unsigned integer
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, uint value,
            CancellationToken ct);

        /// <summary>
        /// Writes a 64 bit unsigned integer
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, ulong value,
            CancellationToken ct);

        /// <summary>
        /// Writes an enumeration value
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, Enum value,
            CancellationToken ct);

        /// <summary>
        /// Writes a unsigned short
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync(string propertyIri, ushort value,
            CancellationToken ct);

        /// <summary>
        /// Writes an xml element
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteXmlAsync(string propertyIri, XmlElement value,
            CancellationToken ct);

        /// <summary>
        /// Writes json token
        /// </summary>
        /// <param name="propertyIri"></param>
        /// <param name="value"></param>
        /// <returns>whether value was written</returns>
        Task WriteJsonAsync(string propertyIri, JToken value,
            CancellationToken ct);

        /// <summary>
        /// Writes a list of elements using write function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyIri"></param>
        /// <param name="values"></param>
        /// <param name="writer"></param>
        /// <returns>whether value was written</returns>
        Task WriteAsync<T>(string propertyIri, IEnumerable<T> values,
            Func<string, T, CancellationToken, Task> writer, CancellationToken ct);

        /// <summary>
        /// Complete current object
        /// </summary>
        void EndObject();

        /// <summary>
        /// End writing
        /// </summary>
        Task EndAsync();
    }
}