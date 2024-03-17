using System.Collections.Generic;

namespace ActionConditionalChecker.Services.InMemory.Hub
{
    public static class ActionsCheckerHub
    {
        public static List<object> Actions { get; set; } 
            = [];

        public static readonly object _lockForErase = new();
    }
}
