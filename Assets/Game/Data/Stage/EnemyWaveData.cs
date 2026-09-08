using System;
using System.Collections.Generic;

namespace TrickcalRevive.Data.Stage
{
    // stage_master.waves_json 한 스폰. DB_설계서 4.4절
    [Serializable]
    public class EnemySpawnData
    {
        public string CharacterId;
        public int Level;

        // normal / elite / boss
        public string Grade;

        public int PosX;
        public int PosY;

        // 0이면 배율 없음으로 본다. 정예·보스 강화에 쓴다.
        public float StatMultiplier;
    }

    [Serializable]
    public class EnemyWaveData
    {
        public int WaveIndex;
        public List<EnemySpawnData> Spawns = new List<EnemySpawnData>();
    }

    // JsonUtility는 최상위 배열을 못 읽어서 감싸는 그릇이 필요하다.
    [Serializable]
    public class EnemyWaveListData
    {
        public List<EnemyWaveData> Waves = new List<EnemyWaveData>();
    }
}
