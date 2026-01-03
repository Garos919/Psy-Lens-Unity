Shader "SHD_PSX_Water"
{
    Properties
    {
        // Base
        _MainTex ("Main Tex", 2D) = "white" {}
        _Color   ("Tint", Color)  = (0.10, 0.18, 0.20, 0.95)

        // Transparency
        _Alpha ("Alpha", Range(0, 1)) = 0.85

        // ----- MODE -----
        // 0 = Waves, 1 = Ripples
        _WaveMode ("Wave Mode (0=Waves,1=Ripples)", Float) = 0

        // ----- UNIVERSAL SHAPE SETTINGS -----
        _AmplitudeMin ("Amplitude Min", Range(0, 0.5)) = 0.02
        _AmplitudeMax ("Amplitude Max", Range(0, 0.5)) = 0.04

        _FrequencyMin ("Frequency Min", Range(0, 10))  = 0.5
        _FrequencyMax ("Frequency Max", Range(0, 10))  = 2.0

        _ShapeSize ("Shape Size", Range(0.1, 10)) = 1.0

        // ----- GLOBAL SETTINGS -----
        _Speed ("Animation Speed", Range(0, 5)) = 0.7

        _DistortStrength       ("Surface Distortion",     Range(0, 0.5)) = 0.05
        _WaterMovementStrength ("Shape Normal Intensity", Range(0, 2))   = 1.0

        _TimeChoppiness ("Movement Choppiness", Range(0, 1)) = 0.0
        _WaterPixelation("Water Pixelation",    Range(0, 1)) = 0.0

        // ----- TOGGLES -----
        _UseDistortion ("Use Distortion", Float) = 1
        _UseSpecular   ("Use Specular",   Float) = 1
        _UseSparkle    ("Use Sparkle",    Float) = 1

        // ----- SPECULAR -----
        _WaterSpecColor           ("Specular Color",        Color)         = (1,1,1,1)
        _WaterSpecIntensity       ("Specular Intensity",    Range(0, 1))   = 0.5
        // Height from which spec is fully visible (clamped 0..0.3)
        _WaterSpecHeightThreshold ("Spec Height Threshold", Range(0, 0.3)) = 0.0
        // 0 = hard edge at threshold, 1 = fade from base (0) up to threshold
        _WaterSpecFadeRange       ("Spec Fade Range",       Range(0, 1))   = 0.1

        // ----- SPARKLE -----
        _WaterSparkleIntensity ("Sparkle Amount",    Range(0, 1))    = 0.7
        _WaterSparkleScale     ("Sparkle Scale",     Range(0.1,100)) = 4.0
        _WaterSparkleSpeed     ("Sparkle Speed",     Range(0, 10))   = 2.0
        _WaterSparkleThreshold ("Sparkle Threshold", Range(0, 1))    = 0.6

        // ----- DEPTH FADE -----
        _DepthFadeDistance ("Depth Fade Distance", Range(0.01, 0.2)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        LOD 200
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        // =========================================================
        //                    FORWARD BASE
        // =========================================================
        Pass
        {
            Tags { "LightMode"="ForwardBase" }

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

            float _Alpha;

            float _WaveMode;

            float _AmplitudeMin;
            float _AmplitudeMax;
            float _FrequencyMin;
            float _FrequencyMax;
            float _ShapeSize;

            float _Speed;

            float _DistortStrength;
            float _WaterMovementStrength;

            float _TimeChoppiness;
            float _WaterPixelation;

            float _UseDistortion;
            float _UseSpecular;
            float _UseSparkle;

            fixed4 _WaterSpecColor;
            float  _WaterSpecIntensity;
            float  _WaterSpecHeightThreshold;
            float  _WaterSpecFadeRange;

            float  _WaterSparkleIntensity;
            float  _WaterSparkleScale;
            float  _WaterSparkleSpeed;
            float  _WaterSparkleThreshold;

            float  _DepthFadeDistance;

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            // -------- time quantization (choppy movement) --------
            float GetChoppyTime(float baseTime)
            {
                if (_TimeChoppiness <= 0.0)
                    return baseTime;

                float stepSize = lerp(0.02, 1.0, _TimeChoppiness);
                float tQuant   = floor(baseTime / stepSize) * stepSize;
                return tQuant;
            }

            // -------- value noise helpers --------
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // ----- WAVES height (directional-ish) -----
            float waveHeight_Waves(float3 worldPos, float t)
            {
                float size       = max(_ShapeSize, 0.001);
                float2 baseCoord = worldPos.xz / size;

                float freqNoise = valueNoise(baseCoord * 0.25);
                float localFreq = lerp(_FrequencyMin, _FrequencyMax, freqNoise);

                float2 p = baseCoord * (localFreq * 0.3);

                float n1    = valueNoise(p + float2(t * 0.21, t * 0.17));
                float n2    = valueNoise(p * 2.13 + float2(-t * 0.31, t * 0.23));
                float shape = n1 * 0.6 + n2 * 0.4; // [0,1]

                float peakShape = max(0.0, (shape - 0.5) * 2.0);

                float ampNoise = valueNoise(baseCoord * 0.7 + float2(-t * 0.13, t * 0.09));
                float amp      = lerp(_AmplitudeMin, _AmplitudeMax, ampNoise);

                return peakShape * amp; // >= 0
            }

            // ----- RIPPLES height (soft random surface motion) -----
            float waveHeight_Ripples(float3 worldPos, float t)
            {
                float size = max(_ShapeSize, 0.001);
                float2 p   = worldPos.xz / size;

                float freqMid = 0.5 * (_FrequencyMin + _FrequencyMax);
                float scale   = max(freqMid, 0.001);

                float tR = t;

                float2 p1 = p * scale * 0.15 + float2( tR * 0.25,  tR * 0.21);
                float2 p2 = p * scale * 0.30 + float2(-tR * 0.19,  tR * 0.17);
                float2 p3 = p * scale * 0.55 + float2( tR * 0.31, -tR * 0.29);

                float s1 = valueNoise(p1);
                float s2 = valueNoise(p2);
                float s3 = valueNoise(p3);

                float combined = (s1 * 0.5 + s2 * 0.35 + s3 * 0.15) * 2.0 - 1.0;

                float ampNoise = valueNoise(p * 0.4 + float2(-tR * 0.11, tR * 0.09));
                float amp      = lerp(_AmplitudeMin, _AmplitudeMax, ampNoise);

                float hSigned = combined * amp;
                return max(0.0, hSigned); // clamp to >= 0
            }

            // ----- Combined height, with pixelation -----
            float waveHeightFn(float3 worldPos, float t)
            {
                float h;

                if (_WaveMode < 0.5)
                    h = waveHeight_Waves(worldPos, t);
                else
                    h = waveHeight_Ripples(worldPos, t);

                if (_WaterPixelation > 0.0)
                {
                    float maxSteps = 64.0;
                    float minSteps = 4.0;
                    float steps    = lerp(maxSteps, minSteps, _WaterPixelation);
                    steps          = max(1.0, steps);
                    h              = floor(h * steps) / steps;
                }

                return max(0.0, h);
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                SHADOW_COORDS(3)
                UNITY_FOG_COORDS(4)
                float4 projPos     : TEXCOORD5;   // for depth fade
            };

            v2f vert (appdata v)
            {
                v2f o;

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

                float tRaw = _Time.y * _Speed;
                float t    = GetChoppyTime(tRaw);

                float wave = waveHeightFn(worldPos.xyz, t);
                worldPos.y += wave;

                o.pos      = UnityWorldToClipPos(worldPos);
                o.worldPos = worldPos.xyz;

                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv          = TRANSFORM_TEX(v.uv, _MainTex);

                UNITY_TRANSFER_SHADOW(o, worldPos);
                UNITY_TRANSFER_FOG(o, o.pos);

                // screen-space position for depth sampling
                o.projPos = ComputeScreenPos(o.pos);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float tRaw = _Time.y * _Speed;
                float t    = GetChoppyTime(tRaw);

                float useDistortion = step(0.5, _UseDistortion);
                float useSpecular   = step(0.5, _UseSpecular);
                float useSparkle    = step(0.5, _UseSparkle);

                // --- approximate slope from height field ---
                float eps = 0.05;

                float hL = waveHeightFn(i.worldPos + float3(-eps, 0, 0), t);
                float hR = waveHeightFn(i.worldPos + float3( eps, 0, 0), t);
                float hD = waveHeightFn(i.worldPos + float3(0, 0, -eps), t);
                float hU = waveHeightFn(i.worldPos + float3(0, 0,  eps), t);

                float dhdx = (hR - hL) / (2.0 * eps);
                float dhdz = (hU - hD) / (2.0 * eps);

                // UV distortion driven by slope
                float2 distort = float2(dhdx, dhdz) * _DistortStrength * useDistortion;
                float2 uv      = i.uv + distort;

                fixed4 tex = tex2D(_MainTex, uv) * _Color;
                tex.a *= _Alpha;

                float3 baseCol = tex.rgb;

                // normals from slope, scaled by water movement strength
                float3 N = normalize(float3(-dhdx * _WaterMovementStrength,
                                            1.0,
                                            -dhdz * _WaterMovementStrength));

                float3 L     = normalize(UnityWorldSpaceLightDir(i.worldPos));
                float  NdotL = max(0.0, dot(N, L));

                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                float3 diffuse = baseCol * _LightColor0.rgb * NdotL * atten * 0.5;
                float3 ambient = baseCol * UNITY_LIGHTMODEL_AMBIENT.rgb;

                // ----- HEIGHT-BASED SPECULAR MASK -----
                float h  = waveHeightFn(i.worldPos, t);
                float Ht = _WaterSpecHeightThreshold;

                float heightMask;
                if (Ht <= 1e-4)
                {
                    // Threshold 0 => full plane
                    heightMask = 1.0;
                }
                else
                {
                    float fadeFrac = saturate(_WaterSpecFadeRange);
                    float effFade  = fadeFrac * Ht;

                    if (effFade <= 1e-4)
                    {
                        heightMask = step(Ht, h);
                    }
                    else
                    {
                        float fadeStart = Ht - effFade;
                        if (h >= Ht)
                            heightMask = 1.0;
                        else if (h <= fadeStart)
                            heightMask = 0.0;
                        else
                            heightMask = (h - fadeStart) / effFade;
                    }
                }

                float lightTerm      = saturate(NdotL * atten);
                float baseSpecFactor = heightMask * lightTerm * _WaterSpecIntensity;

                // Apply specular toggle
                baseSpecFactor *= useSpecular;

                // ----- SPARKLE: per-cell independent flicker -----
                float sparkleBoost = 1.0;

                if (useSparkle > 0.0 && _WaterSparkleIntensity > 0.001 && baseSpecFactor > 0.0)
                {
                    float cellSize = max(0.01, 1.0 / _WaterSparkleScale);
                    float2 worldXZ = i.worldPos.xz;

                    float2 cellId = floor(worldXZ / cellSize);

                    float rBase  = hash21(cellId);
                    float rFreq  = hash21(cellId + 17.23);
                    float rPhase = hash21(cellId + 91.47);

                    float hasSparkle = step(_WaterSparkleThreshold, rBase);

                    if (hasSparkle > 0.0)
                    {
                        float freq  = lerp(0.5, 2.0, rFreq) * _WaterSparkleSpeed;
                        float phase = rPhase * 6.2831853;

                        float s = sin(_Time.y * freq + phase);
                        float sparkle = saturate(s * 0.5 + 0.5);
                        sparkle = pow(sparkle, 6.0);

                        float sparkleMask = sparkle * hasSparkle;

                        sparkleBoost = 1.0 + sparkleMask * _WaterSparkleIntensity * 2.0;
                    }
                }

                float specFactor = baseSpecFactor * sparkleBoost;

                float3 litBase  = ambient + diffuse;
                float  w        = saturate(specFactor);
                float3 finalCol = lerp(litBase, _WaterSpecColor.rgb, w);

                // ----- DEPTH FADE (like fog plane) -----
                float sceneDepthRaw = UNITY_SAMPLE_DEPTH(
                    tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))
                );

                float sceneDepth = LinearEyeDepth(sceneDepthRaw);
                float waterDepth = LinearEyeDepth(i.projPos.z / i.projPos.w);

                float distToGeom = sceneDepth - waterDepth; // >0 when water is in front
                float fade = saturate(distToGeom / _DepthFadeDistance);

                finalCol *= fade;
                tex.a    *= fade;

                fixed4 col = fixed4(finalCol, tex.a);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

        // =========================================================
        //                    FORWARD ADD
        // =========================================================
        Pass
        {
            Tags { "LightMode"="ForwardAdd" }
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

            float _Alpha;

            float _WaveMode;

            float _AmplitudeMin;
            float _AmplitudeMax;
            float _FrequencyMin;
            float _FrequencyMax;
            float _ShapeSize;

            float _Speed;

            float _DistortStrength;
            float _WaterMovementStrength;

            float _TimeChoppiness;
            float _WaterPixelation;

            float _UseDistortion;
            float _UseSpecular;
            float _UseSparkle;

            fixed4 _WaterSpecColor;
            float  _WaterSpecIntensity;
            float  _WaterSpecHeightThreshold;
            float  _WaterSpecFadeRange;

            float  _WaterSparkleIntensity;
            float  _WaterSparkleScale;
            float  _WaterSparkleSpeed;
            float  _WaterSparkleThreshold;

            float  _DepthFadeDistance;

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            float GetChoppyTime(float baseTime)
            {
                if (_TimeChoppiness <= 0.0)
                    return baseTime;

                float stepSize = lerp(0.02, 1.0, _TimeChoppiness);
                float tQuant   = floor(baseTime / stepSize) * stepSize;
                return tQuant;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float waveHeight_Waves(float3 worldPos, float t)
            {
                float size       = max(_ShapeSize, 0.001);
                float2 baseCoord = worldPos.xz / size;

                float freqNoise = valueNoise(baseCoord * 0.25);
                float localFreq = lerp(_FrequencyMin, _FrequencyMax, freqNoise);

                float2 p = baseCoord * (localFreq * 0.3);

                float n1    = valueNoise(p + float2(t * 0.21, t * 0.17));
                float n2    = valueNoise(p * 2.13 + float2(-t * 0.31, t * 0.23));
                float shape = n1 * 0.6 + n2 * 0.4;

                float peakShape = max(0.0, (shape - 0.5) * 2.0);

                float ampNoise = valueNoise(baseCoord * 0.7 + float2(-t * 0.13, t * 0.09));
                float amp      = lerp(_AmplitudeMin, _AmplitudeMax, ampNoise);

                return peakShape * amp;
            }

            float waveHeight_Ripples(float3 worldPos, float t)
            {
                float size = max(_ShapeSize, 0.001);
                float2 p   = worldPos.xz / size;

                float freqMid = 0.5 * (_FrequencyMin + _FrequencyMax);
                float scale   = max(freqMid, 0.001);

                float tR = t;

                float2 p1 = p * scale * 0.15 + float2( tR * 0.25,  tR * 0.21);
                float2 p2 = p * scale * 0.30 + float2(-tR * 0.19,  tR * 0.17);
                float2 p3 = p * scale * 0.55 + float2( tR * 0.31, -tR * 0.29);

                float s1 = valueNoise(p1);
                float s2 = valueNoise(p2);
                float s3 = valueNoise(p3);

                float combined = (s1 * 0.5 + s2 * 0.35 + s3 * 0.15) * 2.0 - 1.0;

                float ampNoise = valueNoise(p * 0.4 + float2(-tR * 0.11, tR * 0.09));
                float amp      = lerp(_AmplitudeMin, _AmplitudeMax, ampNoise);

                float hSigned = combined * amp;
                return max(0.0, hSigned);
            }

            float waveHeightFn(float3 worldPos, float t)
            {
                float h;

                if (_WaveMode < 0.5)
                    h = waveHeight_Waves(worldPos, t);
                else
                    h = waveHeight_Ripples(worldPos, t);

                if (_WaterPixelation > 0.0)
                {
                    float maxSteps = 64.0;
                    float minSteps = 4.0;
                    float steps    = lerp(maxSteps, minSteps, _WaterPixelation);
                    steps          = max(1.0, steps);
                    h              = floor(h * steps) / steps;
                }

                return max(0.0, h);
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                SHADOW_COORDS(3)
                UNITY_FOG_COORDS(4)
                float4 projPos     : TEXCOORD5; // for depth fade
            };

            v2f vertAdd (appdata v)
            {
                v2f o;

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

                float tRaw = _Time.y * _Speed;
                float t    = GetChoppyTime(tRaw);

                float wave = waveHeightFn(worldPos.xyz, t);
                worldPos.y += wave;

                o.pos      = UnityWorldToClipPos(worldPos);
                o.worldPos = worldPos.xyz;

                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv          = TRANSFORM_TEX(v.uv, _MainTex);

                UNITY_TRANSFER_SHADOW(o, worldPos);
                UNITY_TRANSFER_FOG(o, o.pos);

                o.projPos = ComputeScreenPos(o.pos);

                return o;
            }

            fixed4 fragAdd (v2f i) : SV_Target
            {
                float tRaw = _Time.y * _Speed;
                float t    = GetChoppyTime(tRaw);

                float useDistortion = step(0.5, _UseDistortion);
                float useSpecular   = step(0.5, _UseSpecular);
                float useSparkle    = step(0.5, _UseSparkle);

                float eps = 0.05;

                float hL = waveHeightFn(i.worldPos + float3(-eps, 0, 0), t);
                float hR = waveHeightFn(i.worldPos + float3( eps, 0, 0), t);
                float hD = waveHeightFn(i.worldPos + float3(0, 0, -eps), t);
                float hU = waveHeightFn(i.worldPos + float3(0, 0,  eps), t);

                float dhdx = (hR - hL) / (2.0 * eps);
                float dhdz = (hU - hD) / (2.0 * eps);

                float2 distort = float2(dhdx * _DistortStrength * useDistortion,
                                        dhdz * _DistortStrength * useDistortion);
                float2 uv      = i.uv + distort;

                fixed4 tex = tex2D(_MainTex, uv) * _Color;
                tex.a *= _Alpha;

                float3 baseCol = tex.rgb;

                float3 N = normalize(float3(-dhdx * _WaterMovementStrength,
                                            1.0,
                                            -dhdz * _WaterMovementStrength));

                float3 L     = normalize(UnityWorldSpaceLightDir(i.worldPos));
                float  NdotL = max(0.0, dot(N, L));

                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                float3 diffuse = baseCol * _LightColor0.rgb * NdotL * atten * 0.5;

                float h  = waveHeightFn(i.worldPos, t);
                float Ht = _WaterSpecHeightThreshold;

                float heightMask;
                if (Ht <= 1e-4)
                {
                    heightMask = 1.0;
                }
                else
                {
                    float fadeFrac = saturate(_WaterSpecFadeRange);
                    float effFade  = fadeFrac * Ht;

                    if (effFade <= 1e-4)
                    {
                        heightMask = step(Ht, h);
                    }
                    else
                    {
                        float fadeStart = Ht - effFade;
                        if (h >= Ht)
                            heightMask = 1.0;
                        else if (h <= fadeStart)
                            heightMask = 0.0;
                        else
                            heightMask = (h - fadeStart) / effFade;
                    }
                }

                float lightTerm      = saturate(NdotL * atten);
                float baseSpecFactor = heightMask * lightTerm * _WaterSpecIntensity;

                baseSpecFactor *= useSpecular;

                float sparkleBoost = 1.0;

                if (useSparkle > 0.0 && _WaterSparkleIntensity > 0.001 && baseSpecFactor > 0.0)
                {
                    float cellSize = max(0.01, 1.0 / _WaterSparkleScale);
                    float2 worldXZ = i.worldPos.xz;

                    float2 cellId = floor(worldXZ / cellSize);

                    float rBase  = hash21(cellId);
                    float rFreq  = hash21(cellId + 17.23);
                    float rPhase = hash21(cellId + 91.47);

                    float hasSparkle = step(_WaterSparkleThreshold, rBase);

                    if (hasSparkle > 0.0)
                    {
                        float freq  = lerp(0.5, 2.0, rFreq) * _WaterSparkleSpeed;
                        float phase = rPhase * 6.2831853;

                        float s = sin(_Time.y * freq + phase);
                        float sparkle = saturate(s * 0.5 + 0.5);
                        sparkle = pow(sparkle, 6.0);

                        float sparkleMask = sparkle * hasSparkle;
                        sparkleBoost = 1.0 + sparkleMask * _WaterSparkleIntensity * 2.0;
                    }
                }

                float specFactor = baseSpecFactor * sparkleBoost;

                float  w       = saturate(specFactor);
                float3 result  = lerp(diffuse, _WaterSpecColor.rgb, w);

                // depth fade
                float sceneDepthRaw = UNITY_SAMPLE_DEPTH(
                    tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))
                );

                float sceneDepth = LinearEyeDepth(sceneDepthRaw);
                float waterDepth = LinearEyeDepth(i.projPos.z / i.projPos.w);

                float distToGeom = sceneDepth - waterDepth;
                float fade = saturate(distToGeom / _DepthFadeDistance);

                result *= fade;

                fixed4 col = fixed4(result, tex.a);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }

    CustomEditor "SHD_PSX_Water_GUI"
    FallBack "Transparent/Diffuse"
}
