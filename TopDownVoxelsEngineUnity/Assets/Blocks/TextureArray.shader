Shader "Custom/TextureArray"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _mainTex ("Albedo (RGB)", 2DArray) = "white" {}
        _mainNormals ("Normals", 2DArray) = "white" {}
        _mainHeights ("Heights", 2DArray) = "white" {}
        _frameTex ("frame Albedo (RGB)", 2DArray) = "white" {}
        _frameNormals ("frame Normals", 2DArray) = "white" {}
        _frameHeights ("frame Heights", 2DArray) = "white" {}
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

        int2 Unpack2(float packed)
        {
            int a = floor(packed / 4095.0);
            int b = round(packed - a * 4095.0);
            return int2(a, b);
        }

        int3 Unpack3(float packed)
        {
            int a = floor(packed / (255.0 * 255.0));
            int b = floor(packed - a * 255.0 * 255.0);
            int c = floor(packed - a * 255.0 * 255.0 - b * 255.0);
            return int3(a, b, c);
        }

        UNITY_DECLARE_TEX2DARRAY(_mainTex);
        UNITY_DECLARE_TEX2DARRAY(_frameTex);
        UNITY_DECLARE_TEX2DARRAY(_mainNormals);
        UNITY_DECLARE_TEX2DARRAY(_mainHeights);
        UNITY_DECLARE_TEX2DARRAY(_frameNormals);
        UNITY_DECLARE_TEX2DARRAY(_frameHeights);

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        sampler2D _Ramp;
        float _ParallaxStrength;

        struct Input
        {
            float2 textCoords;
            float3 worldPos;
            float3 worldNormals;
            float3 tangentViewDir;
            float mainTextureIndex;
            float mainNormalsIndex;
            float mainHeightsIndex;
            float frameTextureIndex;
            float frameNormalIndex;
            float frameHeightsIndex;
            float tileIndex;
            float mainWindFactor;
            float frameWindFactor;
        };

        float2 triplanarUV(Input IN)
        {
            float2 uv =
                // "* float2(sign(IN.worldNormals.x), 1)" is to correct UV on backfaces (because mesh vertex or reversed)
                abs(IN.worldNormals.x) * IN.worldPos.zy * float2(sign(IN.worldNormals.x), 1)
                // Can't get uvParallax to work if normal is absolute for some weird reason
                + IN.worldNormals.y * IN.worldPos.xz * float2(sign(IN.worldNormals.y), 1)
                // "* float2(-sign(IN.worldNormals.z), 1)" is to correct UV on backfaces (because mesh vertex or reversed)
                + abs(IN.worldNormals.z) * IN.worldPos.xy * float2(-sign(IN.worldNormals.z), 1);
            return float2(uv.x, uv.y);
        }

        // x = height
        // y = isFrameVisible
        float2 GetHeight(float3 rayPosFrame, float frameHeightsIndex, float3 rayPosMain, float mainHeightsIndex)
        {
            const float frameHeight = UNITY_SAMPLE_TEX2DARRAY(_frameHeights, float3(rayPosFrame.xz, frameHeightsIndex)).
                r;
            const float baseHeight = UNITY_SAMPLE_TEX2DARRAY(_mainHeights, float3(rayPosMain.xz, mainHeightsIndex)).r;
            const float h = max(frameHeight, baseHeight);
            float isFrameVisible = frameHeight >= baseHeight;
            return float2((h - 1) * _ParallaxStrength, isFrameVisible);
            // return float2((baseHeight - 1) * _ParallaxStrength, isframeVisible);
        }

        // x,y = uv
        // z = isFrameVisible
        // w = height
        float4 ParallaxMapping(
            float2 frameTexCoord, fixed3 viewDir, float frameHeightsIndex, float2 mainTexCoord, float mainHeightsIndex
        )
        {
            const float maxSteps = 48;

            const float stepHeight = 1.0 / maxSteps;
            // Where is the ray starting? y is up and we always start at the surface
            float3 rayPosFrame = float3(frameTexCoord.x, 0, frameTexCoord.y);
            float3 rayPosMain = float3(mainTexCoord.x, 0, mainTexCoord.y);
            // surface is 0 and we go inside to the negatives
            float rayHeight = 0;
            float currentHeight = 0;
            // What's the direction of the ray?
            const float3 rayDir = viewDir * _ParallaxStrength;
            const float3 rayStep = rayDir * stepHeight;
            float isFrameVisible = 0;
            for (int i = 0; i < maxSteps; ++i)
            {
                // red is [0;1], height is [-_ParallaxStrength;0]
                const float prevHeight = currentHeight;
                float2 result = GetHeight(rayPosFrame, frameHeightsIndex, rayPosMain, mainHeightsIndex);
                currentHeight = result.x;
                isFrameVisible = result.y;
                // have we cross the texture height yet?
                if (rayHeight <= currentHeight)
                {
                    const float delta1 = currentHeight - rayHeight;
                    const float delta2 = (rayHeight + stepHeight * _ParallaxStrength) - prevHeight;
                    const float ratio = delta1 / (delta1 + delta2);
                    rayPosFrame = (ratio) * (rayPosFrame - rayStep) + (1.0 - ratio) * rayPosFrame;
                    break;
                }
                rayPosFrame = rayPosFrame + rayStep;
                rayPosMain = rayPosMain + rayStep;
                rayHeight -= stepHeight * _ParallaxStrength;
            }


            return float4(rayPosFrame.xz - frameTexCoord, isFrameVisible, currentHeight);
        }

        float RandomValue(float2 seed)
        {
            return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
        }

        float Smoothstep(float edge0, float edge1, float x)
        {
            float t = saturate((x - edge0) / (edge1 - edge0));
            return t * t * (3 - 2 * t);
        }

        float PerlinNoise(float2 p)
        {
            float2 fl = floor(p);
            float2 fc = frac(p);

            float a = RandomValue(fl);
            float b = RandomValue(fl + float2(1, 0));
            float c = RandomValue(fl + float2(0, 1));
            float d = RandomValue(fl + float2(1, 1));

            float2 u = fc * fc * (3.0 - 2.0 * fc);
            return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            // float t = (float)(IN.frameTextureIndex * 55 + IN.tileIndex);
            // o.Albedo = UNITY_SAMPLE_TEX2DARRAY(_frameTex, float3(IN.textCoords, t));
            // o.Albedo = UNITY_SAMPLE_TEX2DARRAY(_frameTex, float3(IN.textCoords, t));
            // o.Albedo = float3( IN.mainWindFactor, IN.frameWindFactor, 0);
            // return;
            const float scaleFactor = 0.3;
            const float2 tuv = triplanarUV(IN) * scaleFactor;
            const float2 texCoord = IN.textCoords;
            float frameHeightsIndex = IN.frameHeightsIndex * 55 + IN.tileIndex;
            // First, calculate new UV using parallax occlusion mapping
            float4 mapping = ParallaxMapping(IN.textCoords, IN.tangentViewDir, frameHeightsIndex, tuv,
                IN.mainHeightsIndex);
            float2 uvOffset = mapping.xy;
            const float isFrameVisible = mapping.z;
            const float height = 0.2 + mapping.w * 3;

            float perlin_noise = PerlinNoise(float2(
                    (IN.worldPos.x - _Time.y * 10) * 0.03,
                    (IN.worldPos.z - _Time.y * 8) * 0.1)
            );
            float perlin_noise2 = PerlinNoise(float2(
                    (IN.worldPos.x - _Time.y * 2) * .1,
                    (IN.worldPos.z - _Time.y * 6) * 0.5)
            ) - 0.5;
            float wind = (perlin_noise * perlin_noise2) * height;
            uvOffset -= wind * 0.1 * (IN.mainWindFactor * (1 - isFrameVisible) + IN.frameWindFactor * isFrameVisible);
            // o.Albedo = wind;
            // return;

            // 55 frames per collection of auto-tile, skip to offset to the start of the designated collection
            // then pick the right tile in that collection
            float frameNormalIndex = IN.frameNormalIndex * 55 + IN.tileIndex;
            fixed4 frameNormals = UNITY_SAMPLE_TEX2DARRAY(_frameNormals, float3(texCoord + uvOffset, frameNormalIndex));
            const half3 frameNormalsUnpacked = frameNormals;

            fixed4 mainAlbedo = UNITY_SAMPLE_TEX2DARRAY(_mainTex, float3(tuv + uvOffset, IN.mainTextureIndex));
            fixed4 mainNormals = UNITY_SAMPLE_TEX2DARRAY(_mainNormals, float3(tuv + uvOffset, IN.mainNormalsIndex));

            // Calculate new normals
            const half3 normalsUnpacked = mainNormals * (1 - isFrameVisible) + frameNormalsUnpacked *
                isFrameVisible;

            if (IN.frameTextureIndex > -1)
            {
                // 55 frames per collection of autotile, skip to offset to the start of the designated collection
                // then pick the right tile in that collection
                float frameAlbedoIndex = IN.frameTextureIndex * 55 + IN.tileIndex;
                fixed4 frameAlbedo = UNITY_SAMPLE_TEX2DARRAY(_frameTex, float3(texCoord + uvOffset, frameAlbedoIndex));
                // Use frame in priority, and mainAlbedo if frame alpha is smaller
                mainAlbedo = lerp(mainAlbedo, frameAlbedo, isFrameVisible);
                // mainAlbedo = isFrameVisible;
            }

            o.Albedo = mainAlbedo; // * windMask;
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
            const float3 worldVertexPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            const float3 worldViewDir = worldVertexPos - _WorldSpaceCameraPos;

            //To convert from world space to tangent space we need the following
            //https://docs.unity3d.com/Manual/SL-VertexFragmentShaderExamples.html
            const float3 worldNormal = UnityObjectToWorldNormal(v.normal);
            const float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
            const float3 worldBitangent = cross(worldNormal, worldTangent) * v.tangent.w * unity_WorldTransformParams.w;

            const float3 viewDir = worldToTangentSpace(normalize(worldViewDir), worldNormal, worldTangent,
                worldBitangent);

            // from https://github.com/basementstudio/basement-laboratory/blob/main/src/experiments/43.depth-shader.js#L56C7-L57C53
            const float3 normal = worldToTangentSpace(worldNormal, worldNormal, worldTangent, worldBitangent);
            const float facingCoefficient = -dot(viewDir, normal);
            // + lerp to limit the infinity effect when reaching near parallel angle (near 0)
            o.tangentViewDir = viewDir / (facingCoefficient + lerp(0.2, 0, saturate(facingCoefficient)));
            // o.tangentViewDir = viewDir;

            int2 r = Unpack2(v.texcoord.z);
            o.mainTextureIndex = r.x;
            o.mainNormalsIndex = r.y;
            o.mainHeightsIndex = v.texcoord.w;

            o.tileIndex = v.texcoord2.x;
            // v.texcoord2.y currently unused
            r = Unpack2(v.texcoord2.z);
            o.frameTextureIndex = r.x;
            o.frameNormalIndex = r.y;
            o.frameHeightsIndex = v.texcoord2.w;
            int3 r3 = Unpack3(v.texcoord2.y);
            o.mainWindFactor = r3.x / 254;
            o.frameWindFactor = r3.y / 254;

            o.textCoords = v.texcoord.xy;
            o.worldPos = mul(UNITY_MATRIX_M, v.vertex).xyz;
            o.worldNormals = worldNormal;
        }
        ENDCG
    }
    FallBack "Diffuse"
}