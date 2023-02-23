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
    using System.Linq;
    using System.Threading.Tasks;

    public sealed partial class Applications
    {
        [Parameter]
        public string Page { get; set; } = "1";

        public string Status { get; set; }
        public bool IsOpen { get; set; }
        public ApplicationInfoModel ApplicationData { get; set; }
        private PagedResult<ApplicationInfoModel> ApplicationList { get; set; } =
            new PagedResult<ApplicationInfoModel>();
        private PagedResult<ApplicationInfoModel> _pagedApplicationList =
            new();
        private IAsyncDisposable _applicationEvent;
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
            ApplicationList = CommonHelper.UpdatePage(RegistryHelper.GetApplicationListAsync, page, ApplicationList, ref _pagedApplicationList, CommonHelper.PageLength);
            NavigationManager.NavigateTo(NavigationManager.BaseUri + "applications/" + page);
            for (var i = 0; i < _pagedApplicationList.Results.Count; i++)
            {
                _pagedApplicationList.Results[i] = (await RegistryService.GetApplicationAsync(_pagedApplicationList.Results[i].ApplicationId).ConfigureAwait(false)).Application;
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
                await UpdateApplicationAsync().ConfigureAwait(false);
                StateHasChanged();

                _applicationEvent = await RegistryServiceEvents.SubscribeApplicationEventsAsync(
                    async data => await InvokeAsync(() => ApplicationEventAsync(data)).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Unregister application and remove it from UI
        /// </summary>
        /// <param name="applicationId"></param>
        private async Task UnregisterApplicationUIAsync(string applicationId)
        {
            var index = ApplicationList.Results.ConvertAll(t => t.ApplicationId).IndexOf(applicationId);
            ApplicationList.Results.RemoveAt(index);
            _pagedApplicationList = ApplicationList.GetPaged(int.Parse(Page, CultureInfo.InvariantCulture),
                CommonHelper.PageLength, ApplicationList.Error);
            StateHasChanged();

            Status = await RegistryHelper.UnregisterApplicationAsync(applicationId).ConfigureAwait(false);
        }

        /// <summary>
        /// Open then Drawer
        /// </summary>
        /// <param name="OpenDrawer"></param>
        private void OpenDrawer(ApplicationInfoModel application)
        {
            IsOpen = true;
            ApplicationData = application;
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
        /// Action on ApplicationEvent
        /// </summary>
        /// <param name="ev"></param>
        private Task ApplicationEventAsync(ApplicationEventModel ev)
        {
            ApplicationList.Results.Update(ev);
            _pagedApplicationList = ApplicationList.GetPaged(int.Parse(Page, CultureInfo.InvariantCulture),
                CommonHelper.PageLength, ApplicationList.Error);
            CommonHelper.CheckErrorOrEmpty(_pagedApplicationList, ref _tableView, ref _tableEmpty);
            StateHasChanged();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Update application list
        /// </summary>
        private async Task UpdateApplicationAsync()
        {
            ApplicationList = await RegistryHelper.GetApplicationListAsync().ConfigureAwait(false);
            Page = "1";
            _pagedApplicationList = ApplicationList.GetPaged(int.Parse(Page, CultureInfo.InvariantCulture),
                CommonHelper.PageLength, ApplicationList.Error);
            CommonHelper.CheckErrorOrEmpty(_pagedApplicationList, ref _tableView, ref _tableEmpty);
            CommonHelper.Spinner = string.Empty;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_applicationEvent != null)
            {
                await _applicationEvent.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
