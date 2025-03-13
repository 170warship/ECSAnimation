using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;
using static EntityAnimationRendererAssetDictComponentData;

[UpdateInGroup(typeof(EntityAnimationGroup))]
[UpdateAfter(typeof(EntityAnimationCollectionSystem))]
public partial class EntityAnimationDrawSystem : SystemBase
{
    private class AnimationDrawComponentData
    {
        public DictData AssetData;
        public NativeList<EntityAnimationInstanceComponentData> _trulyDatas;

        private Texture2D _texture;
        private int _currentSize;
        private int _currentSizePow2;
        private NativeArray<EntityAnimationInstanceComponentData> _texDatas;

        private Material _material;
        private Mesh _mesh;
        private Bounds _bounds;
        private ComputeBuffer _computeBuffer;
        private int _subMeshIndex;
        private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
        private const int _size = 3;

        public void OnCreate()
        {
            _texDatas = new NativeArray<EntityAnimationInstanceComponentData>(_currentSize, Allocator.Persistent);
            _trulyDatas = new NativeList<EntityAnimationInstanceComponentData>(Allocator.Persistent);
            _computeBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        }

        public void OnDestory()
        {
            _texDatas.Dispose();
        }

        public void OnUpdate()
        {
            if (_material == null)
            {
                if (AssetData.State == AssetState.None)
                {
                    AssetData.StartLoad();
                    return;
                }

                if (AssetData.State == AssetState.Loading)
                {
                    return;
                }

                _mesh = AssetData.Mesh;
                _material = AssetData.Mat;

                _bounds = new Bounds(Vector3.zero, Vector3.one * 512);
                _subMeshIndex = Mathf.Clamp(_subMeshIndex, 0, _mesh.subMeshCount - 1);
                _args[0] = (uint)_mesh.GetIndexCount(_subMeshIndex);
                _args[2] = (uint)_mesh.GetIndexStart(_subMeshIndex);
                _args[3] = (uint)_mesh.GetBaseVertex(_subMeshIndex);
            }

            var trulyDataLength = _trulyDatas.Length;

            if (trulyDataLength == 0) return;

            AutoCreateTex(trulyDataLength);

            for (int i = 0; i < trulyDataLength; i++)
            {
                _texDatas[i] = _trulyDatas[i];
            }

            _texture.SetPixelData(_texDatas, 0);
            _texture.Apply();

            _args[1] = (uint)trulyDataLength;
            _computeBuffer.SetData(_args);
            Graphics.DrawMeshInstancedIndirect(
                _mesh
                , _subMeshIndex
                , _material
                , _bounds
                , _computeBuffer
                , 0
                , null
                , UnityEngine.Rendering.ShadowCastingMode.On
                , true
                , 0
                , null);
        }

        private void AutoCreateTex(int length)
        {
            var compareValue = length * _size;
            if (_currentSizePow2 > compareValue) return;

            while (_currentSizePow2 < compareValue)
            {
                if (_currentSize == 0) _currentSize = 1 << 4;
                _currentSize *= 2;
                _currentSizePow2 = Mathf.CeilToInt(math.pow(_currentSize, 2));
            }

            if (_texture != null)
            {
                GameObject.Destroy(_texture);
            }

            _texDatas.Dispose();
            _texDatas = new NativeArray<EntityAnimationInstanceComponentData>(_currentSizePow2, Allocator.Persistent);

            _texture = new Texture2D(_currentSize, _currentSize, TextureFormat.RGBAFloat, false);
            _texture.filterMode = FilterMode.Point;

            _material.SetTexture("_InstanceDataTex", _texture);
            _material.SetInt("_Size", _currentSize);
        }
    }

    private Dictionary<int, AnimationDrawComponentData> _allDraw;
    private EntityAnimationRendererAssetDictComponentData _assetData;

    protected override void OnCreate()
    {
        _allDraw = new Dictionary<int, AnimationDrawComponentData>();
    }

    protected override void OnDestroy()
    {
        foreach (var item in _allDraw.Values)
        {
            item.OnDestory();
        }
        _allDraw.Clear();
    }


    protected override void OnUpdate()
    {
        if (_assetData == null) _assetData = EntityManager.GetComponentObject<EntityAnimationRendererAssetDictComponentData>(SystemAPI.GetSingletonEntity<EntityPoolTag>());

        foreach (var item in _allDraw.Values)
        {
            item._trulyDatas.Clear();
        }

        var commonUseData = SystemAPI.GetSingleton<EntityCommonValueComponentData>();

        foreach (var (_, meshAndMatIndexs, animationInstanceData) in SystemAPI
            .Query<InstanceTag, DynamicBuffer<MeshAndMatIndexBuffer>, EntityAnimationInstanceComponentData>())
        {
            for (int i = 0; i < meshAndMatIndexs.Length; i++)
            {
                if (!commonUseData.CheckInScreen(animationInstanceData.pos)) continue;

                var meshAndMatIndex = meshAndMatIndexs[i].MeshAndMatIndex;

                if (!_allDraw.TryGetValue(meshAndMatIndex, out var draw))
                {
                    var createNew = new AnimationDrawComponentData();
                    createNew.AssetData = _assetData.AssetDict[meshAndMatIndex];
                    createNew.OnCreate();
                    draw = createNew;
                    _allDraw.Add(meshAndMatIndex, createNew);
                }

                draw._trulyDatas.Add(animationInstanceData);
            }
        }

        foreach (var item in _allDraw.Values)
        {
            item.OnUpdate();
        }
    }
}