using System;
using System.Collections.Generic;

namespace TrickcalRevive.Infra.SaveHandlers
{
    // JsonUtility는 최상위 배열을 직접 못 다뤄서 한 겹 감싼다.
    [Serializable]
    internal sealed class ListWrapper<T>
    {
        public List<T> Items = new List<T>();
    }
}
