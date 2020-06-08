// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;

    public partial class Publishers {
        [Parameter]
        public string Page { get; set; } = "1";

        private PagedResult<PublisherApiModel> _publisherList = new PagedResult<PublisherApiModel>();
        private PagedResult<PublisherApiModel> _pagedPublisherList = new PagedResult<PublisherApiModel>();
        private IAsyncDisposable _publisherEvent { get; set; }
        private string _tableView = "visible";
        private string _tableEmpty = "displayNone";

        /// <summary>
        /// Notify page change
        /// </summary>
        /// <param name="page"></param>
        public async Task PagerPageChangedAsync(int page) {
            CommonHelper.Spinner = "loader-big";
            StateHasChanged();
            _publisherList = CommonHelper.UpdatePage(RegistryHelper.GetPublisherListAsync, page, _publisherList, ref _pagedPublisherList, CommonHelper.PageLengthSmall);
            NavigationManager.NavigateTo(NavigationManager.BaseUri + "publishers/" + page);
            for (int i = 0; i < _pagedPublisherList.Results.Count; i++) {
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
                _publisherList = await RegistryHelper.GetPublisherListAsync();
                Page = "1";
                _pagedPublisherList = _publisherList.GetPaged(int.Parse(Page), CommonHelper.PageLengthSmall, _publisherList.Error);
                CommonHelper.Spinner = string.Empty;
                CommonHelper.CheckErrorOrEmpty(_pagedPublisherList, ref _tableView, ref _tableEmpty);
                StateHasChanged();

                _publisherEvent = await RegistryServiceEvents.SubscribePublisherEventsAsync(
                    ev => InvokeAsync(() => PublisherEvent(ev)));
            }
        }

        private Task PublisherEvent(PublisherEventApiModel ev) {
            _publisherList.Results.Update(ev);
            _pagedPublisherList = _publisherList.GetPaged(int.Parse(Page), CommonHelper.PageLengthSmall, _publisherList.Error);
            StateHasChanged();
            return Task.CompletedTask;
        }

        public async void Dispose() {
            if (_publisherEvent != null) {
                await _publisherEvent.DisposeAsync();
            }
        }
    }
}