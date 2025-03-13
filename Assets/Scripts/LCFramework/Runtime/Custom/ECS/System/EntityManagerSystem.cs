
using Unity.Collections;
using Unity.Entities;

public partial class EntityManagerSystem : SystemBase
{

    public void ClearAllEntity()
    {
        var multiplyThreadBufferCache = SystemAPI.GetSingletonRW<EntityMultiplyThreadBufferCacheComponentData>();
        var spawnBuffer = multiplyThreadBufferCache.ValueRW.SpawnBuffer;
        var unSpawnBuffer = multiplyThreadBufferCache.ValueRW.UnSpawnpawnBuffer;

        spawnBuffer.Clear();

        foreach (var instanceData in SystemAPI.Query<InstanceTag>())
        {
            unSpawnBuffer.Add(new EntityUnSpawnBuffer()
            {
                Entity = instanceData.Self,
                PrefabId = instanceData.PrefabId,
            });
        }
    }

    protected override void OnUpdate()
    {
        
    }

    public int GetEntityCount()
    {
        return SystemAPI.QueryBuilder().WithAll<InstanceTag>().Build().ToEntityArray(Allocator.Persistent).Length;
    }
}