// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MqttTestValidator.Interfaces {
    using System.Linq.Expressions;

    public interface ITaskRepository {
        /// <summary>
        /// Check if id exist
        /// </summary>
        bool Contains(ulong id);
        /// <summary>
        /// Returns entity by ID
        /// </summary>
        IVerificationTask GetById(ulong id);
        /// <summary>
        /// Returns all entities
        /// </summary>
        IEnumerable<IVerificationTask> GetAll();
        /// <summary>
        /// Find entity by filter
        /// </summary>
        IEnumerable<IVerificationTask> Find(Expression<Func<IVerificationTask, bool>> expression);
        /// <summary>
        /// Add new entity
        /// </summary>
        void Add(IVerificationTask entity);
        /// <summary>
        /// Add new entitites
        /// </summary>
        void AddRange(IEnumerable<IVerificationTask> entities);
        /// <summary>
        /// Remove entity
        /// </summary>
        void Remove(IVerificationTask entity);
        /// <summary>
        /// Remove entities
        /// </summary>
        void RemoveRange(IEnumerable<IVerificationTask> entities);
    }
}
