/* ========================================================================
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The async interface for a subscription.
    /// </summary>
    public partial class Subscription
    {
        /// <summary>
        /// Creates a subscription on the server and adds all monitored items.
        /// </summary>
        /// <param name="ct"></param>
        public async Task CreateAsync(CancellationToken ct = default)
        {
            VerifySubscriptionState(false);

            // create the subscription.
            var revisedMaxKeepAliveCount = m_keepAliveCount;
            var revisedLifetimeCount = m_lifetimeCount;

            AdjustCounts(ref revisedMaxKeepAliveCount, ref revisedLifetimeCount);

            var response = await m_session.CreateSubscriptionAsync(
                null,
                m_publishingInterval,
                revisedLifetimeCount,
                revisedMaxKeepAliveCount,
                m_maxNotificationsPerPublish,
                m_publishingEnabled,
                m_priority,
                ct).ConfigureAwait(false);

            CreateSubscription(
                response.SubscriptionId,
                response.RevisedPublishingInterval,
                response.RevisedMaxKeepAliveCount,
                response.RevisedLifetimeCount);

            await CreateItemsAsync(ct).ConfigureAwait(false);

            ChangesCompleted();
        }

        /// <summary>
        /// Deletes a subscription on the server.
        /// </summary>
        /// <param name="silent"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        public async Task DeleteAsync(bool silent, CancellationToken ct = default)
        {
            if (!silent)
            {
                VerifySubscriptionState(true);
            }

            // nothing to do if not created.
            if (!this.Created)
            {
                return;
            }

            try
            {
                lock (m_cache)
                {
                    ResetPublishTimerAndWorkerState();
                }

                // delete the subscription.
                UInt32Collection subscriptionIds = new uint[] { m_id };

                var response = await m_session.DeleteSubscriptionsAsync(
                    null,
                    subscriptionIds,
                    ct).ConfigureAwait(false);

                // validate response.
                ClientBase.ValidateResponse(response.Results, subscriptionIds);
                ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, subscriptionIds);

                if (StatusCode.IsBad(response.Results[0]))
                {
                    throw new ServiceResultException(
                        ClientBase.GetResult(response.Results[0], 0, response.DiagnosticInfos, response.ResponseHeader));
                }
            }

            // suppress exception if silent flag is set.
            catch (Exception e)
            {
                if (!silent)
                {
                    throw new ServiceResultException(e, StatusCodes.BadUnexpectedError);
                }
            }
            // always put object in disconnected state even if an error occurs.
            finally
            {
                DeleteSubscription();
            }

            ChangesCompleted();
        }

        /// <summary>
        /// Modifies a subscription on the server.
        /// </summary>
        /// <param name="ct"></param>
        public async Task ModifyAsync(CancellationToken ct = default)
        {
            VerifySubscriptionState(true);

            // modify the subscription.
            var revisedKeepAliveCount = m_keepAliveCount;
            var revisedLifetimeCounter = m_lifetimeCount;

            AdjustCounts(ref revisedKeepAliveCount, ref revisedLifetimeCounter);

            var response = await m_session.ModifySubscriptionAsync(
                null,
                m_id,
                m_publishingInterval,
                revisedLifetimeCounter,
                revisedKeepAliveCount,
                m_maxNotificationsPerPublish,
                m_priority,
                ct).ConfigureAwait(false);

            // update current state.
            ModifySubscription(
                response.RevisedPublishingInterval,
                response.RevisedMaxKeepAliveCount,
                response.RevisedLifetimeCount);

            ChangesCompleted();
        }

        /// <summary>
        /// Changes the publishing enabled state for the subscription.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        public async Task SetPublishingModeAsync(bool enabled, CancellationToken ct = default)
        {
            VerifySubscriptionState(true);

            // modify the subscription.
            UInt32Collection subscriptionIds = new uint[] { m_id };

            var response = await m_session.SetPublishingModeAsync(
                null,
                enabled,
                new uint[] { m_id },
                ct).ConfigureAwait(false);

            // validate response.
            ClientBase.ValidateResponse(response.Results, subscriptionIds);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, subscriptionIds);

            if (StatusCode.IsBad(response.Results[0]))
            {
                throw new ServiceResultException(
                    ClientBase.GetResult(response.Results[0], 0, response.DiagnosticInfos, response.ResponseHeader));
            }

            // update current state.
            m_currentPublishingEnabled = m_publishingEnabled = enabled;
            m_changeMask |= SubscriptionChangeMask.Modified;

            ChangesCompleted();
        }

        /// <summary>
        /// Applies any changes to the subscription items.
        /// </summary>
        /// <param name="ct"></param>
        public async Task ApplyChangesAsync(CancellationToken ct = default)
        {
            await DeleteItemsAsync(ct).ConfigureAwait(false);
            await ModifyItemsAsync(ct).ConfigureAwait(false);
            await CreateItemsAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates all items on the server that have not already been created.
        /// </summary>
        /// <param name="ct"></param>
        public async Task<IList<MonitoredItem>> CreateItemsAsync(CancellationToken ct = default)
        {
            List<MonitoredItem> itemsToCreate;
            var requestItems = PrepareItemsToCreate(out itemsToCreate);

            if (requestItems.Count == 0)
            {
                return itemsToCreate;
            }

            // create monitored items.
            var response = await m_session.CreateMonitoredItemsAsync(
                null,
                m_id,
                m_timestampsToReturn,
                requestItems,
                ct).ConfigureAwait(false);

            var results = response.Results;
            ClientBase.ValidateResponse(results, itemsToCreate);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, itemsToCreate);

            // update results.
            for (var ii = 0; ii < results.Count; ii++)
            {
                itemsToCreate[ii].SetCreateResult(requestItems[ii], results[ii], ii, response.DiagnosticInfos, response.ResponseHeader);
            }

            m_changeMask |= SubscriptionChangeMask.ItemsCreated;
            ChangesCompleted();

            // return the list of items affected by the change.
            return itemsToCreate;
        }

        /// <summary>
        /// Modifies all items that have been changed.
        /// </summary>
        /// <param name="ct"></param>
        public async Task<IList<MonitoredItem>> ModifyItemsAsync(CancellationToken ct = default)
        {
            VerifySubscriptionState(true);

            var requestItems = new MonitoredItemModifyRequestCollection();
            var itemsToModify = new List<MonitoredItem>();

            PrepareItemsToModify(requestItems, itemsToModify);

            if (requestItems.Count == 0)
            {
                return itemsToModify;
            }

            // modify the subscription.
            var response = await m_session.ModifyMonitoredItemsAsync(
                null,
                m_id,
                m_timestampsToReturn,
                requestItems,
                ct).ConfigureAwait(false);

            var results = response.Results;
            ClientBase.ValidateResponse(results, itemsToModify);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, itemsToModify);

            // update results.
            for (var ii = 0; ii < results.Count; ii++)
            {
                itemsToModify[ii].SetModifyResult(requestItems[ii], results[ii], ii, response.DiagnosticInfos, response.ResponseHeader);
            }

            m_changeMask |= SubscriptionChangeMask.ItemsModified;
            ChangesCompleted();

            // return the list of items affected by the change.
            return itemsToModify;
        }

        /// <summary>
        /// Deletes all items that have been marked for deletion.
        /// </summary>
        /// <param name="ct"></param>
        public async Task<IList<MonitoredItem>> DeleteItemsAsync(CancellationToken ct)
        {
            VerifySubscriptionState(true);

            if (m_deletedItems.Count == 0)
            {
                return new List<MonitoredItem>();
            }

            var itemsToDelete = m_deletedItems;
            m_deletedItems = new List<MonitoredItem>();

            var monitoredItemIds = new UInt32Collection();

            foreach (var monitoredItem in itemsToDelete)
            {
                monitoredItemIds.Add(monitoredItem.Status.Id);
            }

            var response = await m_session.DeleteMonitoredItemsAsync(
                null,
                m_id,
                monitoredItemIds,
                ct).ConfigureAwait(false);

            var results = response.Results;
            ClientBase.ValidateResponse(results, monitoredItemIds);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, monitoredItemIds);

            // update results.
            for (var ii = 0; ii < results.Count; ii++)
            {
                itemsToDelete[ii].SetDeleteResult(results[ii], ii, response.DiagnosticInfos, response.ResponseHeader);
            }

            m_changeMask |= SubscriptionChangeMask.ItemsDeleted;
            ChangesCompleted();

            // return the list of items affected by the change.
            return itemsToDelete;
        }

        /// <summary>
        /// Set monitoring mode of items.
        /// </summary>
        /// <param name="monitoringMode"></param>
        /// <param name="monitoredItems"></param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="monitoredItems"/> is <c>null</c>.</exception>
        public async Task<List<ServiceResult>> SetMonitoringModeAsync(
            MonitoringMode monitoringMode,
            IList<MonitoredItem> monitoredItems,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(monitoredItems);

            VerifySubscriptionState(true);

            if (monitoredItems.Count == 0)
            {
                return null;
            }

            // get list of items to update.
            var monitoredItemIds = new UInt32Collection();
            foreach (var monitoredItem in monitoredItems)
            {
                monitoredItemIds.Add(monitoredItem.Status.Id);
            }

            var response = await m_session.SetMonitoringModeAsync(
                null,
                m_id,
                monitoringMode,
                monitoredItemIds,
                ct).ConfigureAwait(false);

            var results = response.Results;
            ClientBase.ValidateResponse(results, monitoredItemIds);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, monitoredItemIds);

            // update results.
            var errors = new List<ServiceResult>();
            var noErrors = UpdateMonitoringMode(
                monitoredItems, errors, results,
                response.DiagnosticInfos, response.ResponseHeader,
                monitoringMode);

            // raise state changed event.
            m_changeMask |= SubscriptionChangeMask.ItemsModified;
            ChangesCompleted();

            // return null list if no errors occurred.
            if (noErrors)
            {
                return null;
            }

            return errors;
        }

        /// <summary>
        /// Tells the server to refresh all conditions being monitored by the subscription.
        /// </summary>
        /// <param name="ct"></param>
        public async Task ConditionRefreshAsync(CancellationToken ct = default)
        {
            VerifySubscriptionState(true);

            var methodsToCall = new CallMethodRequestCollection();
            methodsToCall.Add(new CallMethodRequest()
            {
                MethodId = MethodIds.ConditionType_ConditionRefresh,
                InputArguments = new VariantCollection() { new Variant(m_id) }
            });

            var response = await m_session.CallAsync(
                null,
                methodsToCall,
                ct).ConfigureAwait(false);
        }

    }
}
