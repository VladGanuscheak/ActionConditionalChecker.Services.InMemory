using ActionConditionalChecker.Contracts;
using ActionConditionalChecker.Contracts.AccessCondition;
using ActionConditionalChecker.Contracts.ActionInfo;
using OperationResult;
using System;
using System.Linq;
using System.Threading.Tasks;
using ActionConditionalChecker.Services.InMemory.Hub;
using Result = OperationResult;
using ActionConditionalChecker.Services.InMemory.Exceptions;

namespace ActionConditionalChecker.Services.InMemory
{
    public class AsyncActionConditionalChecker : IAsyncActionConditionalChecker
    {
        /// <inheritdoc/>
        public async Task<OperationResult<bool>> CanExecuteAsync<TRequest>(AsyncAccessCondition<TRequest> accessCondition)
        {
            var now = DateTime.UtcNow;

            foreach (var action in ActionsCheckerHub.Actions
                .Select(x => x as AsyncAccessCondition<TRequest>)
                .Where(x => x != null && 
                    (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now))))
            {
                if (await action.Predicate(accessCondition.Request))
                {
                    return Result.OperationResult.Succeeded(false);
                }
            }

            return Result.OperationResult.Succeeded(true);
        }

        /// <inheritdoc/>
        public async Task EnsureCanExecuteAsync<TRequest>(AsyncAccessCondition<TRequest> accessCondition)
        {
            var now = DateTime.UtcNow;

            foreach (var action in ActionsCheckerHub.Actions
                .Select(x => x as AsyncAccessCondition<TRequest>)
                .Where(x => x != null &&
                    (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now))))
            {
                if (await action.Predicate(accessCondition.Request))
                {
                    throw new LockedResourceException<TRequest>(action);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<OperationResult<ActionState>> ExecuteAsync<TRequest>(AsyncActionInfo<TRequest> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                foreach(var action in ActionsCheckerHub.Actions
                    .Select(x => x as AsyncAccessCondition<TRequest>)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now))))
                {
                    if (action.Predicate(actionInfo.AccessCondition.Request).Result)
                    {
                        return OperationResult<ActionState>.Failed()
                            .WithArgument(nameof(action.ExpiresAtUtc), action.ExpiresAtUtc)
                            .WithArgument(nameof(action.WaitTillActionCompletion), action.WaitTillActionCompletion);
                    }
                }

                ActionsCheckerHub.Actions.Add(actionInfo.AccessCondition);
            }

            try
            {
                await actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);

                if (actionInfo.AccessCondition.WaitTillActionCompletion)
                {
                    lock (actionInfo.Lock)
                    {
                        ActionsCheckerHub.Actions.Remove(actionInfo.AccessCondition);
                    }
                }

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
        public async Task<OperationResult<Tuple<ActionState, TResponse>>> ExecuteAsync<TRequest, TResponse>(AsyncActionInfo<TRequest, TResponse> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                foreach(var action in ActionsCheckerHub.Actions
                    .Select(x => x as AsyncAccessCondition<TRequest>)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now))))
                {
                    if (action.Predicate(actionInfo.AccessCondition.Request).Result)
                    {
                        return OperationResult<Tuple<ActionState, TResponse>>.Failed()
                            .WithArgument(nameof(action.ExpiresAtUtc), action.ExpiresAtUtc)
                            .WithArgument(nameof(action.WaitTillActionCompletion), action.WaitTillActionCompletion);
                    }
                }

                ActionsCheckerHub.Actions.Add(actionInfo.AccessCondition);
            }

            try
            {
                var response = await actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);

                if (actionInfo.AccessCondition.WaitTillActionCompletion)
                {
                    lock (actionInfo.Lock)
                    {
                        ActionsCheckerHub.Actions.Remove(actionInfo.AccessCondition);
                    }
                }

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
        public async Task<OperationResult<ActionState>> ExecuteOrSkipAsync<TRequest>(AsyncActionInfo<TRequest> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                foreach(var action in ActionsCheckerHub.Actions
                    .Select(x => x as AsyncAccessCondition<TRequest>)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now))))
                {
                    if (action != null)
                    {
                        return OperationResult<ActionState>.Succeeded(null)
                            .WithMessage("Skipped!");
                    }

                    ActionsCheckerHub.Actions.Add(actionInfo.AccessCondition);
                }
            }

            try
            {
                await actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);

                if (actionInfo.AccessCondition.WaitTillActionCompletion)
                {
                    lock (actionInfo.Lock)
                    {
                        ActionsCheckerHub.Actions.Remove(actionInfo.AccessCondition);
                    }
                }

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
        public async Task<OperationResult<Tuple<ActionState, TResponse>>> ExecuteOrSkipAsync<TRequest, TResponse>(AsyncActionInfo<TRequest, TResponse> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                foreach(var action in ActionsCheckerHub.Actions
                    .Select(x => x as AsyncAccessCondition<TRequest>)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now))))
                {
                    if (action.Predicate(actionInfo.AccessCondition.Request).Result)
                    {
                        return OperationResult<Tuple<ActionState, TResponse>>
                            .Succeeded(null)
                            .WithMessage("Skipped!");
                    }
                }

                ActionsCheckerHub.Actions.Add(actionInfo.AccessCondition);
            }

            try
            {
                var response = await actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);

                if (actionInfo.AccessCondition.WaitTillActionCompletion)
                {
                    lock (actionInfo.Lock)
                    {
                        ActionsCheckerHub.Actions.Remove(actionInfo.AccessCondition);
                    }
                }

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
        public async Task<OperationResult<ActionState>> ExecuteOrThrowExceptionAsync<TRequest>(AsyncActionInfo<TRequest> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                foreach(var action in ActionsCheckerHub.Actions
                    .Select(x => x as AsyncAccessCondition<TRequest>)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now))))
                {
                    if (action.Predicate(actionInfo.AccessCondition.Request).Result)
                    {
                        throw new LockedResourceException<TRequest>(action);
                    }
                }

                ActionsCheckerHub.Actions.Add(actionInfo.AccessCondition);
            }

            try
            {
                await actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);

                if (actionInfo.AccessCondition.WaitTillActionCompletion)
                {
                    lock (actionInfo.Lock)
                    {
                        ActionsCheckerHub.Actions.Remove(actionInfo.AccessCondition);
                    }
                }

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
        public async Task<OperationResult<Tuple<ActionState, TResponse>>> ExecuteOrThrowExceptionAsync<TRequest, TResponse>(AsyncActionInfo<TRequest, TResponse> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                foreach(var action in ActionsCheckerHub.Actions
                    .Select(x => x as AsyncAccessCondition<TRequest>)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now)))) 
                {
                    if (action.Predicate(actionInfo.AccessCondition.Request).Result)
                    {
                        throw new LockedResourceException<TRequest>(action);
                    }
                }

                ActionsCheckerHub.Actions.Add(actionInfo.AccessCondition);
            }

            try
            {
                var response = await actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);

                if (actionInfo.AccessCondition.WaitTillActionCompletion)
                {
                    lock (actionInfo.Lock)
                    {
                        ActionsCheckerHub.Actions.Remove(actionInfo.AccessCondition);
                    }
                }

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
