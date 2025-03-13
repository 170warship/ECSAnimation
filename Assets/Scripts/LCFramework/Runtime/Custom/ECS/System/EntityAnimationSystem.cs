﻿
using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using UnityEngine.UIElements;
using static EntityAnimationCollectionSystem;

[UpdateInGroup(typeof(EntityAnimationGroup))]
[BurstCompile]
public partial struct EntityAnimationCollectionSystem : ISystem
{
    public struct RendererWriteData
    {
        public int MeshAndMatIndex;
        public EntityAnimationInstanceComponentData InstanceData;
    }

    [BurstCompile]
    public partial struct ECSAnimationSystemJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;

        [BurstCompile]
        public void Execute(
                DynamicBuffer<EntityAnimationConfigComponentData> configs
                , ref EntityAnimationRuntimeComponentData data
                , ref EntityAnimationInstanceComponentData instanceData
                , DynamicBuffer<EntityAnimationConfigBuffer> configBuffer
                , DynamicBuffer<EntityAnimationEventConfigBuffer> eventConfigBuffer
                , DynamicBuffer<EntityAnimationTriggerEventBuffer> eventBuffer
                , DynamicBuffer<EntityAnimationWaitTriggerEventBuffer> waitEventBuffer
                , InstanceTag _
                , ref LocalTransform tsData
                , Entity entity
            )
        {
            if (configBuffer.Length == 0) goto PlayAnim;

            if (data.CurrentAnimationId == data.NextAnimationId || data.Stop) goto PlayAnim;

            //切换
            data.CurrentAnimationId = data.NextAnimationId;
            instanceData.lastUVY = instanceData.uvY;
            instanceData.lerpValue = 0;
            for (int i = 0; i < configBuffer.Length; i++)
            {
                if (data.CurrentAnimationId == configBuffer[i].AnimationId)
                {
                    data.Time = 0;
                    data.NormalizeTime = 0;
                    data.CurrentRepeatCount = 0;

                    data.CurrentConfig = configBuffer[i];
                    data.CurrentAnimationId = data.CurrentConfig.AnimationId;
                    data.NextAnimationId = data.CurrentConfig.AnimationId;
                    ReigsterWaitEvent(eventConfigBuffer, waitEventBuffer, data);
                    goto PlayAnim;
                }
            }

        //播放动画
        PlayAnim:
            var texHeight = configs[0].Height;
            if (data.CurrentConfig == EntityAnimationConfigBuffer.Null)
            {
                if (configBuffer.Length > 0)
                {
                    data.CurrentConfig = configBuffer[0];
                    data.CurrentAnimationId = data.CurrentConfig.AnimationId;
                    data.NextAnimationId = data.CurrentConfig.AnimationId;
                    ReigsterWaitEvent(eventConfigBuffer, waitEventBuffer, data);
                }
            }

            var tempRepeatCount = data.CurrentRepeatCount;
            data.Time += DeltaTime * data.Speed * (data.Stop ? 0 : 1);
            data.CurrentRepeatCount = (int)(data.Time / data.CurrentConfig.TotalSec);
            var trulyValue = data.CurrentConfig.TotalSec == 0
                ? 0
                : data.Time / data.CurrentConfig.TotalSec;
            data.NormalizeTime = data.CurrentConfig.AnimationLoop
                ? math.abs(trulyValue - math.trunc(trulyValue))
                : trulyValue;
            var uvY = math.clamp((data.NormalizeTime * ((data.CurrentConfig.EndLine - data.CurrentConfig.StartLine) * 1.0f / texHeight)) + data.CurrentConfig.StartLine * 1.0f / texHeight, 0, data.CurrentConfig.EndLine * 1.0f / texHeight - 1f / texHeight);

            var rot = ((Quaternion)tsData.Rotation).eulerAngles;

            instanceData.pos = tsData.Position;
            instanceData.uvY = uvY;
            instanceData.rot = new float3(math.radians(rot.x), math.radians(rot.y), math.radians(rot.z));
            instanceData.scale = tsData.Scale;
            instanceData.lerpValue = math.clamp(instanceData.lerpValue + 1 / data.Duration * DeltaTime, 0, 1);
            instanceData.flashWhite = math.clamp(instanceData.flashWhite - 1 / 0.3f * DeltaTime, 0, 1);

            //事件处理
            eventBuffer.Clear();

            var compareNormalizeTime = tempRepeatCount == data.CurrentRepeatCount ? data.NormalizeTime : 1;
            for (int i = 0; i < waitEventBuffer.Length; i++)
            {
                var waitEventData = waitEventBuffer[i];
                if (compareNormalizeTime >= waitEventData.NormalizeTriggerTime)
                {
                    eventBuffer.Add(new EntityAnimationTriggerEventBuffer { EventId = waitEventData.EventId });
                    waitEventBuffer.RemoveAt(i--);
                }
            }

            if (!data.CurrentConfig.AnimationLoop) return;

            var internalRepeatCount = data.CurrentRepeatCount - tempRepeatCount;
            if (internalRepeatCount == 0) return;

            for (int i = 0; i < internalRepeatCount - 1; i++)
            {
                for (int j = 0; j < eventConfigBuffer.Length; j++)
                {
                    var eventConfigBufferData = eventConfigBuffer[j];
                    if (eventConfigBufferData.AnimationId == data.CurrentAnimationId)
                    {
                        eventBuffer.Add(new EntityAnimationTriggerEventBuffer() { EventId = eventConfigBufferData.EventId });
                    }
                }
            }

            ReigsterWaitEvent(eventConfigBuffer, waitEventBuffer, data);
        }

        private void ReigsterWaitEvent(
            DynamicBuffer<EntityAnimationEventConfigBuffer> eventConfigBuffer
            , DynamicBuffer<EntityAnimationWaitTriggerEventBuffer> waitEventBuffer
            , EntityAnimationRuntimeComponentData data)
        {
            waitEventBuffer.Clear();
            for (int i = 0; i < eventConfigBuffer.Length; i++)
            {
                var eventConfigBufferData = eventConfigBuffer[i];
                if (eventConfigBufferData.AnimationId == data.CurrentAnimationId)
                {
                    waitEventBuffer.Add(new EntityAnimationWaitTriggerEventBuffer()
                    {
                        EventId = eventConfigBufferData.EventId,
                        NormalizeTriggerTime = eventConfigBufferData.NormalizeTriggerTime
                    });
                }
            }
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnDestory(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new ECSAnimationSystemJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
        }.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();
    }
}