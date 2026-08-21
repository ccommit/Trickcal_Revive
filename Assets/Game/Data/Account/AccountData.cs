using System;

namespace TrickcalRevive.Data.Account
{
    [Serializable]
    public class AccountData
    {
        public string AccountId;
        public string LoginId;
        public string Password;
        public string Nickname;
        public int PlayerLevel;
        public int Exp;
        public long Gold;
        public long Elleaf;
        public long Macaron;
        public int Stamina;
        public int StaminaMax;
        public int SaveVersion;
    }
}
