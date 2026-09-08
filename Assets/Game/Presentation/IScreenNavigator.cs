using System;

namespace TrickcalRevive.Presentation
{
    /// <summary>
    /// Main씬 안에서 화면(패널)만 바꾼다 — 씬 리로드 없음(02_로비화면전환_설계서 §2.4).
    /// GameObject를 직접 켜고 끄지 않는다: 화면 id 변경 이벤트만 발행하고, 실제 표시·숨김·
    /// 전환 연출은 MainUI가 ScreenChanged를 구독해 처리한다.
    /// </summary>
    public interface IScreenNavigator
    {
        string CurrentScreen { get; }
        event Action<string> ScreenChanged;

        void Show(string screenId);

        /// <summary>뒤로가기 판단용 — 스택을 바꾸지 않고 이전 화면이 있는지만 본다.</summary>
        bool TryPeekPrevious(out string previousScreenId);

        /// <summary>이전 화면으로 실제로 돌아간다(스택 pop). 갈 곳 없으면 false.</summary>
        bool GoBack();

        /// <summary>Main씬 진입 시 호출 — 방문 스택을 비우고 지정한 화면에서 새로 시작한다.</summary>
        void Reset(string initialScreenId);
    }
}
