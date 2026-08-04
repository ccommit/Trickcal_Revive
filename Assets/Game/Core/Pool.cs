using System.Collections.Generic;
using UnityEngine;

namespace TrickcalRevive.Core
{
    public sealed class Pool
    {
        private readonly Dictionary<GameObject, Queue<GameObject>> freeByPrefab =
            new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> prefabByInstance =
            new Dictionary<GameObject, GameObject>();

        public void Prewarm(GameObject prefab, int count)
        {
            var queue = GetOrCreateQueue(prefab);
            for (var i = 0; i < count; i++)
            {
                var instance = CreateInstance(prefab);
                instance.SetActive(false);
                queue.Enqueue(instance);
            }
        }

        public GameObject Get(GameObject prefab)
        {
            var queue = GetOrCreateQueue(prefab);
            var instance = queue.Count > 0 ? queue.Dequeue() : CreateInstance(prefab);
            instance.SetActive(true);
            return instance;
        }

        public void Release(GameObject instance)
        {
            if (instance == null || !prefabByInstance.TryGetValue(instance, out var prefab))
                return;

            instance.SetActive(false);
            GetOrCreateQueue(prefab).Enqueue(instance);
        }

        private GameObject CreateInstance(GameObject prefab)
        {
            var instance = Object.Instantiate(prefab);
            prefabByInstance[instance] = prefab;
            return instance;
        }

        private Queue<GameObject> GetOrCreateQueue(GameObject prefab)
        {
            if (!freeByPrefab.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<GameObject>();
                freeByPrefab[prefab] = queue;
            }
            return queue;
        }
    }
}
