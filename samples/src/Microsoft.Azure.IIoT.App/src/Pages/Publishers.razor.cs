// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.App.Extensions;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using System;
    using System.Threading.Tasks;

    public partial class Publishers {
        [Parameter]
        public string Page { get; set; } = "1";

        private PagedResult<PublisherApiModel> PublisherList { get; set; } =
            new PagedResult<PublisherApiModel>();
        private PagedResult<PublisherApiModel> _pagedPublisherList =
            new();
        private IAsyncDisposable _publisherEvent;
        private string _tableView = "visible";
        private string _tableEmpty = "displayNone";

        public bool IsOpen { get; set; } = false;

        /// <summary>
        /// Notify page change
        /// </summary>
        /// <param name="page"></param>
        public async Task PagerPageChangedAsync(int page) {
            CommonHelper.Spinner = "loader-big";
            StateHasChanged();
            PublisherList = CommonHelper.UpdatePage(RegistryHelper.GetPublisherListAsync, page, PublisherList, ref _pagedPublisherList, CommonHelper.PageLengthSmall);
            NavigationManager.NavigateTo(NavigationManager.BaseUri + "publishers/" + page);
            for (var i = 0; i < _pagedPublisherList.Results.Count; i++) {
                _pagedPublisherList.Results[i] = await RegistryService.GetPublisherAsync(_pagedPublisherList.Results[i].Id);
            }
            CommonHelper.Spinner = string.Empty;
            StateHasChanged();
        }

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized() {
            CommonHelper.Spinner = "loader-big";
        }

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                PublisherList = await RegistryHelper.GetPublisherListAsync();
                Page = "1";
                _pagedPublisherList = PublisherList.GetPaged(int.Parse(Page), CommonHelper.PageLengthSmall, PublisherList.Error);
                CommonHelper.Spinner = string.Empty;
                CommonHelper.CheckErrorOrEmpty(_pagedPublisherList, ref _tableView, ref _tableEmpty);
                StateHasChanged();

                _publisherEvent = await RegistryServiceEvents.SubscribePublisherEventsAsync(
                    ev => InvokeAsync(() => PublisherEvent(ev)));
            }
        }

        private Task PublisherEvent(PublisherEventApiModel ev) {
            _pagedPublisherList = PublisherList.GetPaged(int.Parse(Page), CommonHelper.PageLengthSmall, PublisherList.Error);
            StateHasChanged();
            return Task.CompletedTask;
        }

        public async void Dispose() {
            if (_publisherEvent != null) {
                await _publisherEvent.DisposeAsync();
            }
        }

        private bool IsTimeIntervalSet(TimeSpan? interval) {
            return interval != null && interval.Value != TimeSpan.MinValue;
        }

        /// <summary>
        /// Open then Drawer
        /// </summary>
        /// <param name="OpenDrawer"></param>
        private void OpenDrawer(PublisherApiModel publisherModel) {
            IsOpen = true;
        }

        /// <summary>
        /// Close the Drawer
        /// </summary>
        private void CloseDrawer() {
            IsOpen = false;
            StateHasChanged();
        }
    }
}