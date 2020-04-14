// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Storage {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Crypto.Storage.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Private key database - do not use in production as keys are
    /// acccessible to everyone with access to the database.  This
    /// is for testing and demonstration only.
    /// </summary>
    public class KeyDatabase : IKeyStore {

        /// <summary>
        /// Create database
        /// </summary>
        /// <param name="container"></param>
        /// <param name="serializer"></param>
        public KeyDatabase(IItemContainerFactory container, IJsonSerializer serializer) {
            _keys = container.OpenAsync("keystore").Result.AsDocuments();
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public async Task<KeyHandle> CreateKeyAsync(string name,
            CreateKeyParams keyParams, KeyStoreProperties store, CancellationToken ct) {
            return await ImportKeyAsync(name, keyParams.CreateKey(), store, ct);
        }

        /// <inheritdoc/>
        public async Task<KeyHandle> ImportKeyAsync(string name, Key key,
            KeyStoreProperties store, CancellationToken ct) {
            var document = new KeyDocument {
                Id = name,
                KeyJson = _serializer.FromObject(key),
                IsDisabled = false,
                IsExportable = store?.Exportable ?? false,
            };
            var result = await _keys.AddAsync(document, ct, name);
            return new KeyId(result.Id);
        }

        /// <inheritdoc/>
        public Task DeleteKeyAsync(KeyHandle handle, CancellationToken ct) {
            return _keys.DeleteAsync(KeyId.GetId(handle), ct);
        }

        /// <inheritdoc/>
        public Task<KeyHandle> GetKeyHandleAsync(string name,
            CancellationToken ct) {
            return Task.FromResult<KeyHandle>(new KeyId(name));
        }

        /// <inheritdoc/>
        public async Task DisableKeyAsync(KeyHandle handle, CancellationToken ct) {
            var keyId = KeyId.GetId(handle);
            while (true) {
                try {
                    var document = await _keys.FindAsync<KeyDocument>(keyId, ct);
                    if (document == null) {
                        throw new ResourceNotFoundException($"{keyId} not found");
                    }
                    if (!document.Value.IsDisabled) {
                        await _keys.ReplaceAsync(document, new KeyDocument {
                            Id = document.Value.Id,
                            IsDisabled = true,
                            KeyJson = document.Value.KeyJson
                        }, ct);
                    }
                    return;
                }
                catch (ResourceOutOfDateException) {
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<Key> GetPublicKeyAsync(KeyHandle handle, CancellationToken ct) {
            var document = await _keys.GetAsync<KeyDocument>(
                KeyId.GetId(handle), ct);
            return document.Value.KeyJson.ToKey().GetPublicKey();
        }

        /// <inheritdoc/>
        public async Task<Key> ExportKeyAsync(KeyHandle handle, CancellationToken ct) {
            var document = await _keys.GetAsync<KeyDocument>(
                KeyId.GetId(handle), ct);
            if (!document.Value.IsExportable) {
                throw new InvalidOperationException("Key is not exportable");
            }
            if (document.Value.IsDisabled) {
                throw new InvalidOperationException("Key is disabled");
            }
            return document.Value.KeyJson.ToKey();
        }

        /// <inheritdoc/>
        public async Task<byte[]> SignAsync(KeyHandle handle, byte[] hash,
            SignatureType algorithm, CancellationToken ct) {
            var document = await _keys.GetAsync<KeyDocument>(
                KeyId.GetId(handle), ct);
            if (document.Value.IsDisabled) {
                throw new InvalidOperationException("Key is disabled");
            }
            var key = document.Value.KeyJson.ToKey();
            if (key == null) {
                throw new ResourceNotFoundException("Key not found");
            }
            switch (key.Type) {
                case KeyType.RSA:
                    using (var rsa = key.ToRSA()) {
                        return rsa.SignHash(hash, algorithm.ToHashAlgorithmName(),
                            algorithm.ToRSASignaturePadding());
                    }
                case KeyType.ECC:
                    using (var ecc = key.ToECDsa()) {
                        return ecc.SignHash(hash);
                    }
                default:
                    throw new ArgumentException("Bad key type passed for signing");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> VerifyAsync(KeyHandle handle, byte[] hash,
            SignatureType algorithm, byte[] signature, CancellationToken ct) {
            var document = await _keys.GetAsync<KeyDocument>(KeyId.GetId(handle), ct);
            if (document.Value.IsDisabled) {
                throw new InvalidOperationException("Key is disabled");
            }
            var key = document.Value.KeyJson.ToKey();
            if (key == null) {
                throw new ResourceNotFoundException("Key not found");
            }
            switch (key.Type) {
                case KeyType.RSA:
                    using (var rsa = key.ToRSA()) {
                        return rsa.VerifyHash(hash, signature, algorithm.ToHashAlgorithmName(),
                            algorithm.ToRSASignaturePadding());
                    }
                case KeyType.ECC:
                    using (var ecc = key.ToECDsa()) {
                        return ecc.VerifyHash(hash, signature);
                    }
                default:
                    throw new ArgumentException("Bad key type passed for signing");
            }
        }

        private readonly IDocuments _keys;
        private readonly IJsonSerializer _serializer;
    }
}

