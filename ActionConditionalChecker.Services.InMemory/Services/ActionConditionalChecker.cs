using ActionConditionalChecker.Contracts;
using ActionConditionalChecker.Contracts.AccessCondition;
using ActionConditionalChecker.Contracts.ActionInfo;
using OperationResult;
using System;
using System.Linq;
using ActionConditionalChecker.Services.InMemory.Hub;
using Result = OperationResult;
using ActionConditionalChecker.Services.InMemory.Exceptions;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections;

namespace ActionConditionalChecker.Services.InMemory
{
    public class Test : IProducerConsumerCollection<object>
    {
        public int Count => throw new NotImplementedException();

        public bool IsSynchronized => throw new NotImplementedException();

        public object SyncRoot => throw new NotImplementedException();

        public void CopyTo(object[] array, int index)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<object> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public object[] ToArray()
        {
            throw new NotImplementedException();
        }

        public bool TryAdd(object item)
        {
            throw new NotImplementedException();
        }

        public bool TryTake(out object item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class ActionConditionalChecker : IActionConditionalChecker
    {
        /// <inheritdoc/>
        public OperationResult<bool> CanExecute<TRequest>(AccessCondition<TRequest> accessCondition)
        {
            var now = DateTime.UtcNow;

            var hasBlockingAction = ActionsCheckerHub.Actions
                .Select(x => x as AccessCondition<TRequest>)
                .Any(x => x != null && (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now)) && x.Predicate(accessCondition.Request));

            return Result.OperationResult.Succeeded(!hasBlockingAction);
        }

        /// <inheritdoc/>
        public void EnsureCanExecute<TRequest>(AccessCondition<TRequest> accessCondition)
        {
            var now = DateTime.UtcNow;

            var blockingAction = ActionsCheckerHub.Actions
                .Select(x => x as AccessCondition<TRequest>)
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
                    .Select(x => x as AccessCondition<TRequest>)
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

                ActionsCheckerHub.Actions.Add(actionInfo.AccessCondition);  
            }

            try
            {
                actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);

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
                    .Select(x => x as AccessCondition<TRequest>)
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

                ActionsCheckerHub.Actions.Add(actionInfo.AccessCondition);
            }

            try
            {
                var response = actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);

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
        public OperationResult<ActionState> ExecuteOrSkip<TRequest>(ActionInfo<TRequest> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                var blockingAction = ActionsCheckerHub.Actions
                    .Select(x => x as AccessCondition<TRequest>)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now)) &&
                        x.Predicate(actionInfo.AccessCondition.Request))
                    .FirstOrDefault();

                if (blockingAction != null)
                {
                    return OperationResult<ActionState>.Succeeded(null)
                        .WithMessage("Skipped!");
                }

                ActionsCheckerHub.Actions.Add(actionInfo.AccessCondition);
            }

            try
            {
                actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);

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
        public OperationResult<Tuple<ActionState, TResponse>> ExecuteOrSkip<TRequest, TResponse>(ActionInfo<TRequest, TResponse> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                var blockingAction = ActionsCheckerHub.Actions
                    .Select(x => x as AccessCondition<TRequest>)
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

                ActionsCheckerHub.Actions.Add(actionInfo.AccessCondition);
            }

            try
            {
                var response = actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);

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
        public OperationResult<ActionState> ExecuteOrThrowException<TRequest>(ActionInfo<TRequest> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                var blockingAction = ActionsCheckerHub.Actions
                    .Select(x => x as AccessCondition<TRequest>)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now)) &&
                        x.Predicate(actionInfo.AccessCondition.Request))
                    .FirstOrDefault();

                if (blockingAction != null)
                {
                    throw new LockedResourceException<TRequest>(blockingAction);
                }

                ActionsCheckerHub.Actions.Add(actionInfo.AccessCondition);
            }

            try
            {
                actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);

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
        public OperationResult<Tuple<ActionState, TResponse>> ExecuteOrThrowException<TRequest, TResponse>(ActionInfo<TRequest, TResponse> actionInfo)
        {
            lock (actionInfo.Lock)
            {
                var now = DateTime.UtcNow;

                var blockingAction = ActionsCheckerHub.Actions
                    .Where(x => x.GetType().Equals(typeof(AccessCondition<TRequest>)))
                    .Select(x => x as AccessCondition<TRequest>)
                    .Where(x => x != null &&
                        (x.WaitTillActionCompletion || (!x.WaitTillActionCompletion && x.ExpiresAtUtc > now)) &&
                        x.Predicate(actionInfo.AccessCondition.Request))
                    .FirstOrDefault();

                if (blockingAction != null)
                {
                    throw new LockedResourceException<TRequest>(blockingAction);
                }

                ActionsCheckerHub.Actions.Add(actionInfo.AccessCondition);
            }

            try
            {
                var response = actionInfo.Action.Invoke(actionInfo.AccessCondition.Request);

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
