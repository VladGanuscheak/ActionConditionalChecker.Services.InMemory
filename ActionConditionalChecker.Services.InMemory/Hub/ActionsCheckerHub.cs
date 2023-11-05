using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ActionConditionalChecker.Services.InMemory.Hub
{
    public static class ActionsCheckerHub
    {
        public static List<object> Actions { get; set; } 
            = new List<object>();

        public static readonly object _lockForErase = new object();
    }
}
