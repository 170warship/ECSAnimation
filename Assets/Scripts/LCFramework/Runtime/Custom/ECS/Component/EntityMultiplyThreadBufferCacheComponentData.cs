
using Unity.Collections;
using Unity.Entities;

public struct EntityMultiplyThreadBufferCacheComponentData : IComponentData
{
    public NativeList<EntitySpawnBuffer> SpawnBuffer;
    public NativeList<EntityUnSpawnBuffer> UnSpawnpawnBuffer;
    public NativeParallelMultiHashMap<Entity, EntityDamageBuffer> DamageBuffer;
}