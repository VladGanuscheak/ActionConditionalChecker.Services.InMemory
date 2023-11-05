using ActionConditionalChecker.Contracts.AccessCondition;
using System;
using System.Threading.Tasks;

namespace ActionConditionalChecker.Services.Web.Models
{
    public class AsyncRequestAAccessCondition : AsyncAccessCondition<RequestA>
    {
        public AsyncRequestAAccessCondition(RequestA request, Func<RequestA, Task<bool>> predicate) 
            : base(request, predicate)
        {
        }

        public AsyncRequestAAccessCondition(RequestA request, Func<RequestA, Task<bool>> predicate, DateTime expiresAtUtc) 
            : base(request, predicate, expiresAtUtc)
        {
        }

        public AsyncRequestAAccessCondition(RequestA request, Func<RequestA, Task<bool>> predicate, TimeSpan timeSpan) 
            : base(request, predicate, timeSpan)
        {
        }

        public AsyncRequestAAccessCondition(RequestA request, Func<RequestA, Task<bool>> predicate, DateTime expiresAtUtc, bool waitTillActionCompletion) 
            : base(request, predicate, expiresAtUtc, waitTillActionCompletion)
        {
        }
    }
}
