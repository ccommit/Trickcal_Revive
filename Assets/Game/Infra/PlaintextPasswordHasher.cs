using TrickcalRevive.Domain.Account;

namespace TrickcalRevive.Infra
{
    /// <summary>
    /// 개발용 구현 — 비밀번호를 변환 없이 그대로 저장하고 그대로 대조한다.
    /// </summary>
    /// <remarks>
    /// 서버 없이 로컬 파일만으로 도는 현재 단계의 인증을 위한 것이다. 계정 파일을 열면
    /// 비밀번호가 그대로 보이므로 실제 서비스 자격 증명에는 쓸 수 없다. 서버 인증을 붙일
    /// 때 이 클래스를 실제 해싱 구현으로 교체한다 — <see cref="IPasswordHasher"/>를
    /// 받는 쪽은 고칠 필요가 없다.
    /// </remarks>
    public sealed class PlaintextPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => password;

        public bool Verify(string password, string stored) => stored == password;
    }
}
