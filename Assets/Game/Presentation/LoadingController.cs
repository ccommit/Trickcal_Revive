using UnityEngine;

namespace TrickcalRevive.Presentation
{
    /// <summary>
    /// 씬 전환 시 뜨는 로딩/차단 오버레이. 실제 비주얼(패널·애니메이션)은 UI 배치 단계에서
    /// 채운다 — 지금은 SceneFlowController가 아직 이 메서드들을 호출하지 않으므로 안전한
    /// no-op으로 둔다(02_로비화면전환_설계서 §2.1·§2.2).
    /// </summary>
    public class LoadingController : MonoBehaviour
    {
        public void Show(string type, string message)
        {
        }

        public void Complete()
        {
        }

        public void Fail(string error)
        {
        }
    }
}
