using System;
using System.Collections.Generic;

namespace TrickcalRevive.Core
{
    public sealed class StateMachine<TState> : IStateMachine<TState> where TState : struct
    {
        private readonly HashSet<(TState From, TState To)> allowedTransitions =
            new HashSet<(TState From, TState To)>();

        public TState CurrentState { get; private set; }
        public event Action<TState, TState> StateChanged;

        public StateMachine(TState initialState)
        {
            CurrentState = initialState;
        }

        public void AllowTransition(TState from, TState to)
        {
            allowedTransitions.Add((from, to));
        }

        public bool CanTransition(TState nextState)
        {
            return allowedTransitions.Count == 0 || allowedTransitions.Contains((CurrentState, nextState));
        }

        public bool ChangeState(TState nextState)
        {
            if (!CanTransition(nextState))
                return false;

            var previous = CurrentState;
            CurrentState = nextState;
            StateChanged?.Invoke(previous, nextState);
            return true;
        }
    }
}
