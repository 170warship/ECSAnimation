using Native.Event;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public partial class EntityBakeDoneSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!SystemAPI.HasSingleton<EntityPoolTag>())
            return;
        ECSBridgeManager.ECSInitDone = true;
        var poolEntity = SystemAPI.GetSingletonEntity<EntityPoolTag>();

        var poolBuffer = SystemAPI.GetSingletonBuffer<EntityPrefabBuffer>();

        var cmd = new EntityCommandBuffer(Allocator.Persistent);

        cmd.SetName(poolEntity, "PoolRoot");

        var mtData = new EntityMultiplyThreadBufferCacheComponentData()
        {
            SpawnBuffer = new NativeList<EntitySpawnBuffer>(1 << 16, Allocator.Persistent),
            UnSpawnpawnBuffer = new NativeList<EntityUnSpawnBuffer>(1 << 16, Allocator.Persistent),
            DamageBuffer = new NativeParallelMultiHashMap<Entity, EntityDamageBuffer>(1 << 20, Allocator.Persistent),
        };
        cmd.AddComponent<EntityMultiplyThreadBufferCacheComponentData>(poolEntity, mtData);

        Debug.Log("Start ECS Init");

        for (int i = 0; i < poolBuffer.Length; i++)
        {
            var entity = poolBuffer[i].Prefab;

            cmd.SetName(entity, $"Prefab:{poolBuffer[i].PrefabId}");

            cmd.AddBuffer<EntityPrefabElementBuffer>(entity);

            cmd.AddComponent<Disabled>(entity);

            if (EntityManager.HasComponent<EntityAnimationRendererPathComponentData>(entity))
            {
                var meshAndMatIndexBuffer = cmd.AddBuffer<MeshAndMatIndexBuffer>(entity);
                var animation = EntityManager.GetComponentObject<EntityAnimationRendererPathComponentData>(entity);
                for (int j = 0; j < animation.MeshPath.Length; j++)
                {
                    var key = (animation.MeshPath[j], animation.MatPath[j]);
                    var assetDictComponentData = EntityManager.GetComponentObject<EntityAnimationRendererAssetDictComponentData>(poolEntity);
                    if (!assetDictComponentData.AssetPathDict.TryGetValue(key, out var index))
                    {
                        index = ++assetDictComponentData.DictCount;
                        assetDictComponentData.AssetDict.Add(index, new EntityAnimationRendererAssetDictComponentData.DictData()
                        {
                            //按需加载即可
                            Mesh = null,
                            Mat = null,
                            MeshPath = key.Item1,
                            MatPath = key.Item2,
                            State = EntityAnimationRendererAssetDictComponentData.AssetState.None,
                        });
                        assetDictComponentData.AssetPathDict.Add(key, index);
                    }

                    if (assetDictComponentData.AssetPathDict.TryGetValue(key, out var MeshAndMatIndex))
                    {
                        meshAndMatIndexBuffer.Add(new MeshAndMatIndexBuffer()
                        {
                            MeshAndMatIndex = MeshAndMatIndex,
                        });
                    }
                    else
                    {
                        meshAndMatIndexBuffer.Add(new MeshAndMatIndexBuffer()
                        {
                            MeshAndMatIndex = -1,
                        });
                    }
                }

            }
        }
        Enabled = false;
        cmd.Playback(EntityManager);
        cmd.Dispose();
    }
}