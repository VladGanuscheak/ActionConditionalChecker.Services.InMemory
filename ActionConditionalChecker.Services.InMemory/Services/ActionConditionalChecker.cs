using ActionConditionalChecker.Contracts;
using ActionConditionalChecker.Contracts.AccessCondition;
using ActionConditionalChecker.Contracts.ActionInfo;
using OperationResult;
using System;
using System.Linq;
using ActionConditionalChecker.Services.InMemory.Hub;
using Result = OperationResult;
using ActionConditionalChecker.Services.InMemory.Exceptions;

namespace ActionConditionalChecker.Services.InMemory
{
    public class ActionConditionalChecker : IActionConditionalChecker
    {
        /// <inheritdoc/>
        public OperationResult<bool> CanExecute<TRequest>(AccessCondition<TRequest> accessCondition)
        {
            var now = DateTime.UtcNow;

            var hasBlockingAction = ActionsCheckerHub.Actions
                .Where(x => x.GetType().Equals(typeof(AccessCondition<TRequest>)))
                .Select(x => (AccessCondition<TRequest>)x)
                .Any(x => (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now)) && x.Predicate(accessCondition.Request));

            return Result.OperationResult.Succeeded(!hasBlockingAction);
        }

        /// <inheritdoc/>
        public void EnsureCanExecute<TRequest>(AccessCondition<TRequest> accessCondition)
        {
            var now = DateTime.UtcNow;

            var blockingAction = ActionsCheckerHub.Actions
                    .Where(x => x.GetType().Equals(typeof(AccessCondition<TRequest>)))
                    .Select(x => (AccessCondition<TRequest>)x)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now)) &&
                        x.Predicate(accessCondition.Request))
                    .FirstOrDefault();

            if (blockingAction != null)
            {
                throw new LockedResourceException<TRequest>(blockingAction);
            }
        }

        /// <inheritdoc/>
        public OperationResult<ActionState> Execute<TRequest>(ActionInfo<TRequest> actionInfo)
        {
            lock (actionInfo.Lock) 
            {
                var now = DateTime.UtcNow;

                var blockingAction = ActionsCheckerHub.Actions
                    .Where(x => x.GetType().Equals(typeof(AccessCondition<TRequest>)))
                    .Select(x => (AccessCondition<TRequest>)x)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now)) &&
                        x.Predicate(actionInfo.AccessCondition.Request))
                    .FirstOrDefault();

                if (blockingAction != null)
                {
                    return OperationResult<ActionState>.Failed()
                        .WithArgument(nameof(blockingAction.ExpiresAtUtc), blockingAction.ExpiresAtUtc)
                        .WithArgument(nameof(blockingAction.WaitTillActionCompletion), blockingAction.WaitTillActionCompletion);
                }

                if (!ActionsCheckerHub.Actions.TryAdd(actionInfo.AccessCondition))
                {
                    return OperationResult<ActionState>.Failed()
                        .WithMessage("Could not register the action info!");
                }
            }

            try
            {
                actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);
                return OperationResult<ActionState>.Succeeded(
                    new ActionState(
                        actionInfo.AccessCondition.ExpiresAtUtc, 
                        actionInfo.AccessCondition.WaitTillActionCompletion));
            }
            catch(Exception exception)
            {
                return OperationResult<ActionState>.Failed()
                    .WithError(exception);
            }
        }

        /// <inheritdoc/>
        public OperationResult<Tuple<ActionState, TResponse>> Execute<TRequest, TResponse>(ActionInfo<TRequest, TResponse> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                var blockingAction = ActionsCheckerHub.Actions
                    .Where(x => x.GetType().Equals(typeof(AccessCondition<TRequest>)))
                    .Select(x => (AccessCondition<TRequest>)x)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now)) &&
                        x.Predicate(actionInfo.AccessCondition.Request))
                    .FirstOrDefault();

                if (blockingAction != null)
                {
                    return OperationResult<Tuple<ActionState, TResponse>>.Failed()
                        .WithArgument(nameof(blockingAction.ExpiresAtUtc), blockingAction.ExpiresAtUtc)
                        .WithArgument(nameof(blockingAction.WaitTillActionCompletion), blockingAction.WaitTillActionCompletion);
                }

                if (!ActionsCheckerHub.Actions.TryAdd(actionInfo.AccessCondition))
                {
                    return OperationResult<Tuple<ActionState, TResponse>>.Failed()
                        .WithMessage("Could not register the action info!");
                }
            }

            try
            {
                var response = actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);
                return Result.OperationResult.Succeeded(
                    new Tuple<ActionState, TResponse>(
                    new ActionState(
                        actionInfo.AccessCondition.ExpiresAtUtc,
                        actionInfo.AccessCondition.WaitTillActionCompletion), 
                    response));
            }
            catch (Exception exception)
            {
                return OperationResult<Tuple<ActionState, TResponse>>.Failed()
                    .WithError(exception);
            }
        }

        /// <inheritdoc/>
        public OperationResult<ActionState> ExecuteOrSkip<TRequest>(ActionInfo<TRequest> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                var blockingAction = ActionsCheckerHub.Actions
                    .Where(x => x.GetType().Equals(typeof(AccessCondition<TRequest>)))
                    .Select(x => (AccessCondition<TRequest>)x)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now)) &&
                        x.Predicate(actionInfo.AccessCondition.Request))
                    .FirstOrDefault();

                if (blockingAction != null)
                {
                    return OperationResult<ActionState>.Succeeded(null)
                        .WithMessage("Skipped!");
                }

                if (!ActionsCheckerHub.Actions.TryAdd(actionInfo.AccessCondition))
                {
                    return OperationResult<ActionState>.Failed()
                        .WithMessage("Could not register the action info!");
                }
            }

            try
            {
                actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);
                return OperationResult<ActionState>.Succeeded(
                    new ActionState(
                        actionInfo.AccessCondition.ExpiresAtUtc,
                        actionInfo.AccessCondition.WaitTillActionCompletion));
            }
            catch (Exception exception)
            {
                return OperationResult<ActionState>.Failed()
                    .WithError(exception);
            }
        }

        /// <inheritdoc/>
        public OperationResult<Tuple<ActionState, TResponse>> ExecuteOrSkip<TRequest, TResponse>(ActionInfo<TRequest, TResponse> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                var blockingAction = ActionsCheckerHub.Actions
                    .Where(x => x.GetType().Equals(typeof(AccessCondition<TRequest>)))
                    .Select(x => (AccessCondition<TRequest>)x)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now)) &&
                        x.Predicate(actionInfo.AccessCondition.Request))
                    .FirstOrDefault();

                if (blockingAction != null)
                {
                    return OperationResult<Tuple<ActionState, TResponse>>
                        .Succeeded(null)
                        .WithMessage("Skipped!");
                }

                if (!ActionsCheckerHub.Actions.TryAdd(actionInfo.AccessCondition))
                {
                    return OperationResult<Tuple<ActionState, TResponse>>.Failed()
                        .WithMessage("Could not register the action info!");
                }
            }

            try
            {
                var response = actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);
                return Result.OperationResult.Succeeded(
                    new Tuple<ActionState, TResponse>(
                    new ActionState(
                        actionInfo.AccessCondition.ExpiresAtUtc,
                        actionInfo.AccessCondition.WaitTillActionCompletion),
                    response));
            }
            catch (Exception exception)
            {
                return OperationResult<Tuple<ActionState, TResponse>>.Failed()
                    .WithError(exception);
            }
        }

        /// <inheritdoc/>
        public OperationResult<ActionState> ExecuteOrThrowException<TRequest>(ActionInfo<TRequest> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                var blockingAction = ActionsCheckerHub.Actions
                    .Where(x => x.GetType().Equals(typeof(AccessCondition<TRequest>)))
                    .Select(x => (AccessCondition<TRequest>)x)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now)) &&
                        x.Predicate(actionInfo.AccessCondition.Request))
                    .FirstOrDefault();

                if (blockingAction != null)
                {
                    throw new LockedResourceException<TRequest>(blockingAction);
                }

                if (!ActionsCheckerHub.Actions.TryAdd(actionInfo.AccessCondition))
                {
                    return OperationResult<ActionState>.Failed()
                        .WithMessage("Could not register the action info!");
                }
            }

            try
            {
                actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);
                return OperationResult<ActionState>.Succeeded(
                    new ActionState(
                        actionInfo.AccessCondition.ExpiresAtUtc,
                        actionInfo.AccessCondition.WaitTillActionCompletion));
            }
            catch (Exception exception)
            {
                return OperationResult<ActionState>.Failed()
                    .WithError(exception);
            }
        }

        /// <inheritdoc/>
        public OperationResult<Tuple<ActionState, TResponse>> ExecuteOrThrowException<TRequest, TResponse>(ActionInfo<TRequest, TResponse> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                var blockingAction = ActionsCheckerHub.Actions
                    .Where(x => x.GetType().Equals(typeof(AccessCondition<TRequest>)))
                    .Select(x => (AccessCondition<TRequest>)x)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now)) &&
                        x.Predicate(actionInfo.AccessCondition.Request))
                    .FirstOrDefault();

                if (blockingAction != null)
                {
                    throw new LockedResourceException<TRequest>(blockingAction);
                }

                if (!ActionsCheckerHub.Actions.TryAdd(actionInfo.AccessCondition))
                {
                    return OperationResult<Tuple<ActionState, TResponse>>.Failed()
                        .WithMessage("Could not register the action info!");
                }
            }

            try
            {
                var response = actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);
                return Result.OperationResult.Succeeded(
                    new Tuple<ActionState, TResponse>(
                    new ActionState(
                        actionInfo.AccessCondition.ExpiresAtUtc,
                        actionInfo.AccessCondition.WaitTillActionCompletion),
                    response));
            }
            catch (Exception exception)
            {
                return OperationResult<Tuple<ActionState, TResponse>>.Failed()
                    .WithError(exception);
            }
        }
    }
}
