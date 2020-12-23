// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Models {
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.App.Data;

    public abstract class IIoTItemsCollection<T> : ComponentBase, IDisposable where T : class {
        public PagedResult<T> Items { get; set; } = new PagedResult<T>();

        public bool IsLoading { get; set; }

        public bool IsOpen { get; set; }

        public string Status { get; set; }

        protected string _tableView = "visible";

        protected string _tableEmpty = "displayNone";

        protected IAsyncDisposable _events;

        public virtual async void Dispose() {
            if (_events != null) {
                await _events.DisposeAsync();
            }
        }

        protected override void OnInitialized() {
            IsLoading = true;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                await GetItems(false);
                IsLoading = false;
                CheckErrorOrEmpty();
                StateHasChanged();
                await SubscribeEvents();
            }
        }

        protected virtual Task GetItems(bool getNextPage = false) {
            return Task.CompletedTask;
        }

        protected abstract Task SubscribeEvents();

        /// <summary>
        /// More items should be loaded
        /// </summary>
        /// <returns></returns>
        protected async Task LoadMoreItems() {
            IsLoading = true;
            if (!string.IsNullOrEmpty(Items.ContinuationToken)) {
                await GetItems(true);
            }
            IsLoading = false;
            StateHasChanged();
        }

        /// <summary>
        /// Close the Drawer
        /// </summary>
        protected virtual void CloseDrawer() {
            IsOpen = false;
            StateHasChanged();
        }

        /// <summary>
        /// Check if there is an error
        /// </summary>
        protected void CheckErrorOrEmpty() {
            if (Items.Error != null) {
                _tableView = "hidden";
            }
            else if (Items.Results.Count == 0) {
                _tableEmpty = "displayBlock";
            }
            else {
                _tableEmpty = "displayNone";
            }
        }
    }
}
