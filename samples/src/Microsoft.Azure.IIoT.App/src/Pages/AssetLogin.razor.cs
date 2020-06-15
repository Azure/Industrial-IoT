// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.App.Models;

    public partial class AssetLogin {
        public CredentialModel Credential { get; set; } = new CredentialModel();
        private bool ShowLogin { get; set; } = true;

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                // By this stage we know the client has connected back to the server, and
                // browser services are available. So if we didn't load the data earlier,
                // we should do so now, then trigger a new render
                ShowLogin = !await CheckLoginAsync();
                StateHasChanged();
            }
        }

        /// <summary>
        /// LoadAsync
        /// </summary>
        public async Task LoadAsync() {
            Credential = await GetSecureItemAsync<CredentialModel>(CommonHelper.CredentialKey);
        }

        /// <summary>
        /// CheckLoginAsync
        /// </summary>
        /// <param name="bool"></param>
        public async Task<bool> CheckLoginAsync() {
            bool isLoggedIn = false;
            await LoadAsync();
            if (Credential != null) {
                if (!string.IsNullOrEmpty(Credential.Username) && !string.IsNullOrEmpty(Credential.Password)) {
                    isLoggedIn = true;
                }
            }
            else {
                Credential = new CredentialModel();
            }

            return isLoggedIn;
        }

        /// <summary>
        /// SignOut
        /// </summary>
        public async Task SignOutAsync() {
            await RemoveSecureItemAsync(CommonHelper.CredentialKey);
            ShowLogin = !await CheckLoginAsync();
            StateHasChanged();
        }

        /// <summary>
        /// SignIn
        /// </summary>
        public async Task SignInAsync() {
            await SetSecureItemAsync(CommonHelper.CredentialKey, Credential);
            ShowLogin = !await CheckLoginAsync();
            StateHasChanged();
        }


        public async Task<T> GetSecureItemAsync<T>(string key) {
            var serializedProtectedData = await sessionStorage.GetItemAsync<string>(key);
            return secureData.UnprotectDeserialize<T>(serializedProtectedData);
        }

        public async Task SetSecureItemAsync<T>(string key, T data) {
            var serializedProtectedData = secureData.ProtectSerialize(data);
            await sessionStorage.SetItemAsync(key, serializedProtectedData);
        }

        public async Task RemoveSecureItemAsync(string key) {
            await sessionStorage.RemoveItemAsync(key);
        }
    }
}