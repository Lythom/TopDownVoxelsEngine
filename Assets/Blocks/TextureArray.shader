Shader "Custom/TextureArray"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2DArray) = "white" {}
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

        struct Input
        {
            float2 textCoords;
            float textureIndex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(IN.textCoords, IN.textureIndex));
            o.Albedo = c * _Color;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.textCoords = v.texcoord.xy;
            o.textureIndex = v.texcoord.z;
        }
        ENDCG
    }
    FallBack "Diffuse"
}