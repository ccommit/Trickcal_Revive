namespace TrickcalRevive.Domain.Account
{
    /// <summary>
    /// 비밀번호를 저장형으로 바꾸고 대조하는 책임. 계정을 "어떻게 저장하는가"와
    /// 자격 증명을 "어떻게 보관하는가"는 바뀌는 이유가 달라 분리한다.
    /// </summary>
    /// <remarks>
    /// 구현을 갈아끼우면 저장 형식이 바뀐다. 기존 계정 파일과의 호환은 구현이 책임진다.
    /// </remarks>
    public interface IPasswordHasher
    {
        /// <summary>계정 파일에 저장할 형태로 바꾼다.</summary>
        string Hash(string password);

        /// <summary>입력 비밀번호가 저장된 값과 맞는지 확인한다.</summary>
        bool Verify(string password, string stored);
    }
}
