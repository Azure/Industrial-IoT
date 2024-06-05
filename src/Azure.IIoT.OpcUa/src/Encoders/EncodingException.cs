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
    public sealed class EncodingException : ServiceResultException
    {
        /// <summary>
        /// Additional information
        /// </summary>
        public string? AdditionalInformation { get; set; }

        /// <inheritdoc/>
        public EncodingException(string message) :
            base(StatusCodes.BadEncodingError, message)
        {
        }

        /// <inheritdoc/>
        public EncodingException(string message, Exception innerException) :
            base(StatusCodes.BadEncodingError, message, innerException)
        {
        }

        /// <inheritdoc/>
        public EncodingException(string message, string additionalInformation) :
            base(StatusCodes.BadEncodingError, message)
        {
            AdditionalInformation = additionalInformation;
        }

        /// <inheritdoc/>
        public EncodingException(uint statusCode, string message) :
            base(statusCode, message)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var message = "Failed to encode. " + base.ToString();
            if (AdditionalInformation != null)
            {
                message += "\n" + AdditionalInformation;
            }
            return message;
        }
    }
}
