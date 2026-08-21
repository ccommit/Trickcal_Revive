using System;
using System.Collections.Generic;

namespace TrickcalRevive.Core
{
    /// <summary>
    /// Condition-driven finite state machine: transitions fire on Tick() when their
    /// condition becomes true, unlike StateMachine which only changes state on explicit call.
    /// </summary>
    public sealed class FSM<TState> : IStateMachine<TState> where TState : struct
    {
        private sealed class Transition
        {
            public TState From;
            public TState To;
            public Func<bool> Condition;
        }

        private readonly List<Transition> transitions = new List<Transition>();

        public TState CurrentState { get; private set; }
        public event Action<TState, TState> StateChanged;

        public FSM(TState initialState)
        {
            CurrentState = initialState;
        }

        public void AddTransition(TState from, TState to, Func<bool> condition)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            transitions.Add(new Transition { From = from, To = to, Condition = condition });
        }

        public void Tick(float deltaTime)
        {
            foreach (var transition in transitions)
            {
                if (!EqualityComparer<TState>.Default.Equals(transition.From, CurrentState) || !transition.Condition())
                    continue;

                var previous = CurrentState;
                CurrentState = transition.To;
                StateChanged?.Invoke(previous, transition.To);
                return;
            }
        }
    }
}
