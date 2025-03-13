using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

public class EntityPoolAuthroing : MonoBehaviour
{
    [Serializable]
    public struct EntityPoolAuthoringData
    {
        [SerializeField]
        public int PrefabId;
        [SerializeField]
        public GameObject Prefab;
    }

    [SerializeField]
    public List<EntityPoolAuthoringData> PrefabPool;

    public class EntityPoolAuthroingBaker : Baker<EntityPoolAuthroing>
    {
        public override void Bake(EntityPoolAuthroing authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);


            var assetDictComponentData = new EntityAnimationRendererAssetDictComponentData()
            {
                AssetPathDict = new Dictionary<(string, string), int>(),
                AssetDict = new Dictionary<int, EntityAnimationRendererAssetDictComponentData.DictData>(),
            };

            AddComponentObject(entity, assetDictComponentData);

            var buffer = AddBuffer<EntityPrefabBuffer>(entity);

            for (int i = 0; i < authoring.PrefabPool.Count; i++)
            {
                var data = authoring.PrefabPool[i];
                var Prefab = GetEntity(data.Prefab, TransformUsageFlags.Dynamic);

                buffer.Add(new EntityPrefabBuffer()
                {
                    Prefab = Prefab,
                    PrefabId = data.PrefabId,
                });
            }

            AddComponent<EntityPoolTag>(entity, new EntityPoolTag());
        }
    }
}
