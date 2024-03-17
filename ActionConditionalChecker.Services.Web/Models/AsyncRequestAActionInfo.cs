using ActionConditionalChecker.Contracts.AccessCondition;
using ActionConditionalChecker.Contracts.ActionInfo;
using System;
using System.Threading.Tasks;

namespace ActionConditionalChecker.Services.Web.Models
{
    public class AsyncRequestAActionInfo(Func<RequestA, Task> action, AsyncAccessCondition<RequestA> accessCondition, object actionLock) 
        : AsyncActionInfo<RequestA>(action, accessCondition, actionLock)
    {
    }
}
