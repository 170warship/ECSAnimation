// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/Animation"
{
    Properties
    {
        _MainTex1 ("Texture1", 2D) = "white" {}
        _VertexDataTex ("VertexDataTex", 2D) = "white" {}
        _InstanceDataTex("InstanceDataTex", 2D) = "white" {}
        _UVX ("_UVX", float) = 0
        _Size("_Size", float) = 0
        _NormalTex("NormalDataTex", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            Tags{"RenderType" = "Transparent" "Queue"="Transparent" }    
            Blend  SrcAlpha OneMinusSrcAlpha
            Name "DrawGPUEntity"
            //ZWrite off
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ Anti_Aliasing_ON
 
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                uint vid : SV_VertexID;
                uint sid : SV_INSTANCEID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1; 
                float4 debug : TEXCOORD2;
                float4 customData : TEXCOORD3;
                float4 worldPos : TEXCOORD4;
            };

            sampler2D _MainTex1;
            sampler2D _VertexDataTex;
            sampler2D _InstanceDataTex;            
            sampler2D _NormalTex;
            float4 _MainTex_ST;
            float _UVX;
            float _Size;
            float _NoneAnimation;

            v2f vert (appdata v)
            {
                float vid = v.vid + 0.5;
                v.sid *= 3;

                // 读取instanceData        
                float normalizeLen = (1 / _Size);
                float4 instanceData = tex2Dlod(_InstanceDataTex, float4(v.sid % _Size, v.sid / _Size, 0, 0) * normalizeLen);         
                float4 instanceData2 = tex2Dlod(_InstanceDataTex, float4((v.sid + 1) % _Size, (v.sid + 1) / _Size, 0, 0) * normalizeLen);                
                float4 instanceData3 = tex2Dlod(_InstanceDataTex, float4((v.sid + 2) % _Size, (v.sid + 2) / _Size, 0, 0) * normalizeLen);                

                //法线贴图
                //float4 normal = tex2Dlod(_NormalTex, float4(vid * _UVX, instanceData.w, 0, 0));
                float4 lastNormal = tex2Dlod(_NormalTex, float4(vid * _UVX, instanceData3.x, 0, 0));

                // 顶点位置计算
                float4 vertex = tex2Dlod(_VertexDataTex, float4(vid * _UVX, instanceData.w, 0, 0));
                float4 lastVertex = tex2Dlod(_VertexDataTex, float4(vid * _UVX, instanceData3.x, 0, 0));

                vertex = lerp(lastVertex, vertex, instanceData3.y);
                //normal = lerp(lastNormal, normal, instanceData3.y);

                float4x4 M_Scale = float4x4
                (
                    instanceData2.w,0,0,0,
                    0,instanceData2.w,0,0,
                    0,0,instanceData2.w,0,
                    0,0,0,instanceData2.w
                );
                vertex = mul(M_Scale,vertex);

                // 将缩放矩阵应用于法线
                //float3 rotatedNormal = mul(M_Scale, normal.xyz); 
                // instanceData2 = 0;
                // instanceData.rgb = 0;
                float4x4 M_rotateX = float4x4
                    (
                    1,0,0,0,
                    0,cos(instanceData2.x),-sin(instanceData2.x),0,
                    0,sin(instanceData2.x),cos(instanceData2.x),0,
                    0,0,0,1
                    );
                float4x4 M_rotateY = float4x4
                    (
                    cos(instanceData2.y),0,sin(instanceData2.y),0,
                    0,1,0,0,
                    -sin(instanceData2.y),0,cos(instanceData2.y),0,
                    0,0,0,1
                    );
                float4x4 M_rotateZ = float4x4
                    (
                        cos(instanceData2.z),-sin(instanceData2.z),0,0,
                        sin(instanceData2.z),cos(instanceData2.z),0,0,
                        0,0,1,0,
                        0,0,0,1
                    );


                vertex = mul(M_rotateX,vertex);
                vertex = mul(M_rotateY,vertex);
                vertex = mul(M_rotateZ,vertex);

                //rotatedNormal = mul(M_rotateX, rotatedNormal);
                //rotatedNormal = mul(M_rotateY, rotatedNormal);
                //rotatedNormal = mul(M_rotateZ, rotatedNormal);  

                v2f o;
                float4 worldPos = float4(TransformObjectToWorld(vertex + float4(instanceData.rgb, 0)), 0);
                o.vertex = TransformWorldToHClip(worldPos);
                o.worldPos = vertex + float4(instanceData.xyz, 0);
                o.uv = v.uv;
                //o.normal = normalize(mul((float3x3)unity_WorldToObject, rotatedNormal));
                o.normal = v.normal;
                o.debug = instanceData;
                o.customData = instanceData3;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {                     
                // half4 col = (i.customData.w == 1 || i.customData.w == 0) ? tex2D(_MainTex1, i.uv) : tex2D(_MainTex2, i.uv);
                half4 col = tex2D(_MainTex1, i.uv);
                Light mainLight = GetMainLight();
                // float3 dir = normalize(mainLight.direction);
                // col = col * max(0.3, dot(dir, i.normal)) * half4(mainLight.color, 1.0);    

    //             float4 SHADOW_COORDS = TransformWorldToShadowCoord(i.worldPos);
				// mainLight = GetMainLight(SHADOW_COORDS);
				// half shadow = MainLightRealtimeShadow(SHADOW_COORDS);                
                // col *= half4(shadow, shadow, shadow, 1) * 0.5;

                //目前不存在透明物体
                col.a = 1;

                col += i.customData.z;

                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCast"
 
			Tags{ "LightMode" = "ShadowCaster" }
            
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                uint vid : SV_VERTEXID;
                uint sid : SV_INSTANCEID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            sampler2D _VertexDataTex;
            sampler2D _InstanceDataTex;            
            sampler2D _NormalTex;
            float4 _MainTex_ST;
            float _UVX;
            float _Size;
            float _NoneAnimation;

            v2f vert (appdata v)
            {
                float vid = v.vid + 0.5;
                v.sid *= 3;

                // 读取instanceData        
                float normalizeLen = (1 / _Size);
                float4 instanceData = tex2Dlod(_InstanceDataTex, float4(v.sid % _Size, v.sid / _Size, 0, 0) * normalizeLen);         
                float4 instanceData2 = tex2Dlod(_InstanceDataTex, float4((v.sid + 1) % _Size, (v.sid + 1) / _Size, 0, 0) * normalizeLen);                
                float4 instanceData3 = tex2Dlod(_InstanceDataTex, float4((v.sid + 2) % _Size, (v.sid + 2) / _Size, 0, 0) * normalizeLen);                

                //法线贴图
                //float4 normal = tex2Dlod(_NormalTex, float4(vid * _UVX, instanceData.w, 0, 0));
                float4 lastNormal = tex2Dlod(_NormalTex, float4(vid * _UVX, instanceData3.x, 0, 0));

                // 顶点位置计算
                float4 vertex = tex2Dlod(_VertexDataTex, float4(vid * _UVX, instanceData.w, 0, 0));
                float4 lastVertex = tex2Dlod(_VertexDataTex, float4(vid * _UVX, instanceData3.x, 0, 0));

                vertex = lerp(lastVertex, vertex, instanceData3.y);
                //normal = lerp(lastNormal, normal, instanceData3.y);

                float4x4 M_Scale = float4x4
                (
                    instanceData2.w,0,0,0,
                    0,instanceData2.w,0,0,
                    0,0,instanceData2.w,0,
                    0,0,0,instanceData2.w
                );
                vertex = mul(M_Scale,vertex);

                // 将缩放矩阵应用于法线
                //float3 rotatedNormal = mul(M_Scale, normal.xyz); 

                float4x4 M_rotateX = float4x4
                    (
                    1,0,0,0,
                    0,cos(instanceData2.x),-sin(instanceData2.x),0,
                    0,sin(instanceData2.x),cos(instanceData2.x),0,
                    0,0,0,1
                    );
                float4x4 M_rotateY = float4x4
                    (
                    cos(instanceData2.y),0,sin(instanceData2.y),0,
                    0,1,0,0,
                    -sin(instanceData2.y),0,cos(instanceData2.y),0,
                    0,0,0,1
                    );
                float4x4 M_rotateZ = float4x4
                    (
                        cos(instanceData2.z),-sin(instanceData2.z),0,0,
                        sin(instanceData2.z),cos(instanceData2.z),0,0,
                        0,0,1,0,
                        0,0,0,1
                    );


                vertex = mul(M_rotateX,vertex);
                vertex = mul(M_rotateY,vertex);
                vertex = mul(M_rotateZ,vertex);

                //rotatedNormal = mul(M_rotateX, rotatedNormal);
                //rotatedNormal = mul(M_rotateY, rotatedNormal);
                //rotatedNormal = mul(M_rotateZ, rotatedNormal);  

                v2f o;
                float4 worldPos = float4(TransformObjectToWorld(vertex + float4(instanceData.xyz, 0)), 0);
                o.vertex = TransformWorldToHClip(worldPos);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {                          
                return 0;
            }
			ENDHLSL
        }
    }
}
