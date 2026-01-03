Shader "Custom/FogRandomAmplitude"
{
    Properties
    {
        _MainTex     ("Texture (RGBA)", 2D) = "white" {}
        _Color       ("Tint Color", Color)  = (1,1,1,1)
        _GlobalAlpha ("Global Alpha", Range(0,1)) = 1

        // X,Y = UV scroll direction for the texture
        // Z   = wave heading on plane (combined with X)
        _Scroll      ("Scroll XYZ", Vector) = (0,0,1,0)

        // Texture scrolling speed 0..1 (internally 0..0.1)
        _ScrollSpeed ("Scroll Speed", Range(0,1)) = 0.5

        // Wave motion speed 0..1 (internally 0..0.2 of the old range)
        _WaveSpeed   ("Wave Speed", Range(0,1)) = 0.5

        // Each vertex gets a RANDOM peak amplitude between these two values
        _AmplitudeMin ("Amplitude Min", Range(0,1)) = 0.0
        _AmplitudeMax ("Amplitude Max", Range(0,1)) = 0.2

        // Wave frequency along heading (spacing)
        _WaveFrequency ("Wave Frequency", Range(0,10)) = 1.0

        // Depth fade distance from geometry (0 alpha at contact, full alpha at this distance)
        _DepthFadeDistance ("Depth Fade Distance", Range(0.01, 5)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"      = "Transparent+10"
            "RenderType" = "Transparent"
        }

        LOD 100

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;

            fixed4    _Color;
            float     _GlobalAlpha;
            float4    _Scroll;
            float     _ScrollSpeed;
            float     _WaveSpeed;
            float     _AmplitudeMin;
            float     _AmplitudeMax;
            float     _WaveFrequency;
            float     _DepthFadeDistance;

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos     : SV_POSITION;
                float2 uv      : TEXCOORD0;
                float4 projPos : TEXCOORD1;   // for depth sampling
            };

            // stable pseudo-random 0..1 based only on position
            float rand2(float2 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * (p.x + p.y));
            }

            v2f vert(appdata v)
            {
                v2f o;

                float t = _Time.y;

                // map 0..1 slider to 0..0.1 effective speed for texture scroll
                float effectiveScrollSpeed = _ScrollSpeed * 0.1;

                // texture UV scroll (XY) with mapped speed
                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);
                uv += _Scroll.xy * (effectiveScrollSpeed * t);
                o.uv = uv;

                // plane position in XZ
                float2 p = v.vertex.xz;

                // random peak amplitude per-vertex between min and max
                float r   = rand2(p * 2.173);
                float amp = lerp(_AmplitudeMin, _AmplitudeMax, r);

                // wave heading from Scroll XZ
                float2 waveDir = float2(_Scroll.x, _Scroll.z);
                float  len     = length(waveDir);
                if (len < 0.0001)
                    waveDir = float2(1.0, 0.0); // default direction if zero
                else
                    waveDir /= len;

                // map 0..1 WaveSpeed to old 0..0.2 range
                float effectiveWaveSpeed = _WaveSpeed * 0.2;

                // waves: spacing from WaveFrequency, motion from effectiveWaveSpeed
                float phase  = dot(p, waveDir) * _WaveFrequency + t * effectiveWaveSpeed * 5.0;
                float wave01 = 0.5 + 0.5 * sin(phase);   // 0..1

                // final vertical offset: 0..amp, never below original plane
                v.vertex.y += wave01 * amp;

                // position + screen pos for depth fade
                o.pos     = UnityObjectToClipPos(v.vertex);
                o.projPos = ComputeScreenPos(o.pos);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col.a *= _GlobalAlpha;

                // ---- depth fade against scene geometry ----
                float sceneDepthRaw = UNITY_SAMPLE_DEPTH(
                    tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))
                );

                float sceneDepth = LinearEyeDepth(sceneDepthRaw);
                float fogDepth   = LinearEyeDepth(i.projPos.z / i.projPos.w);

                float distToGeom = sceneDepth - fogDepth;        // >0 when fog is in front
                float fade = saturate(distToGeom / _DepthFadeDistance);

                col.a *= fade;

                return col;
            }

            ENDCG
        }
    }

    FallBack Off
}
