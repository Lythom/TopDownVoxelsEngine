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
            float frameNormalIndex;
            float tileIndex;
            float3 worldPos;
            float3 worldNormals;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float scaleFactor = 1.0 / 2.5;
            // Triplanar sampling
            fixed4 mainAlbedoX = UNITY_SAMPLE_TEX2DARRAY(
                _MainTex, float3(IN.worldPos.yz * scaleFactor, IN.mainTextureIndex));
            fixed4 mainAlbedoY = UNITY_SAMPLE_TEX2DARRAY(
                _MainTex, float3(IN.worldPos.xz * scaleFactor, IN.mainTextureIndex));
            fixed4 mainAlbedoZ = UNITY_SAMPLE_TEX2DARRAY(
                _MainTex, float3(IN.worldPos.xy * scaleFactor, IN.mainTextureIndex));

            float blendX = abs(IN.worldNormals.x);
            float blendY = abs(IN.worldNormals.y);
            float blendZ = abs(IN.worldNormals.z);

            // Normalize blend weights
            float totalBlend = blendX + blendY + blendZ;
            blendX /= totalBlend;
            blendY /= totalBlend;
            blendZ /= totalBlend;

            fixed4 mainAlbedo = blendX * mainAlbedoX + blendY * mainAlbedoY + blendZ * mainAlbedoZ;
            // First texture of the FrameTexture is the autotile normal map.
            fixed4 normals = UNITY_SAMPLE_TEX2DARRAY(_FrameTex, float3(IN.textCoords, IN.tileIndex));
            if (IN.frameTextureIndex >= 0)
            {
                // 55 frames per collection of autotile, skip to offset to the start of the designated collection
                // then pick the right tile in that collection
                fixed4 frameAlbedo = UNITY_SAMPLE_TEX2DARRAY(_FrameTex, float3(IN.textCoords, IN.frameTextureIndex * 55 + IN.tileIndex));
                mainAlbedo = lerp(mainAlbedo, frameAlbedo, frameAlbedo.a);
                fixed4 frameNormals = UNITY_SAMPLE_TEX2DARRAY(_FrameTex, float3(IN.textCoords, IN.frameNormalIndex * 55 + IN.tileIndex));
                normals = lerp(normals, frameNormals, frameAlbedo.a);
            }
            o.Albedo = mainAlbedo * _Color;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = mainAlbedo.a;
            const float3 normal = UnpackNormal(normals);
            o.Normal = normal;
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.textCoords = v.texcoord.xy;
            o.mainTextureIndex = v.texcoord.z;
            o.frameTextureIndex = v.texcoord.w;
            o.tileIndex = v.texcoord2.x;
            o.frameNormalIndex = v.texcoord2.y;
            o.worldPos = mul(UNITY_MATRIX_M, v.vertex).xyz;
            o.worldNormals = mul((float3x3)UNITY_MATRIX_M, v.normal).xyz;
        }
        ENDCG
    }
    FallBack "Diffuse"
}