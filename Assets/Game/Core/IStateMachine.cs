using System;

namespace TrickcalRevive.Core
{
    // StateMachine(수동 전이)과 FSM(조건 자동 전이)이 공유하는 최소 약속.
    // 상태를 읽고 구독하는 쪽은 둘 중 어느 구현인지 몰라도 된다.
    public interface IStateMachine<TState> where TState : struct
    {
        TState CurrentState { get; }
        event Action<TState, TState> StateChanged;
    }
}
