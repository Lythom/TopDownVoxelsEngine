Shader "Dreambuilder/TriplanarAddParticle"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        _MainTex ("Particle Texture", 2D) = "white" {}
    }

    Category
    {
        Tags
        {
            "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        Cull Off Lighting Off ZWrite Off

        SubShader
        {
            Pass
            {

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0
                #pragma multi_compile_particles
                #pragma multi_compile_fog

                #include "UnityCG.cginc"

                sampler2D _MainTex;
                fixed4 _TintColor;

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    fixed4 color : COLOR;
                    float3 normal : NORMAL;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                    UNITY_FOG_COORDS(1)
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                float4 _MainTex_ST;

                float2 getTriplanarUV(float3 worldPos, float3 worldNormals)
                {
                    float2 uv = worldNormals.x * worldPos.zy
                        + worldNormals.y * worldPos.xz
                        + worldNormals.z * worldPos.xy;
                    return float2(uv.x, uv.y);
                }

                v2f vert(appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.color = v.color * _TintColor;
                    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                    o.texcoord = getTriplanarUV(worldPos, worldNormal) + _MainTex_ST.zw;
                    UNITY_TRANSFER_FOG(o, o.vertex);
                    return o;
                }

                UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

                fixed4 frag(v2f i) : SV_Target
                {
                    fixed4 col = 2.0f * i.color * tex2D(_MainTex, i.texcoord);
                    col.a = saturate(col.a);
                    // alpha should not have double-brightness applied to it, but we can't fix that legacy behavior without breaking everyone's effects, so instead clamp the output to get sensible HDR behavior (case 967476)

                    UNITY_APPLY_FOG(i.fogCoord, col);
                    return col;
                }
                ENDCG
            }
        }
    }
}