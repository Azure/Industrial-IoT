﻿using Microsoft.JSInterop;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Blazored.SessionStorage
{
    public class SessionStorageService : ISessionStorageService, ISyncSessionStorageService
    {
        private readonly IJSRuntime _jSRuntime;
        private readonly IJSInProcessRuntime _jSInProcessRuntime;

        public SessionStorageService(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
            _jSInProcessRuntime = jSRuntime as IJSInProcessRuntime;
        }

        public async Task SetItemAsync(string key, object data)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var e = await RaiseOnChangingAsync(key, data);

            if (e.Cancel)
                return;

            await _jSRuntime.InvokeAsync<object>("sessionStorage.setItem", key, JsonSerializer.Serialize(data));

            RaiseOnChanged(key, e.OldValue, data);
        }

        public async Task<T> GetItemAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var serialisedData = await _jSRuntime.InvokeAsync<string>("sessionStorage.getItem", key);

            if (serialisedData == null)
                return default(T);

            return JsonSerializer.Deserialize<T>(serialisedData);
        }

        public async Task RemoveItemAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            await _jSRuntime.InvokeAsync<object>("sessionStorage.removeItem", key);
        }

        public async Task ClearAsync() => await _jSRuntime.InvokeAsync<object>("sessionStorage.clear");

        public async Task<int> LengthAsync() => await _jSRuntime.InvokeAsync<int>("eval", "sessionStorage.length");

        public async Task<string> KeyAsync(int index) => await _jSRuntime.InvokeAsync<string>("sessionStorage.key", index);

        public void SetItem(string key, object data)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (_jSInProcessRuntime == null)
                throw new InvalidOperationException("IJSInProcessRuntime not available");

            var e = RaiseOnChangingSync(key, data);

            if (e.Cancel)
                return;

            _jSInProcessRuntime.Invoke<object>("sessionStorage.setItem", key, JsonSerializer.Serialize(data));

            RaiseOnChanged(key, e.OldValue, data);
        }

        public T GetItem<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (_jSInProcessRuntime == null)
                throw new InvalidOperationException("IJSInProcessRuntime not available");

            var serialisedData = _jSInProcessRuntime.Invoke<string>("sessionStorage.getItem", key);

            if (serialisedData == null)
                return default(T);

            return JsonSerializer.Deserialize<T>(serialisedData);
        }

        public void RemoveItem(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
                
            if (_jSInProcessRuntime == null)
                throw new InvalidOperationException("IJSInProcessRuntime not available");

            _jSInProcessRuntime.Invoke<object>("sessionStorage.removeItem", key);
        }

        public void Clear()
        {
            if (_jSInProcessRuntime == null)
                throw new InvalidOperationException("IJSInProcessRuntime not available");

            _jSInProcessRuntime.Invoke<object>("sessionStorage.clear");
        }

        public int Length()
        {
            if (_jSInProcessRuntime == null)
                throw new InvalidOperationException("IJSInProcessRuntime not available");

            return _jSInProcessRuntime.Invoke<int>("eval", "sessionStorage.length");
        }

        public string Key(int index)
        {
            if (_jSInProcessRuntime == null)
                throw new InvalidOperationException("IJSInProcessRuntime not available");

            return _jSInProcessRuntime.Invoke<string>("sessionStorage.key", index);
        }

        public event EventHandler<ChangingEventArgs> Changing;
        private async Task<ChangingEventArgs> RaiseOnChangingAsync(string key, object data)
        {
            var e = new ChangingEventArgs
            {
                Key = key,
                OldValue = await GetItemAsync<object>(key),
                NewValue = data
            };

            Changing?.Invoke(this, e);

            return e;
        }

        private ChangingEventArgs RaiseOnChangingSync(string key, object data)
        {
            var e = new ChangingEventArgs
            {
                Key = key,
                OldValue = ((ISyncSessionStorageService)this).GetItem<object>(key),
                NewValue = data
            };

            Changing?.Invoke(this, e);

            return e;
        }

        public event EventHandler<ChangedEventArgs> Changed;
        private void RaiseOnChanged(string key, object oldValue, object data)
        {
            var e = new ChangedEventArgs
            {
                Key = key,
                OldValue = oldValue,
                NewValue = data
            };

            Changed?.Invoke(this, e);
        }
    }
}
