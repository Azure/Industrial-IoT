// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.BouncyCastle {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Org.BouncyCastle.Asn1.X509;
    using Org.BouncyCastle.Crypto;
    using System;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// The signature factory for Bouncy Castle to sign a digest.
    /// </summary>
    internal sealed class SignatureFactory : ISignatureFactory {

        /// <inheritdoc/>
        public object AlgorithmDetails { get; }

        /// <summary>
        /// Constructor which also specifies a source of randomness
        /// to be used if one is required.
        /// </summary>
        /// <param name="signatureType">The signature algorithm to use.
        /// </param>
        /// <param name="generator">The signature generator.</param>
        public SignatureFactory(SignatureType signatureType,
            X509SignatureGenerator generator) {
            _generator = generator;
            _signatureType = signatureType;
            var algId = signatureType.ToAlgorithmIdentifier();
            AlgorithmDetails = AlgorithmIdentifier.GetInstance(algId);
        }

        /// <inheritdoc/>
        public IStreamCalculator CreateCalculator() {
            return new StreamCalculator(this);
        }

        /// <summary>
        /// Signs a Bouncy Castle digest stream with the
        /// .Net X509SignatureGenerator.
        /// </summary>
        private sealed class StreamCalculator : IStreamCalculator {

            /// <inheritdoc/>
            public Stream Stream { get; }

            /// <summary>
            /// Create factory
            /// </summary>
            /// <param name="outer"></param>
            public StreamCalculator(SignatureFactory outer) {
                Stream = new MemoryStream();
                _outer = outer;
            }

            /// <inheritdoc/>
            public object GetResult() {
                var memStream = Stream as MemoryStream;
                var digest = memStream.ToArray();
                var signature = _outer._generator.SignData(digest,
                    _outer._signatureType.ToHashAlgorithmName());
                return new MemoryBlockResult(signature);
            }

            /// <summary>
            /// Helper for Bouncy Castle signing operation to store
            /// the result in a memory block.
            /// </summary>
            private class MemoryBlockResult : IBlockResult {

                /// <inheritdoc/>
                public MemoryBlockResult(byte[] data) {
                    _data = data;
                }

                /// <inheritdoc/>
                public byte[] Collect() {
                    return _data;
                }

                /// <inheritdoc/>
                public int Collect(byte[] destination, int offset) {
                    throw new NotSupportedException();
                }

                private readonly byte[] _data;
            }

            private readonly SignatureFactory _outer;
        }

        private readonly X509SignatureGenerator _generator;
        private readonly SignatureType _signatureType;
    }
}
