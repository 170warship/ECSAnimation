
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;

[UpdateInGroup(typeof(EntityLogicGroup))]
[UpdateBefore(typeof(EntityStateSystem))]
[BurstCompile]
public partial struct EntityDamageSystem : ISystem
{
    [BurstCompile]
    public partial struct EntityDamageSystemJob : IJobEntity
    {
        [NativeDisableParallelForRestriction] public ComponentLookup<EntitySearchTag> _searchDataLookup;
        [NativeDisableParallelForRestriction] public ComponentLookup<EntityAnimationInstanceComponentData> _animDataLookup;
        public NativeParallelMultiHashMap<Entity, EntityDamageBuffer>.ReadOnly _damageBuffer;

        [BurstCompile]
        public void Execute(InstanceTag _
            , ref HpBarComponentData hpData
            , Entity entity
            )
        {
            if(_damageBuffer.TryGetFirstValue(entity, out var damageBuffer, out var token))
            {
                do
                {
                    hpData.CurrentHp -= damageBuffer.DamageValue;
                    if (_animDataLookup.HasComponent(entity))
                    {
                        var animData = _animDataLookup.GetRefRW(entity);
                        animData.ValueRW.flashWhite = 1;
                    }
                    if (hpData.CurrentHp <= 0)
                    {
                        _searchDataLookup.SetComponentEnabled(entity, false);
                        break;
                    }
                } while (_damageBuffer.TryGetNextValue(out damageBuffer, ref token));
            }
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new EntityDamageSystemJob()
        {
            _searchDataLookup = SystemAPI.GetComponentLookup<EntitySearchTag>(),
            _animDataLookup = SystemAPI.GetComponentLookup<EntityAnimationInstanceComponentData>(),
            _damageBuffer = SystemAPI.GetSingletonRW<EntityMultiplyThreadBufferCacheComponentData>().ValueRW.DamageBuffer.AsReadOnly(),
        }.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        SystemAPI.GetSingletonRW<EntityMultiplyThreadBufferCacheComponentData>().ValueRW.DamageBuffer.Clear();
    }
}