using ActionConditionalChecker.Contracts.AccessCondition;
using ActionConditionalChecker.Contracts.ActionInfo;
using System;

namespace ActionConditionalChecker.Services.Web.Models
{
    public class RequestAActionInfo(Action<RequestA> action, AccessCondition<RequestA> accessCondition, object actionLock) 
        : ActionInfo<RequestA>(action, accessCondition, actionLock)
    {
    }
}
