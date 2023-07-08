Shader "Custom/TextureArray"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2DArray) = "white" {}
        _FrameTex ("Frame Albedo (RGB)", 2DArray) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Ramp ("Ramp", 2D) = "white" {}
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
        #pragma surface surf Lambert fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        UNITY_DECLARE_TEX2DARRAY(_MainTex);
        UNITY_DECLARE_TEX2DARRAY(_FrameTex);

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        sampler2D _Ramp;

        half4 LightingRamp(SurfaceOutput s, half3 lightDir, half atten)
        {
            half NdotL = dot(s.Normal, lightDir);
            half diff = NdotL * 0.5 + 0.5;
            half3 ramp = tex2D(_Ramp, float2(diff, 0)).rgb;
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * ramp * atten;
            c.a = s.Alpha;
            return c;
        }

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

        fixed4 triplanarAlbedo(Input IN, float scaleFactor)
        {
            // Triplanar sampling
            fixed4 mainAlbedoX = UNITY_SAMPLE_TEX2DARRAY(
                _MainTex, float3(IN.worldPos.zy * scaleFactor, IN.mainTextureIndex));
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

            return blendX * mainAlbedoX + blendY * mainAlbedoY + blendZ * mainAlbedoZ;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float scaleFactor = 1.0 / 2.5;
            fixed4 mainAlbedo = triplanarAlbedo(IN, scaleFactor);
            // First texture of the FrameTexture is the autotile normal map.
            fixed4 normals = UNITY_SAMPLE_TEX2DARRAY(_FrameTex, float3(IN.textCoords, IN.tileIndex));
            if (IN.frameTextureIndex >= 0)
            {
                // 55 frames per collection of autotile, skip to offset to the start of the designated collection
                // then pick the right tile in that collection
                float frameAlbedoIndex = IN.frameTextureIndex * 55 + IN.tileIndex;
                float frameNormalIndex = IN.frameNormalIndex * 55 + IN.tileIndex;
                fixed4 frameAlbedo = UNITY_SAMPLE_TEX2DARRAY(_FrameTex, float3(IN.textCoords, frameAlbedoIndex));
                fixed4 frameNormals = UNITY_SAMPLE_TEX2DARRAY(_FrameTex, float3(IN.textCoords, frameNormalIndex));
                // Use frame in priority, and mainAlbedo if frame alpha is smaller
                mainAlbedo = lerp(mainAlbedo, frameAlbedo, frameAlbedo.a);
                // mainAlbedo = half4(1,1,1,1);
                normals = lerp(normals, frameNormals, frameAlbedo.a);
            }
            o.Albedo = mainAlbedo;
            // Metallic and smoothness come from slider variables
            // o.Metallic = _Metallic;
            // o.Smoothness = _Glossiness;
            //o.Alpha = mainAlbedo.a;
            o.Normal = normals - half4(0.5, 0.5, 0, 0);
            // o.Normal = float3(0, 0, 1);
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