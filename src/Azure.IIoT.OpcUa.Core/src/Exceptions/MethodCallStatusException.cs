// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Text.Json;

    /// <summary>
    /// This exception is thrown when method call returned a
    /// status other than 200.
    /// </summary>
    /// <remarks>
    /// Payloads are (de)serialized through the source-generated
    /// <see cref="CoreJsonContext"/> so the type stays Native-AOT and
    /// trim safe. The wire shape is <see cref="ErrorDetails"/> (RFC 7807
    /// problem details) for backward compatibility.
    /// </remarks>
    public class MethodCallStatusException : MethodCallException
    {
        /// <summary>
        /// Problem details
        /// </summary>
        public ErrorDetails Details { get; }

        /// <summary>
        /// Status code
        /// </summary>
        public int Status => Details.Status ?? 500;

        /// <inheritdoc/>
        internal MethodCallStatusException() :
            this((string?)null)
        {
        }

        /// <inheritdoc/>
        public MethodCallStatusException(string? message) :
            this(null, message)
        {
        }

        /// <inheritdoc/>
        public MethodCallStatusException(string? message, Exception innerException) :
            this(null, innerException, message)
        {
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="status"></param>
        /// <param name="errorDetails"></param>
        /// <param name="title"></param>
        /// <param name="type"></param>
        public MethodCallStatusException(int? status, string? errorDetails,
            string? title = null, string? type = null) :
            this(new ErrorDetails
            {
                Detail = errorDetails,
                Title = title,
                Type = type,
                Status = status ?? 500
            })
        {
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="status"></param>
        /// <param name="innerException"></param>
        /// <param name="errorDetails"></param>
        /// <param name="title"></param>
        /// <param name="type"></param>
        public MethodCallStatusException(int? status, Exception innerException,
            string? errorDetails, string? title = null, string? type = null) :
            this(new ErrorDetails
            {
                Detail = errorDetails,
                Title = title,
                Type = type,
                Status = status ?? 500
            }, innerException)
        {
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="details"></param>
        public MethodCallStatusException(ErrorDetails details) :
            base(AsString(details))
        {
            Details = details;
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="details"></param>
        /// <param name="innerException"></param>
        public MethodCallStatusException(ErrorDetails details,
            Exception innerException) :
            base(AsString(details), innerException)
        {
            Details = details;
        }

        /// <summary>
        /// Try deserialize exception
        /// </summary>
        /// <param name="response"></param>
        /// <param name="outerStatus"></param>
        public static MethodCallStatusException Deserialize(
            ReadOnlyMemory<byte> response, int? outerStatus = null)
        {
            try
            {
                var result = Deserialize(response, outerStatus, out _);
                if (result != null)
                {
                    return result;
                }
                var message = Encoding.UTF8.GetString(response.Span);
                return new MethodCallStatusException(outerStatus ?? 500, message);
            }
            catch (Exception ex)
            {
                return new MethodCallStatusException(outerStatus ?? 500, ex, ex.Message);
            }
        }

        /// <summary>
        /// Throw
        /// </summary>
        /// <param name="response"></param>
        /// <param name="outerStatus"></param>
        [DoesNotReturn]
        public static void Throw(ReadOnlyMemory<byte> response,
            int? outerStatus = null)
        {
            var result = Deserialize(response, outerStatus, out var inner);
            if (result != null)
            {
                throw result;
            }
            if (inner != null)
            {
                throw new MethodCallStatusException(outerStatus ?? 500, inner, inner.Message);
            }
            throw new MethodCallStatusException(outerStatus ?? 500, "Undefined error.");
        }

        /// <summary>
        /// Get payload
        /// </summary>
        public ReadOnlyMemory<byte> Serialize()
        {
            return JsonSerializer.SerializeToUtf8Bytes(Details,
                CoreJsonContext.Default.ErrorDetails).AsMemory();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return AsString(Details);
        }

        /// <summary>
        /// Convert to string message
        /// </summary>
        /// <param name="details"></param>
        /// <returns></returns>
        private static string AsString(ErrorDetails details)
        {
            return JsonSerializer.Serialize(details,
                CoreJsonContext.Default.ErrorDetails);
        }

        /// <summary>
        /// Helper to deserialize the payload
        /// </summary>
        /// <param name="response"></param>
        /// <param name="outerStatus"></param>
        /// <param name="innerException"></param>
        /// <returns></returns>
        private static MethodCallStatusException? Deserialize(
            ReadOnlyMemory<byte> response, int? outerStatus,
            out Exception? innerException)
        {
            innerException = null;
            if (response.Length == 0 || response.Span[0] == 0)
            {
                return new MethodCallStatusException(outerStatus ?? 500, string.Empty);
            }
            try
            {
                var details = JsonSerializer.Deserialize(response.Span,
                    CoreJsonContext.Default.ErrorDetails);
                if (details != null)
                {
                    details.Status ??= outerStatus;
                    return new MethodCallStatusException(details);
                }
            }
            catch (Exception ex)
            {
                innerException = ex;
            }
            return null;
        }
    }
}
