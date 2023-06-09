Shader "Custom/TextureArray"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2DArray) = "white" {}
        _MainNormals ("Normals", 2DArray) = "white" {}
        _FrameTex ("Frame Albedo (RGB)", 2DArray) = "white" {}
        _FrameNormals ("Frame Normals", 2DArray) = "white" {}
        _FrameHeights ("Frame Heights", 2DArray) = "white" {}
        _Ramp ("Ramp", 2D) = "white" {}
        _ParallaxStrength ("Parallax Strength", Range(0, 1)) = 0
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

        #pragma target 5.0

        UNITY_DECLARE_TEX2DARRAY(_MainTex);
        UNITY_DECLARE_TEX2DARRAY(_FrameTex);
        UNITY_DECLARE_TEX2DARRAY(_MainNormals);
        UNITY_DECLARE_TEX2DARRAY(_FrameNormals);
        UNITY_DECLARE_TEX2DARRAY(_FrameHeights);

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        sampler2D _Ramp;
        float _ParallaxStrength;

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
            float3 textureIndex; // x in main, y is frame, z i tileIndex
            float3 worldPos;
            float3 worldNormals;
            float3 tangentViewDir;
        };

        float2 triplanarUV(Input IN)
        {
            return
                // "* float2(sign(IN.worldNormals.x), 1)" is to correct UV on backfaces (because mesh vertex or reversed)
                abs(IN.worldNormals.x) * IN.worldPos.zy * float2(sign(IN.worldNormals.x), 1)
                // Can't get uvParallax to work if normal is absoluted for some weird reason
                + IN.worldNormals.y * IN.worldPos.xz * float2(sign(IN.worldNormals.y), 1)
                // "* float2(-sign(IN.worldNormals.z), 1)" is to correct UV on backfaces (because mesh vertex or reversed)
                + abs(IN.worldNormals.z) * IN.worldPos.xy * float2(-sign(IN.worldNormals.z), 1);
        }

        float2 ParallaxMapping(float2 texCoord, fixed3 viewDir, float textureIndex)
        {
            float maxSteps = 32;

            float stepSize = 1.0 / maxSteps;
            // Where is the ray starting? y is up and we always start at the surface
            float3 rayPos = float3(texCoord.x, 0, texCoord.y);
            // surface is 0 and we go inside to the negatives
            float rayHeight = 0;
            float currentHeight = 0;
            // What's the direction of the ray?
            float3 rayDir = viewDir * _ParallaxStrength;
            float3 rayStep = rayDir * stepSize;

            for (int i = 0; i < maxSteps; ++i)
            {
                // red is [0;1], height is [-_ParallaxStrength;0]
                const float prevHeight = currentHeight;
                currentHeight = (UNITY_SAMPLE_TEX2DARRAY(_FrameHeights, float3(rayPos.xz, textureIndex)).r - 1) *
                    _ParallaxStrength;
                // have we cross the texture height yet?
                if (rayHeight <= currentHeight)
                {
                    const float delta1 = currentHeight - rayHeight;
                    const float delta2 = (rayHeight + stepSize * _ParallaxStrength) - prevHeight;
                    const float ratio = delta1 / (delta1 + delta2);
                    rayPos = (ratio) * (rayPos - rayStep) + (1.0 - ratio) * rayPos;
                    break;
                }
                rayPos = rayPos + rayStep;
                rayHeight -= stepSize * _ParallaxStrength;
            }


            return rayPos.xz - texCoord;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float mainTextureIndex = IN.textureIndex.x;
            float frameTextureIndex = IN.textureIndex.y;
            float tileIndex = IN.textureIndex.z;
            float scaleFactor = 0.3;
            float2 tuv = triplanarUV(IN) * scaleFactor;
            // First texture of the FrameTexture is the autotile normal map.
            fixed4 normals = UNITY_SAMPLE_TEX2DARRAY(_MainNormals, float3(tuv, mainTextureIndex));
            half3 normalsUnpacked = UnpackNormal(normals);

            float2 texCoord = IN.textCoords;
            float2 uvOffset = 0;
            if (frameTextureIndex >= 0)
            {
                float frameNormalIndex = frameTextureIndex * 55 + tileIndex;
                uvOffset = ParallaxMapping(IN.textCoords, IN.tangentViewDir, frameNormalIndex);
                // First, calculate new UV using parralax occlusion mapping

                // o.Albedo = float3(texCoord, 0);
                // o.Albedo = normalize(IN.tangentViewDir) + 0.5;
                // return;

                // 55 frames per collection of autotile, skip to offset to the start of the designated collection
                // then pick the right tile in that collection
                fixed4 frameNormals = UNITY_SAMPLE_TEX2DARRAY(_FrameNormals,
                                                              float3(texCoord + uvOffset, frameNormalIndex));
                const half3 frameNormalsUnpacked = UnpackNormal(frameNormals);

                // Calcule la nouvelle normale
                half3 newNormal;
                newNormal.xy = normalsUnpacked.xy + frameNormalsUnpacked.xy;
                newNormal.z = normalsUnpacked.z * frameNormalsUnpacked.z;
                normalsUnpacked = newNormal;
            }
            fixed4 mainAlbedo = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(tuv + uvOffset, mainTextureIndex));

            if (frameTextureIndex >= 0)
            {
                // 55 frames per collection of autotile, skip to offset to the start of the designated collection
                // then pick the right tile in that collection
                float frameAlbedoIndex = frameTextureIndex * 55 + tileIndex;
                fixed4 frameAlbedo = UNITY_SAMPLE_TEX2DARRAY(_FrameTex, float3(texCoord + uvOffset, frameAlbedoIndex));
                // Use frame in priority, and mainAlbedo if frame alpha is smaller
                mainAlbedo = lerp(mainAlbedo, frameAlbedo, frameAlbedo.a);
            }
            o.Albedo = mainAlbedo;
            // o.Alpha = mainAlbedo.a;
            //o.Normal = (normals - half4(0.5, 0.5, 0, 0));
            o.Normal = normalsUnpacked;
            // o.Normal = float3(0, 0, 1);
            // o.Normal = UnpackNormal(normals);
        }

        float3 worldToTangentSpace(float3 vec, float3 worldNormal, float3 worldTangent, float3 worldBitangent)
        {
            return float3(
                dot(vec, worldTangent),
                dot(vec, worldNormal),
                dot(vec, worldBitangent)
            );
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 worldVertexPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float3 worldViewDir = worldVertexPos - _WorldSpaceCameraPos;

            //To convert from world space to tangent space we need the following
            //https://docs.unity3d.com/Manual/SL-VertexFragmentShaderExamples.html
            float3 worldNormal = UnityObjectToWorldNormal(v.normal);
            float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
            float3 worldBitangent = cross(worldNormal, worldTangent) * v.tangent.w * unity_WorldTransformParams.w;

            float3 viewDir = worldToTangentSpace(normalize(worldViewDir), worldNormal, worldTangent, worldBitangent);

            // from https://github.com/basementstudio/basement-laboratory/blob/main/src/experiments/43.depth-shader.js#L56C7-L57C53
            float3 normal = worldToTangentSpace(worldNormal, worldNormal, worldTangent, worldBitangent);
            float facingCoeficient = -dot(viewDir, normal);
            o.tangentViewDir = viewDir / facingCoeficient;

            o.textCoords = v.texcoord.xy;
            o.textureIndex.x = v.texcoord.z;
            o.textureIndex.y = v.texcoord.w;
            o.textureIndex.z = v.texcoord2.x;
            o.worldPos = mul(UNITY_MATRIX_M, v.vertex).xyz;
            o.worldNormals = worldNormal;
        }
        ENDCG
    }
    FallBack "Diffuse"
}