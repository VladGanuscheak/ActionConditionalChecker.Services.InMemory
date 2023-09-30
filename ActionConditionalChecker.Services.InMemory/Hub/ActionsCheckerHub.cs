using System.Collections.Concurrent;

namespace ActionConditionalChecker.Services.InMemory.Hub
{
    public static class ActionsCheckerHub
    {
        public static BlockingCollection<object> Actions { get; set; } 
            = new BlockingCollection<object>();

        public static readonly object _lockForErase = new object();
    }
}
