Shader "Custom/TextureArray"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2DArray) = "white" {}
        _MainNormals ("Normals", 2DArray) = "white" {}
        _FrameTex ("Frame Albedo (RGB)", 2DArray) = "white" {}
        _FrameNormals ("Frame Normals", 2DArray) = "white" {}
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
        UNITY_DECLARE_TEX2DARRAY(_MainNormals);
        UNITY_DECLARE_TEX2DARRAY(_FrameNormals);

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        sampler2D _Ramp;

        // half4 LightingRamp(SurfaceOutput s, half3 lightDir, half atten)
        // {
        //     half NdotL = dot(s.Normal, lightDir);
        //     half diff = NdotL * 0.5 + 0.5;
        //     half3 ramp = tex2D(_Ramp, float2(diff, 0)).rgb;
        //     half4 c;
        //     c.rgb = s.Albedo * _LightColor0.rgb * ramp * atten;
        //     c.a = s.Alpha;
        //     return c;
        // }

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

        float2 triplanarUV(Input IN)
        {
            return abs(IN.worldNormals.x) * IN.worldPos.zy
                + abs(IN.worldNormals.y) * IN.worldPos.xz
                + abs(IN.worldNormals.z) * IN.worldPos.xy;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float scaleFactor = 0.3;
            float2 tuv = triplanarUV(IN) * scaleFactor;
            fixed4 mainAlbedo = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(tuv, IN.mainTextureIndex));
            // First texture of the FrameTexture is the autotile normal map.
            fixed4 normals = UNITY_SAMPLE_TEX2DARRAY(_MainNormals, float3(tuv, IN.mainTextureIndex));
            half3 normalsUnpacked = UnpackNormal(normals);
            if (IN.frameTextureIndex >= 0)
            {
                // 55 frames per collection of autotile, skip to offset to the start of the designated collection
                // then pick the right tile in that collection
                float frameAlbedoIndex = IN.frameTextureIndex * 55 + IN.tileIndex;
                fixed4 frameAlbedo = UNITY_SAMPLE_TEX2DARRAY(_FrameTex, float3(IN.textCoords, frameAlbedoIndex));
                // Use frame in priority, and mainAlbedo if frame alpha is smaller
                mainAlbedo = lerp(mainAlbedo, frameAlbedo, frameAlbedo.a);
            }
            if (IN.frameNormalIndex >= 0)
            {
                // 55 frames per collection of autotile, skip to offset to the start of the designated collection
                // then pick the right tile in that collection
                float frameNormalIndex = IN.frameNormalIndex * 55 + IN.tileIndex;
                fixed4 frameNormals = UNITY_SAMPLE_TEX2DARRAY(_FrameNormals, float3(IN.textCoords, frameNormalIndex));
                const half3 frameNormalsUnpacked = UnpackNormal(frameNormals);
            
                // Calcule la nouvelle normale
                half3 newNormal;
                newNormal.xy = normalsUnpacked.xy + frameNormalsUnpacked.xy;
                newNormal.z = normalsUnpacked.z * frameNormalsUnpacked.z;
                normalsUnpacked = newNormal;
            }
            o.Albedo = mainAlbedo;
            // o.Alpha = mainAlbedo.a;
            //o.Normal = (normals - half4(0.5, 0.5, 0, 0));
            o.Normal = normalsUnpacked;
            // o.Normal = float3(0, 0, 1);
            // o.Normal = UnpackNormal(normals);
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