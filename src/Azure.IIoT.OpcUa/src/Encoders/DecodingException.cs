// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Opc.Ua;
    using System;

    /// <inheritdoc/>
    [Serializable]
    public sealed class DecodingException : ServiceResultException
    {
        /// <summary>
        /// Additional information
        /// </summary>
        public string? AdditionalInformation { get; set; }

        /// <inheritdoc/>
        public DecodingException(string message) :
            base(StatusCodes.BadDecodingError, message)
        {
        }

        /// <inheritdoc/>
        public DecodingException(string message, Exception innerException) :
            base(StatusCodes.BadDecodingError, message, innerException)
        {
        }

        /// <inheritdoc/>
        public DecodingException(string message, string additionalInformation) :
            base(StatusCodes.BadDecodingError, message)
        {
            AdditionalInformation = additionalInformation;
        }

        /// <inheritdoc/>
        public DecodingException(uint statusCode, string message) :
            base(statusCode, message)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var message = "Failed to decode. " + base.ToString();
            if (AdditionalInformation != null)
            {
                message += "\n" + AdditionalInformation;
            }
            return message;
        }
    }
}
