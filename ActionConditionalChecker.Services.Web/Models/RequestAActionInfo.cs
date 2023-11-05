using ActionConditionalChecker.Contracts.AccessCondition;
using ActionConditionalChecker.Contracts.ActionInfo;
using System;

namespace ActionConditionalChecker.Services.Web.Models
{
    public class RequestAActionInfo : ActionInfo<RequestA>
    {
        public RequestAActionInfo(Action<RequestA> action, AccessCondition<RequestA> accessCondition, object actionLock) 
            : base(action, accessCondition, actionLock)
        {
        }
    }
}
