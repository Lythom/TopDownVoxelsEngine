Shader "Custom/TextureArray"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2DArray) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        [PerRendererData] _TextureIndex ("_TextureIndex", Range(0,16)) = 0.0
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
        #pragma vertex vert
        #pragma fragment frag

        #pragma multi_compile_instancing

        UNITY_DECLARE_TEX2DARRAY(_MainTex);

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_DEFINE_INSTANCED_PROP(float, _TextureIndex)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            //setup instance id
            UNITY_SETUP_INSTANCE_ID(IN);
            float textureIndex = UNITY_ACCESS_INSTANCED_PROP(Props, _TextureIndex);

            fixed4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(IN.uv_MainTex, textureIndex)) * _Color;
            o.Albedo = c;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }

        void vert(inout appdata_full v, out Input o)
        {
            o.uv_MainTex = v.texcoord.xy;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);
        }
        ENDCG
    }
    FallBack "Diffuse"
}