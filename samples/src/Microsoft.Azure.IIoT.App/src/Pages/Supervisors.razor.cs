// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages
{
    using global::Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.App.Extensions;
    using Microsoft.Azure.IIoT.App.Models;
    using System;
    using System.Globalization;
    using System.Threading.Tasks;

    public sealed partial class Supervisors
    {
        [Parameter]
        public string Page { get; set; } = "1";

        public string Status { get; set; }
        public bool IsOpen { get; set; }
        public string SupervisorId { get; set; }
        private PagedResult<SupervisorModel> SupervisorList { get; set; } =
            new PagedResult<SupervisorModel>();
        private PagedResult<SupervisorModel> _pagedSupervisorList =
            new();
        private IAsyncDisposable _supervisorEvent;
        private string _tableView = "visible";
        private string _tableEmpty = "displayNone";

        /// <summary>
        /// Notify page change
        /// </summary>
        /// <param name="page"></param>
        public async Task PagerPageChangedAsync(int page)
        {
            CommonHelper.Spinner = "loader-big";
            StateHasChanged();
            SupervisorList = CommonHelper.UpdatePage(RegistryHelper.GetSupervisorListAsync, page, SupervisorList, ref _pagedSupervisorList, CommonHelper.PageLength);
            NavigationManager.NavigateTo(NavigationManager.BaseUri + "supervisors/" + page);
            for (var i = 0; i < _pagedSupervisorList.Results.Count; i++)
            {
                _pagedSupervisorList.Results[i] = await RegistryService.GetSupervisorAsync(_pagedSupervisorList.Results[i].Id).ConfigureAwait(false);
            }
            CommonHelper.Spinner = string.Empty;
            StateHasChanged();
        }

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized()
        {
            CommonHelper.Spinner = "loader-big";
        }

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await UpdateSupervisorAsync().ConfigureAwait(false);
                CommonHelper.Spinner = string.Empty;
                CommonHelper.CheckErrorOrEmpty(_pagedSupervisorList, ref _tableView, ref _tableEmpty);
                StateHasChanged();

                _supervisorEvent = await RegistryServiceEvents.SubscribeSupervisorEventsAsync(
                    async data => await InvokeAsync(() => SupervisorEventAsync(data)).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Open then Drawer
        /// </summary>
        /// <param name="OpenDrawer"></param>
        private void OpenDrawer(string supervisorId)
        {
            IsOpen = true;
            SupervisorId = supervisorId;
        }

        /// <summary>
        /// Close the Drawer
        /// </summary>
        private void CloseDrawer()
        {
            IsOpen = false;
            StateHasChanged();
        }

        /// <summary>
        /// action on Supervisor Event
        /// </summary>
        /// <param name="ev"></param>
        private Task SupervisorEventAsync(SupervisorEventModel ev)
        {
            SupervisorList.Results.Update(ev);
            _pagedSupervisorList = SupervisorList.GetPaged(int.Parse(Page, CultureInfo.InvariantCulture), CommonHelper.PageLength, SupervisorList.Error);
            StateHasChanged();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Update Supervisor list
        /// </summary>
        private async Task UpdateSupervisorAsync()
        {
            SupervisorList = await RegistryHelper.GetSupervisorListAsync().ConfigureAwait(false);
            Page = "1";
            _pagedSupervisorList = SupervisorList.GetPaged(int.Parse(Page, CultureInfo.InvariantCulture), CommonHelper.PageLength, SupervisorList.Error);
            CommonHelper.Spinner = "";
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_supervisorEvent != null)
            {
                await _supervisorEvent.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
