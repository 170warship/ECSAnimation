
using Nebukam.ORCA;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(EntityPoolGroup))]
[UpdateBefore(typeof(EntitySpawnSystem))]
[BurstCompile]
public partial struct EntityUnSpawnSystem : ISystem
{

    private DynamicBuffer<EntityPrefabBuffer> _prefabBuffer;
    private EntityCommandBuffer _cmd;
    private NativeList<int> _changeNumber;

    [BurstCompile]
    private partial struct EntityUnSpawnSystemJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter _cmd;
        public NativeArray<EntityUnSpawnBuffer>.ReadOnly _unSpawnBuffer;
        [ReadOnly] public DynamicBuffer<EntityPrefabBuffer> _prefabBuffer;
        public NativeList<int>.ParallelWriter _changeNumber;

        [ReadOnly] public ComponentLookup<EntityRVOAgentComponentData> _rvoDataLookup;
        [ReadOnly] public ComponentLookup<EntityLookupComponentData> _lookupDataLookup;
        [ReadOnly] public ComponentLookup<EntityStateComponentData> _stateDataLookup;
        [ReadOnly] public ComponentLookup<EntitySearchTag> _searchDataLookup;
        [ReadOnly] public ComponentLookup<HpBarComponentData> _hpBarDataLookup;
        [ReadOnly] public BufferLookup<Child> _childLookup;

        [BurstCompile]
        public void Execute(int index)
        {
            var unSpawnData = _unSpawnBuffer[index];

            UnSpawn(unSpawnData, index);
        }

        [BurstCompile]
        private void UnSpawn(EntityUnSpawnBuffer unSpawnData, int index)
        {
            for (int i = 0; i < _prefabBuffer.Length; i++)
            {
                var prefabBufferData = _prefabBuffer[i];
                if (prefabBufferData.PrefabId == unSpawnData.PrefabId)
                {
                    _cmd.AppendToBuffer(index, prefabBufferData.Prefab, new EntityPrefabElementBuffer()
                    {
                        Entity = unSpawnData.Entity,
                    });
                    _cmd.SetComponentEnabled<InstanceTag>(index, unSpawnData.Entity, false);
                    _cmd.SetComponent(index, unSpawnData.Entity, new LocalTransform()
                    {
                        Position = new float3(1, 1, 1) * 9999,
                        Rotation = quaternion.identity,
                        Scale = 0,
                    });                    
                    _changeNumber.AddNoResize(i);

                    //Custom
                    if(_rvoDataLookup.HasComponent(unSpawnData.Entity)) _cmd.RemoveComponent<EntityRVOAgentComponentData>(index, unSpawnData.Entity);
                    if (_lookupDataLookup.HasComponent(unSpawnData.Entity)) _cmd.RemoveComponent<EntityLookupComponentData>(index, unSpawnData.Entity);
                    if (_stateDataLookup.HasComponent(unSpawnData.Entity)) _cmd.RemoveComponent<EntityStateComponentData>(index, unSpawnData.Entity);
                    if (_searchDataLookup.HasComponent(unSpawnData.Entity)) _cmd.RemoveComponent<EntitySearchTag>(index, unSpawnData.Entity);
                    if (_hpBarDataLookup.HasComponent(unSpawnData.Entity)) _cmd.RemoveComponent<HpBarComponentData>(index, unSpawnData.Entity);

                    RecursionDisable(unSpawnData.Entity, index);
                    break;
                }
            }
        }



        [BurstCompile]
        private void RecursionDisable(Entity entity, int index)
        {
            if (_childLookup.TryGetBuffer(entity, out var childBuffer))
            {
                for (int i = 0; i < childBuffer.Length; i++)
                {
                    var child = childBuffer[i].Value;
                    RecursionDisable(child, index);
                }
            }

            _cmd.AddComponent<Disabled>(index, entity);
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _changeNumber = new NativeList<int>(1 << 20, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        _changeNumber.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _cmd = new EntityCommandBuffer(Allocator.Persistent);
        _prefabBuffer = SystemAPI.GetSingletonBuffer<EntityPrefabBuffer>();
        var unSpawnBuffer = SystemAPI.GetSingleton<EntityMultiplyThreadBufferCacheComponentData>().UnSpawnpawnBuffer;
        _changeNumber.Clear();
        new EntityUnSpawnSystemJob()
        {
            _cmd = _cmd.AsParallelWriter(),
            _unSpawnBuffer = unSpawnBuffer.AsReadOnly(),
            _prefabBuffer = _prefabBuffer,
            _changeNumber = _changeNumber.AsParallelWriter(),

            _rvoDataLookup = SystemAPI.GetComponentLookup<EntityRVOAgentComponentData>(),
            _lookupDataLookup = SystemAPI.GetComponentLookup<EntityLookupComponentData>(),
            _stateDataLookup = SystemAPI.GetComponentLookup<EntityStateComponentData>(),
            _searchDataLookup = SystemAPI.GetComponentLookup<EntitySearchTag>(),
            _hpBarDataLookup = SystemAPI.GetComponentLookup<HpBarComponentData>(),
            _childLookup = SystemAPI.GetBufferLookup<Child>(),
        }.Schedule(unSpawnBuffer.Length, 1).Complete();

        _cmd.Playback(state.EntityManager);
        _cmd.Dispose();

        _prefabBuffer = SystemAPI.GetSingletonBuffer<EntityPrefabBuffer>();
        for (int i = 0; i < _changeNumber.Length; i++)
        {
            var key = _changeNumber[i];
            var data = _prefabBuffer[key];
            data.UseCount -= 1;
            data.FreeCount += 1;
            _prefabBuffer[key] = data;
        }

        //unSpawnBuffer = SystemAPI.GetSingletonBuffer<EntityUnSpawnBuffer>();
        //unSpawnBuffer.Clear();
        SystemAPI.GetSingletonRW<EntityMultiplyThreadBufferCacheComponentData>().ValueRW.UnSpawnpawnBuffer.Clear();
    }
}
