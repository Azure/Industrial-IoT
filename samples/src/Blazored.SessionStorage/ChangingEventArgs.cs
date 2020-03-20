using System;

namespace Blazored.SessionStorage
{
    public class ChangingEventArgs : ChangedEventArgs
    {
        public bool Cancel { get; set; }
    }
}
