using ActionConditionalChecker.Contracts.AccessCondition;
using ActionConditionalChecker.Contracts.ActionInfo;
using System;
using System.Threading.Tasks;

namespace ActionConditionalChecker.Services.Web.Models
{
    public class AsyncRequestAActionInfo : AsyncActionInfo<RequestA>
    {
        public AsyncRequestAActionInfo(Func<RequestA, Task> action, AsyncAccessCondition<RequestA> accessCondition, object actionLock) 
            : base(action, accessCondition, actionLock)
        {
        }
    }
}
