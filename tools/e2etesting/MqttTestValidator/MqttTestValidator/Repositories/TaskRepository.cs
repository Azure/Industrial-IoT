// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MqttTestValidator.Repositories {
    using MqttTestValidator.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    internal sealed class TaskRepository : ITaskRepository {

        private readonly IDictionary<ulong, IVerificationTask> _store = new Dictionary<ulong, IVerificationTask>(20);

        /// <inheritdoc />
        public bool Contains(ulong id) {
            return _store.ContainsKey(id);
        }

        /// <inheritdoc />
        public void Add(IVerificationTask entity) {
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));

            if (_store.ContainsKey(entity.Id)) {
                throw new ArgumentException("Entity already exist");
            }

            _store.Add(entity.Id, entity);
        }

        /// <inheritdoc />
        public void AddRange(IEnumerable<IVerificationTask> entities) {
            ArgumentNullException.ThrowIfNull(entities, nameof(entities));

            foreach (var entity in entities) {
                Add(entity);
            }
        }

        /// <inheritdoc />
        public IEnumerable<IVerificationTask> Find(Expression<Func<IVerificationTask, bool>> expression) {
            var filter = expression.Compile();
            return _store.Where(kvp => filter(kvp.Value)).Select(kvp => kvp.Value);
        }

        /// <inheritdoc />
        public IEnumerable<IVerificationTask> GetAll() {
            return _store.Values;
        }

        /// <inheritdoc />
        public IVerificationTask GetById(ulong id) {
            if (!_store.ContainsKey(id)) {
                throw new ArgumentException("unkown task id");
            }

            return _store[id];
        }

        /// <inheritdoc />
        public void Remove(IVerificationTask entity) {
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));

            if (!_store.ContainsKey(entity.Id)) {
                throw new ArgumentException("unkown task id");
            }

            _store.Remove(entity.Id);
        }

        /// <inheritdoc />
        public void RemoveRange(IEnumerable<IVerificationTask> entities) {
            ArgumentNullException.ThrowIfNull(entities, nameof(entities));

            foreach (var entity in entities) {
                Remove(entity);
            }
        }
    }
}
