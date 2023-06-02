Shader "Custom/TextureArray"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2DArray) = "white" {}
        _FrameTex ("Frame Albedo (RGB)", 2DArray) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        UNITY_DECLARE_TEX2DARRAY(_MainTex);
        UNITY_DECLARE_TEX2DARRAY(_FrameTex);

        struct Input
        {
            float2 textCoords;
            float mainTextureIndex;
            float frameTextureIndex;
            float tileIndex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(IN.textCoords, IN.mainTextureIndex));
            if (IN.frameTextureIndex >= 0)
            {
                // 55 frames per collection of autotile, skip to offset to the start of the designated collection
                // then pick the right tile in that collection
                fixed4 fc = UNITY_SAMPLE_TEX2DARRAY(
                    _FrameTex, float3(IN.textCoords, IN.frameTextureIndex * 55 + IN.tileIndex));
                c = lerp(c, fc, fc.a);
            }
            o.Albedo = c * _Color;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            // First texture of the FrameTexture is the autotile normal map.
            fixed4 n = UNITY_SAMPLE_TEX2DARRAY(_FrameTex, float3(IN.textCoords, IN.tileIndex));
            const float3 normal = UnpackNormal(n);
            o.Normal = normal;
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.textCoords = v.texcoord.xy;
            o.mainTextureIndex = v.texcoord.z;
            o.frameTextureIndex = v.texcoord.w;
            o.tileIndex = v.texcoord2.x;
        }
        ENDCG
    }
    FallBack "Diffuse"
}