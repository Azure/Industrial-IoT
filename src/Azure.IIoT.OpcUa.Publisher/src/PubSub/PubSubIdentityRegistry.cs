// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Persistent state that maps Publisher identifiers to their assigned
    /// OPC UA PubSub identifiers.
    /// </summary>
    public sealed class PubSubIdentityRegistrySnapshot
    {
        /// <summary>
        /// Gets or sets the persisted identity entries.
        /// </summary>
        [JsonRequired]
        public List<PubSubIdentityRegistryEntry> Entries { get; set; } = [];
    }

    /// <summary>
    /// A persistent public-to-native PubSub identifier mapping.
    /// </summary>
    public sealed record class PubSubIdentityRegistryEntry
    {
        /// <summary>
        /// Gets or sets the identifier namespace.
        /// </summary>
        public required string Scope { get; init; }

        /// <summary>
        /// Gets or sets the Publisher-facing identifier.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets or sets the assigned native identifier.
        /// </summary>
        public required ushort Value { get; init; }
    }

    /// <summary>
    /// Loads and stores PubSub identity mappings. Register an implementation
    /// backed by shared durable storage when Publisher is run in HA mode.
    /// </summary>
    public interface IPubSubIdentityRegistryStore
    {
        /// <summary>
        /// Loads the current identity mappings.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The loaded mapping snapshot.</returns>
        ValueTask<PubSubIdentityRegistrySnapshot> LoadAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores a complete identity mapping snapshot atomically.
        /// </summary>
        /// <param name="snapshot">Snapshot to store.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes after the snapshot is durable.</returns>
        ValueTask SaveAsync(PubSubIdentityRegistrySnapshot snapshot,
            CancellationToken cancellationToken = default);
    }

    internal interface IPubSubIdentityRegistry
    {
        ValueTask<IPubSubIdentityTransaction> BeginAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ushort?> TryGetIdAsync(string scope, string id,
            CancellationToken cancellationToken = default);

        ValueTask<string?> TryGetPublicIdAsync(string scope, ushort id,
            CancellationToken cancellationToken = default);
    }

    internal interface IPubSubIdentityTransaction : IAsyncDisposable
    {
        ushort GetOrAllocate(string scope, string id);

        ValueTask CommitAsync(CancellationToken cancellationToken = default);
    }

    internal sealed class PubSubIdentityRegistry : IPubSubIdentityRegistry
    {
        public PubSubIdentityRegistry(IPubSubIdentityRegistryStore store,
            Func<string, uint>? candidateFactory = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _candidateFactory = candidateFactory ?? CreateCandidate;
        }

        public async ValueTask<IPubSubIdentityTransaction> BeginAsync(
            CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
                return new Transaction(this, new Dictionary<string, ushort>(_entries,
                    StringComparer.Ordinal));
            }
            catch
            {
                _gate.Release();
                throw;
            }
        }

        public async ValueTask<ushort?> TryGetIdAsync(string scope, string id,
            CancellationToken cancellationToken = default)
        {
            ValidateIdentity(scope, id);
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
                return _entries.TryGetValue(CreateKey(scope, id), out var value)
                    ? value
                    : null;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async ValueTask<string?> TryGetPublicIdAsync(string scope, ushort id,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(scope))
            {
                throw new ArgumentException("The identity scope must not be empty.", nameof(scope));
            }
            if (id == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Zero is not a PubSub identifier.");
            }

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
                return _reverse.TryGetValue(CreateValueKey(scope, id), out var publicId)
                    ? publicId
                    : null;
            }
            finally
            {
                _gate.Release();
            }
        }

        private async ValueTask EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (_initialized)
            {
                return;
            }

            var snapshot = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            var (entries, reverse) = ValidateSnapshot(snapshot);

            _entries = entries;
            _reverse = reverse;
            _initialized = true;
        }

        internal static (Dictionary<string, ushort> Entries,
            Dictionary<string, string> Reverse) ValidateSnapshot(
                PubSubIdentityRegistrySnapshot? snapshot)
        {
            if (snapshot?.Entries is null)
            {
                throw new InvalidDataException(
                    "The persisted PubSub identity registry must contain an entries array.");
            }

            var entries = new Dictionary<string, ushort>(StringComparer.Ordinal);
            var reverse = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in snapshot.Entries)
            {
                if (entry is null)
                {
                    throw new InvalidDataException(
                        "The persisted PubSub identity registry contains a null mapping.");
                }
                try
                {
                    ValidateIdentity(entry.Scope, entry.Id);
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidDataException(
                        "The persisted PubSub identity registry contains an invalid mapping.",
                        ex);
                }
                if (entry.Value == 0)
                {
                    throw new InvalidDataException("The persisted PubSub identifier must not be zero.");
                }

                var key = CreateKey(entry.Scope, entry.Id);
                var valueKey = CreateValueKey(entry.Scope, entry.Value);
                if (!entries.TryAdd(key, entry.Value) ||
                    !reverse.TryAdd(valueKey, entry.Id))
                {
                    throw new InvalidDataException(
                        "The persisted PubSub identity registry contains duplicate mappings.");
                }
            }
            return (entries, reverse);
        }

        private async ValueTask CommitAsync(Transaction transaction,
            CancellationToken cancellationToken)
        {
            try
            {
                var reverse = CreateReverse(transaction.Entries);
                var snapshot = new PubSubIdentityRegistrySnapshot
                {
                    Entries = transaction.Entries
                        .Select(entry => CreateEntry(entry.Key, entry.Value))
                        .OrderBy(entry => entry.Scope, StringComparer.Ordinal)
                        .ThenBy(entry => entry.Id, StringComparer.Ordinal)
                        .ToList()
                };
                await _store.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
                _entries = transaction.Entries;
                _reverse = reverse;
                transaction.Complete();
            }
            finally
            {
                if (transaction.IsCompleted)
                {
                    _gate.Release();
                }
            }
        }

        private void Rollback(Transaction transaction)
        {
            if (transaction.Complete())
            {
                _gate.Release();
            }
        }

        private ushort Allocate(Dictionary<string, ushort> entries, string scope, string id)
        {
            var key = CreateKey(scope, id);
            if (entries.TryGetValue(key, out var assigned))
            {
                return assigned;
            }

            var candidate = (ushort)(_candidateFactory(key) % ushort.MaxValue + 1);
            for (var i = 0; i < ushort.MaxValue; i++)
            {
                var valueKey = CreateValueKey(scope, candidate);
                if (!ContainsValue(entries, valueKey))
                {
                    entries.Add(key, candidate);
                    return candidate;
                }

                candidate = candidate == ushort.MaxValue
                    ? (ushort)1
                    : (ushort)(candidate + 1);
            }

            throw new InvalidOperationException(
                $"No native PubSub identifier remains in scope '{scope}'.");
        }

        private static Dictionary<string, string> CreateReverse(
            Dictionary<string, ushort> entries)
        {
            var reverse = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                var identity = ParseKey(entry.Key);
                if (!reverse.TryAdd(CreateValueKey(identity.Scope, entry.Value), identity.Id))
                {
                    throw new InvalidDataException(
                        "Multiple public identifiers map to the same native PubSub identifier.");
                }
            }
            return reverse;
        }

        private static bool ContainsValue(Dictionary<string, ushort> entries, string valueKey)
        {
            foreach (var entry in entries)
            {
                var identity = ParseKey(entry.Key);
                if (CreateValueKey(identity.Scope, entry.Value) == valueKey)
                {
                    return true;
                }
            }
            return false;
        }

        private static PubSubIdentityRegistryEntry CreateEntry(string key, ushort value)
        {
            var identity = ParseKey(key);
            return new PubSubIdentityRegistryEntry
            {
                Scope = identity.Scope,
                Id = identity.Id,
                Value = value
            };
        }

        private static (string Scope, string Id) ParseKey(string key)
        {
            var separator = key.IndexOf('\u001f');
            return (key[..separator], key[(separator + 1)..]);
        }

        private static string CreateKey(string scope, string id)
        {
            return scope + '\u001f' + id;
        }

        private static string CreateValueKey(string scope, ushort id)
        {
            return scope + '\u001f' + id;
        }

        private static uint CreateCandidate(string key)
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;
            var hash = offset;
            foreach (var character in key)
            {
                hash ^= character;
                hash *= prime;
            }
            return hash;
        }

        private static void ValidateIdentity(string scope, string id)
        {
            if (string.IsNullOrWhiteSpace(scope))
            {
                throw new ArgumentException("The identity scope must not be empty.", nameof(scope));
            }
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("The public identifier must not be empty.", nameof(id));
            }
            if (scope.IndexOf('\u001f') >= 0 || id.IndexOf('\u001f') >= 0)
            {
                throw new ArgumentException(
                    "The identity scope and public identifier may not contain control separators.");
            }
        }

        private sealed class Transaction : IPubSubIdentityTransaction
        {
            public Transaction(PubSubIdentityRegistry owner,
                Dictionary<string, ushort> entries)
            {
                _owner = owner;
                Entries = entries;
            }

            public Dictionary<string, ushort> Entries { get; }

            public bool IsCompleted => _completed;

            public ushort GetOrAllocate(string scope, string id)
            {
                if (_completed)
                {
                    throw new InvalidOperationException("The identity transaction is complete.");
                }
                ValidateIdentity(scope, id);
                return _owner.Allocate(Entries, scope, id);
            }

            public ValueTask CommitAsync(CancellationToken cancellationToken = default)
            {
                if (_completed)
                {
                    throw new InvalidOperationException("The identity transaction is complete.");
                }
                return _owner.CommitAsync(this, cancellationToken);
            }

            public ValueTask DisposeAsync()
            {
                _owner.Rollback(this);
                return default;
            }

            public bool Complete()
            {
                if (_completed)
                {
                    return false;
                }
                _completed = true;
                return true;
            }

            private readonly PubSubIdentityRegistry _owner;
            private bool _completed;
        }

        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly IPubSubIdentityRegistryStore _store;
        private readonly Func<string, uint> _candidateFactory;
        private Dictionary<string, ushort> _entries = new(StringComparer.Ordinal);
        private Dictionary<string, string> _reverse = new(StringComparer.Ordinal);
        private bool _initialized;
    }

    internal sealed class FilePubSubIdentityRegistryStore : IPubSubIdentityRegistryStore
    {
        public FilePubSubIdentityRegistryStore(IOptions<PublisherOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var stateFile = options.Value.PublishedNodesFile;
            var directory = string.IsNullOrWhiteSpace(stateFile)
                ? Environment.CurrentDirectory
                : Path.GetDirectoryName(Path.GetFullPath(stateFile))
                    ?? Environment.CurrentDirectory;
            _path = Path.Combine(directory, "pubsub-identities.json");
        }

        internal FilePubSubIdentityRegistryStore(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public async ValueTask<PubSubIdentityRegistrySnapshot> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            var foundCandidate = false;
            Exception? lastError = null;
            foreach (var candidate in new[] { _path, _backupPath, _temporaryPath })
            {
                if (!File.Exists(candidate))
                {
                    continue;
                }
                foundCandidate = true;

                try
                {
                    await using var stream = File.OpenRead(candidate);
                    var snapshot = await JsonSerializer.DeserializeAsync(stream,
                        PubSubIdentityJsonContext.Default.PubSubIdentityRegistrySnapshot,
                        cancellationToken).ConfigureAwait(false);
                    if (snapshot is not null)
                    {
                        PubSubIdentityRegistry.ValidateSnapshot(snapshot);
                        return snapshot;
                    }
                    lastError = new JsonException(
                        "The persisted PubSub identity registry was null.");
                }
                catch (JsonException ex)
                {
                    // A completed atomic replacement may have left a durable
                    // backup. Continue recovery with that snapshot.
                    lastError = ex;
                }
                catch (InvalidDataException ex)
                {
                    lastError = ex;
                }
            }

            if (foundCandidate)
            {
                throw new InvalidDataException(
                    "No valid persisted PubSub identity registry snapshot was found.",
                    lastError);
            }
            return new PubSubIdentityRegistrySnapshot();
        }

        public async ValueTask SaveAsync(PubSubIdentityRegistrySnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            PubSubIdentityRegistry.ValidateSnapshot(snapshot);
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var completed = false;
            try
            {
                await using (var stream = new FileStream(_temporaryPath, FileMode.Create,
                    FileAccess.Write, FileShare.None, bufferSize: 4096,
                    FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await JsonSerializer.SerializeAsync(stream, snapshot,
                        PubSubIdentityJsonContext.Default.PubSubIdentityRegistrySnapshot,
                        cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    stream.Flush(flushToDisk: true);
                }
                if (File.Exists(_path))
                {
                    File.Replace(_temporaryPath, _path, _backupPath);
                }
                else
                {
                    File.Move(_temporaryPath, _path);
                }
                completed = true;
            }
            finally
            {
                if (completed && File.Exists(_temporaryPath))
                {
                    File.Delete(_temporaryPath);
                }
                if (completed && File.Exists(_backupPath))
                {
                    File.Delete(_backupPath);
                }
            }
        }

        private readonly string _path;
        private string _temporaryPath => _path + ".new";
        private string _backupPath => _path + ".bak";
    }

    [JsonSerializable(typeof(PubSubIdentityRegistrySnapshot))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    internal sealed partial class PubSubIdentityJsonContext : JsonSerializerContext
    {
    }
}
