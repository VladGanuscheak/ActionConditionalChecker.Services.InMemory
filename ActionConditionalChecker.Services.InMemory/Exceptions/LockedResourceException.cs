using ActionConditionalChecker.Contracts.AccessCondition;
using ActionConditionalChecker.Contracts.Exceptions;
using System;

namespace ActionConditionalChecker.Services.InMemory.Exceptions
{
    /// <inheritdoc/>
    public class LockedResourceException<TRequest>(BaseAccessCondition<TRequest> accessCondition) : ActionConditionalException<TRequest>(accessCondition)
    {
        protected override string ConstructExceptionMessage(BaseAccessCondition<TRequest> accessCondition)
        {
            if (accessCondition.WaitTillActionCompletion)
            {
                return "The blocking action has not expired!";
            }

            if (accessCondition.ExpiresAtUtc.HasValue)
            {
                var secondsTillExpiration = (accessCondition.ExpiresAtUtc.Value - DateTime.UtcNow).TotalSeconds;

                if (secondsTillExpiration < 3)
                {
                    secondsTillExpiration = 3;
                }

                return $"The blocking action will expire aproximately after {secondsTillExpiration} seconds!";
            }

            return "The blocking action has not expired!";
        }
    }
}
