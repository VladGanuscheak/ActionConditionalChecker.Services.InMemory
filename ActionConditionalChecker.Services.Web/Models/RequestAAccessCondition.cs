using ActionConditionalChecker.Contracts.AccessCondition;
using System;

namespace ActionConditionalChecker.Services.Web.Models
{
    public class RequestAAccessCondition : AccessCondition<RequestA>
    {
        public RequestAAccessCondition(RequestA request, Func<RequestA, bool> predicate) : base(request, predicate)
        {
        }

        public RequestAAccessCondition(RequestA request, Func<RequestA, bool> predicate, DateTime expiresAtUtc) : base(request, predicate, expiresAtUtc)
        {
        }

        public RequestAAccessCondition(RequestA request, Func<RequestA, bool> predicate, TimeSpan timeSpan) : base(request, predicate, timeSpan)
        {
        }

        public RequestAAccessCondition(RequestA request, Func<RequestA, bool> predicate, DateTime expiresAtUtc, bool waitTillActionCompletion) : base(request, predicate, expiresAtUtc, waitTillActionCompletion)
        {
        }
    }
}
