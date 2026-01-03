Shader "SHD_PSX_Terrain"
{
    Properties
    {
        // 1) Base texture
        _MainTex ("Main Tex", 2D) = "white" {}
        _Color   ("Tint", Color) = (1,1,1,1)

        // 2) Normal map
        [Toggle] _UseNormalMap ("Use Normal Map", Float) = 0
        _BumpMap ("Normal Map", 2D) = "bump" {}

        // 3) Alpha
        [Toggle] _UseAlphaClip ("Use Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        // 4) Color steps
        [Toggle] _UseColorSteps ("Use Color Steps", Float) = 0
        _ColorSteps ("Color Steps", Range(2, 32)) = 8

        // 5) Wobble
        [Toggle] _UseWobble ("Use PSX Wobble", Float) = 0
        _WobblePixels ("Wobble Pixel Size", Range(1, 16)) = 2

        // 6) Rest (dither)
        [Toggle] _UseDither ("Use Dithering", Float) = 0
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
        }
        LOD 200
        Cull Back
        ZWrite On

        // ---------- SHADOW CASTER (alpha-aware) ----------
        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex   vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4   _MainTex_ST;
            fixed4   _Color;

            float _UseAlphaClip;
            float _Cutoff;
            float _UseDither;
            float _DitherStrength;

            struct v2fShadow
            {
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
            };

            v2fShadow vertShadow (appdata_full v)
            {
                v2fShadow o;
                UNITY_SETUP_INSTANCE_ID(v);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 fragShadow (v2fShadow i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                float alpha = c.a;

                if (_UseAlphaClip > 0.5)
                {
                    if (_UseDither > 0.5 && _DitherStrength > 0.0)
                    {
                        float n   = frac(sin(dot(i.pos.xy, float2(12.9898, 78.233))) * 43758.5453);
                        float amp = _DitherStrength * 0.5;
                        alpha     = saturate(alpha + (n - 0.5) * (2.0 * amp));
                    }

                    clip(alpha - _Cutoff);
                }

                SHADOW_CASTER_FRAGMENT(i);
            }
            ENDCG
        }

        // ---------- BASE PASS ----------
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            float4   _MainTex_ST;
            fixed4   _Color;

            sampler2D _BumpMap;
            float4    _BumpMap_ST;
            float     _UseNormalMap;

            float _UseColorSteps;
            float _ColorSteps;

            float _UseWobble;
            float _WobblePixels;

            float _UseDither;
            float _DitherStrength;

            float _UseAlphaClip;
            float _Cutoff;

            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
                float2 uv      : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos          : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 worldPos     : TEXCOORD1;
                float3 worldNormal  : TEXCOORD2;
                SHADOW_COORDS(3)
                float4 worldTangent : TEXCOORD4;
                UNITY_FOG_COORDS(5)
            };

            v2f vert (appdata v)
            {
                v2f o;

                float4 worldPos    = mul(unity_ObjectToWorld, v.vertex);
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);

                float4 tangentOS   = v.tangent;
                float3 worldTan    = UnityObjectToWorldDir(tangentOS.xyz);
                worldTan           = normalize(worldTan);
                float  tangentSign = tangentOS.w * unity_WorldTransformParams.w;

                float4 clipPos = UnityWorldToClipPos(worldPos);

                if (_UseWobble > 0.5)
                {
                    float2 ndc       = clipPos.xy / clipPos.w;
                    float2 screenPos = (ndc * 0.5 + 0.5) * _ScreenParams.xy;

                    float pixels = max(_WobblePixels, 1.0);
                    screenPos    = floor(screenPos / pixels) * pixels;

                    ndc          = (screenPos / _ScreenParams.xy) * 2.0 - 1.0;
                    clipPos.xy   = ndc * clipPos.w;
                }

                o.pos          = clipPos;
                o.uv           = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos     = worldPos.xyz;
                o.worldNormal  = worldNormal;
                o.worldTangent = float4(worldTan, tangentSign);

                UNITY_TRANSFER_SHADOW(o, worldPos);
                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * _Color;

                if (_UseColorSteps > 0.5)
                {
                    float steps = max(_ColorSteps, 1.0);
                    tex.rgb = floor(tex.rgb * steps) / steps;
                }

                float alpha = tex.a;

                if (_UseAlphaClip > 0.5 && _UseDither > 0.5 && _DitherStrength > 0.0)
                {
                    float n   = frac(sin(dot(i.pos.xy, float2(12.9898, 78.233))) * 43758.5453);
                    float amp = _DitherStrength * 0.5;
                    alpha     = saturate(alpha + (n - 0.5) * (2.0 * amp));
                }

                if (_UseAlphaClip > 0.5)
                {
                    clip(alpha - _Cutoff);
                }

                tex.a = alpha;

                // Base normal
                float3 N = normalize(i.worldNormal);

                // Optional normal map
                if (_UseNormalMap > 0.5)
                {
                    float2 bumpUV   = TRANSFORM_TEX(i.uv, _BumpMap);
                    float3 normalTS = UnpackNormal(tex2D(_BumpMap, bumpUV));

                    float3 T = normalize(i.worldTangent.xyz);
                    float3 B = cross(N, T) * i.worldTangent.w;
                    float3x3 TBN = float3x3(T, B, N);

                    float3 normalWS = mul(normalTS, TBN);
                    N = normalize(normalWS);
                }

                float3 L = normalize(UnityWorldSpaceLightDir(i.worldPos));
                float  NdotL = max(0.0, dot(N, L));

                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                float3 diffuse = tex.rgb * _LightColor0.rgb * NdotL * atten;
                float3 ambient = tex.rgb * UNITY_LIGHTMODEL_AMBIENT.rgb;

                float3 finalColor = diffuse + ambient;

                fixed4 col = fixed4(finalColor, tex.a);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

        // ---------- ADD PASS ----------
        Pass
        {
            Tags { "LightMode" = "ForwardAdd" }
            Blend One One
            ZWrite Off

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex   vertAdd
            #pragma fragment fragAdd
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            float4   _MainTex_ST;
            fixed4   _Color;

            sampler2D _BumpMap;
            float4    _BumpMap_ST;
            float     _UseNormalMap;

            float _UseColorSteps;
            float _ColorSteps;

            float _UseWobble;
            float _WobblePixels;

            float _UseDither;
            float _DitherStrength;

            float _UseAlphaClip;
            float _Cutoff;

            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
                float2 uv      : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos          : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 worldPos     : TEXCOORD1;
                float3 worldNormal  : TEXCOORD2;
                SHADOW_COORDS(3)
                float4 worldTangent : TEXCOORD4;
                UNITY_FOG_COORDS(5)
            };

            v2f vertAdd (appdata v)
            {
                v2f o;

                float4 worldPos    = mul(unity_ObjectToWorld, v.vertex);
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);

                float4 tangentOS   = v.tangent;
                float3 worldTan    = UnityObjectToWorldDir(tangentOS.xyz);
                worldTan           = normalize(worldTan);
                float  tangentSign = tangentOS.w * unity_WorldTransformParams.w;

                float4 clipPos = UnityWorldToClipPos(worldPos);

                if (_UseWobble > 0.5)
                {
                    float2 ndc       = clipPos.xy / clipPos.w;
                    float2 screenPos = (ndc * 0.5 + 0.5) * _ScreenParams.xy;

                    float pixels = max(_WobblePixels, 1.0);
                    screenPos    = floor(screenPos / pixels) * pixels;

                    ndc          = (screenPos / _ScreenParams.xy) * 2.0 - 1.0;
                    clipPos.xy   = ndc * clipPos.w;
                }

                o.pos          = clipPos;
                o.uv           = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos     = worldPos.xyz;
                o.worldNormal  = worldNormal;
                o.worldTangent = float4(worldTan, tangentSign);

                UNITY_TRANSFER_SHADOW(o, worldPos);
                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            fixed4 fragAdd (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * _Color;

                if (_UseColorSteps > 0.5)
                {
                    float steps = max(_ColorSteps, 1.0);
                    tex.rgb = floor(tex.rgb * steps) / steps;
                }

                float alpha = tex.a;

                if (_UseAlphaClip > 0.5 && _UseDither > 0.5 && _DitherStrength > 0.0)
                {
                    float n   = frac(sin(dot(i.pos.xy, float2(12.9898, 78.233))) * 43758.5453);
                    float amp = _DitherStrength * 0.5;
                    alpha     = saturate(alpha + (n - 0.5) * (2.0 * amp));
                }

                if (_UseAlphaClip > 0.5)
                {
                    clip(alpha - _Cutoff);
                }

                float3 N = normalize(i.worldNormal);

                if (_UseNormalMap > 0.5)
                {
                    float2 bumpUV   = TRANSFORM_TEX(i.uv, _BumpMap);
                    float3 normalTS = UnpackNormal(tex2D(_BumpMap, bumpUV));

                    float3 T = normalize(i.worldTangent.xyz);
                    float3 B = cross(N, T) * i.worldTangent.w;
                    float3x3 TBN = float3x3(T, B, N);

                    float3 normalWS = mul(normalTS, TBN);
                    N = normalize(normalWS);
                }

                float3 L = normalize(UnityWorldSpaceLightDir(i.worldPos));
                float  NdotL = max(0.0, dot(N, L));

                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                float3 diffuse = tex.rgb * _LightColor0.rgb * NdotL * atten;

                fixed4 col = fixed4(diffuse, alpha);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
