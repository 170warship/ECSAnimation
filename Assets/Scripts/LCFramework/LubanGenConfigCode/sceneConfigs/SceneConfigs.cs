//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using Bright.Serialization;
using System.Collections.Generic;
using SimpleJSON;



namespace cfg.sceneConfigs
{ 

public sealed partial class SceneConfigs :  Bright.Config.BeanBase 
{
    public SceneConfigs(JSONNode _json) 
    {
        { if(!_json["Id"].IsNumber) { throw new SerializationException(); }  Id = _json["Id"]; }
        { if(!_json["SceneAssetPath"].IsString) { throw new SerializationException(); }  SceneAssetPath = _json["SceneAssetPath"]; }
        { if(!_json["SceneName"].IsString) { throw new SerializationException(); }  SceneName = _json["SceneName"]; }
        { var __json0 = _json["MapDataAssetPath"]; if(!__json0.IsArray) { throw new SerializationException(); } MapDataAssetPath = new System.Collections.Generic.List<string>(__json0.Count); foreach(JSONNode __e0 in __json0.Children) { string __v0;  { if(!__e0.IsString) { throw new SerializationException(); }  __v0 = __e0; }  MapDataAssetPath.Add(__v0); }   }
        PostInit();
    }

    public SceneConfigs(int Id, string SceneAssetPath, string SceneName, System.Collections.Generic.List<string> MapDataAssetPath ) 
    {
        this.Id = Id;
        this.SceneAssetPath = SceneAssetPath;
        this.SceneName = SceneName;
        this.MapDataAssetPath = MapDataAssetPath;
        PostInit();
    }

    public static SceneConfigs DeserializeSceneConfigs(JSONNode _json)
    {
        return new sceneConfigs.SceneConfigs(_json);
    }

    /// <summary>
    /// 这是id
    /// </summary>
    public int Id { get; private set; }
    /// <summary>
    /// 场景加载名字
    /// </summary>
    public string SceneAssetPath { get; private set; }
    /// <summary>
    /// 场景名字
    /// </summary>
    public string SceneName { get; private set; }
    /// <summary>
    /// 地图数据
    /// </summary>
    public System.Collections.Generic.List<string> MapDataAssetPath { get; private set; }

    public const int __ID__ = 129644398;
    public override int GetTypeId() => __ID__;

    public  void Resolve(Dictionary<string, object> _tables)
    {
        PostResolve();
    }

    public  void TranslateText(System.Func<string, string, string> translator)
    {
    }

    public override string ToString()
    {
        return "{ "
        + "Id:" + Id + ","
        + "SceneAssetPath:" + SceneAssetPath + ","
        + "SceneName:" + SceneName + ","
        + "MapDataAssetPath:" + Bright.Common.StringUtil.CollectionToString(MapDataAssetPath) + ","
        + "}";
    }
    
    partial void PostInit();
    partial void PostResolve();
}
}
