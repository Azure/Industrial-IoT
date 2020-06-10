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

    public partial class Supervisors {

        [Parameter]
        public string Page { get; set; } = "1";

        public string Status { get; set; }
        public bool IsOpen { get; set; } = false;
        public string SupervisorId { get; set; }
        private PagedResult<SupervisorApiModel> SupervisorList { get; set; } =
            new PagedResult<SupervisorApiModel>();
        private PagedResult<SupervisorApiModel> _pagedSupervisorList =
            new PagedResult<SupervisorApiModel>();
        private IAsyncDisposable _supervisorEvent;
        private string _tableView = "visible";
        private string _tableEmpty = "displayNone";

        /// <summary>
        /// Notify page change
        /// </summary>
        /// <param name="page"></param>
        public async Task PagerPageChangedAsync(int page) {
            CommonHelper.Spinner = "loader-big";
            StateHasChanged();
            SupervisorList = CommonHelper.UpdatePage(RegistryHelper.GetSupervisorListAsync, page, SupervisorList, ref _pagedSupervisorList, CommonHelper.PageLength);
            NavigationManager.NavigateTo(NavigationManager.BaseUri + "supervisors/" + page);
            for (int i = 0; i < _pagedSupervisorList.Results.Count; i++) {
                _pagedSupervisorList.Results[i] = await RegistryService.GetSupervisorAsync(_pagedSupervisorList.Results[i].Id);
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
                await UpdateSupervisorAsync();
                CommonHelper.Spinner = string.Empty;
                CommonHelper.CheckErrorOrEmpty<SupervisorApiModel>(_pagedSupervisorList, ref _tableView, ref _tableEmpty);
                StateHasChanged();

                _supervisorEvent = await RegistryServiceEvents.SubscribeSupervisorEventsAsync(
                    async data => {
                        await InvokeAsync(() => SupervisorEvent(data));
                    });
            }
        }

        // <summary>
        /// Open then Drawer
        /// </summary>
        /// <param name="OpenDrawer"></param>
        private void OpenDrawer(string supervisorId) {
            IsOpen = true;
            SupervisorId = supervisorId;
        }

        /// <summary>
        /// Close the Drawer
        /// </summary>
        private void CloseDrawer() {
            IsOpen = false;
            StateHasChanged();
        }

        /// <summary>
        /// Reset Supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        private async Task ResetSupervisorUIAsync(string supervisorId) {
            Status = await RegistryHelper.ResetSupervisorAsync(supervisorId);
        }

        /// <summary>
        /// action on Supervisor Event
        /// </summary>
        /// <param name="ev"></param>
        private Task SupervisorEvent(SupervisorEventApiModel ev) {
            SupervisorList.Results.Update(ev);
            _pagedSupervisorList = SupervisorList.GetPaged(int.Parse(Page), CommonHelper.PageLength, SupervisorList.Error);
            StateHasChanged();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Update Supervisor list
        /// </summary>
        private async Task UpdateSupervisorAsync() {
            SupervisorList = await RegistryHelper.GetSupervisorListAsync();
            Page = "1";
            _pagedSupervisorList = SupervisorList.GetPaged(int.Parse(Page), CommonHelper.PageLength, SupervisorList.Error);
            CommonHelper.Spinner = "";
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public async void Dispose() {
            if (_supervisorEvent != null) {
                await _supervisorEvent.DisposeAsync();
            }
        }
    }
}