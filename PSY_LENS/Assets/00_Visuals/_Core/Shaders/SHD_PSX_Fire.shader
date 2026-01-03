Shader "SHD_PixelGrid_Flicker"
{
    Properties
    {
        _Color          ("Edge Color", Color)                 = (1.0, 0.6, 0.2, 1.0)
        _CoreColor      ("Core Color", Color)                 = (0.8, 0.25, 0.0, 1.0)

        // Uniform pixel size in UV space
        _PixelSize      ("Pixel Size", Range(0.01, 0.5))      = 0.1

        // How often pixels change (steps per second)
        _FlickerSpeed   ("Flicker Speed", Range(0, 20))       = 8.0

        // How much brightness flickers (0 = stable, 1 = full random)
        _BrightnessFlicker ("Brightness Flicker", Range(0, 1)) = 0.4

        // Controls how strongly black areas stay opaque (1 = linear, >1 = stronger core)
        _OpacityBias    ("Opacity Bias", Range(0.1, 5.0))     = 1.5

        // How much the mask uses randomness vs a more stable interpretation
        _Randomness     ("Mask Randomness", Range(0, 1))      = 0.5

        // How strongly color changes with depth into the black region
        _DepthPower     ("Depth Power", Range(0.1, 5.0))      = 2.0

        // Alpha gradient: white = transparent, black = base for opacity probability
        _AlphaTex       ("Alpha Gradient (white=transparent)", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off   // both sides

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            fixed4 _CoreColor;
            float  _PixelSize;
            float  _FlickerSpeed;
            float  _BrightnessFlicker;
            float  _OpacityBias;
            float  _Randomness;
            float  _DepthPower;

            sampler2D _AlphaTex;
            float4    _AlphaTex_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;   // assume 0–1 UVs
                return o;
            }

            // 2D hash -> 0..1
            float hash21(float2 p)
            {
                p = frac(p * float2(0.1031, 0.11369));
                p += dot(p, p.yx + 33.33);
                return frac(p.x * p.y);
            }

            // Sample mask at the CENTER of a given pixel cell (in cell coordinates)
            float sampleMaskAtCell(float2 cell)
            {
                float2 uvCenter = (cell + 0.5) * _PixelSize;
                uvCenter = clamp(uvCenter, 0.0, 1.0);
                float m = tex2D(_AlphaTex, TRANSFORM_TEX(uvCenter, _AlphaTex)).r;
                return saturate(m); // 0 = black, 1 = white
            }

            // Depth inside black region in pixel grid:
            // how many steps LEFT/RIGHT we can move before hitting grey/white.
            // Bottom is allowed to be deep; only side proximity to grey reduces depth.
            float computeDepthFromEdgeCell(float2 cell)
            {
                const float greyThreshold = 0.3;   // above this = edge/white
                const int   MAX_STEPS    = 6;

                float minStepsToEdge = (float)MAX_STEPS;

                // Only check from the sides (left/right)
                float2 dirs[2];
                dirs[0] = float2( 1.0, 0.0);
                dirs[1] = float2(-1.0, 0.0);

                [unroll]
                for (int d = 0; d < 2; d++)
                {
                    float2 c = cell;
                    float  stepsTaken = (float)MAX_STEPS;

                    [unroll]
                    for (int s = 1; s <= MAX_STEPS; s++)
                    {
                        c += dirs[d]; // move 1 pixel-cell left/right
                        float m = sampleMaskAtCell(c);

                        if (m >= greyThreshold)
                        {
                            stepsTaken = (float)(s - 1);
                            break;
                        }
                    }

                    minStepsToEdge = min(minStepsToEdge, stepsTaken);
                }

                float depth = minStepsToEdge / (float)MAX_STEPS; // 0 at side edge, 1 deep in horizontally
                return saturate(depth);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Pixel cell index from uniform pixel size
                float2 cell = floor(i.uv / _PixelSize + 0.0001);

                // Discrete time step; each integer step changes random state
                float timeIndex = floor(_Time.y * _FlickerSpeed);

                // Base random for brightness (0..1)
                float baseRand = hash21(cell + timeIndex * float2(19.19, -27.41));

                // Flicker: blend between stable (1.0) and full random (0..1)
                float flicker = lerp(1.0, baseRand, _BrightnessFlicker);

                // Mask at this cell (center), black = 0, white = 1
                float mask = sampleMaskAtCell(cell);

                // Horizontal depth from edge inside black region
                float depthFromEdge = computeDepthFromEdgeCell(cell);

                // Local darkness (so near-grey can't become super "deep")
                float localDark = 1.0 - mask; // 1 = black, 0 = white

                // Final depth factor:
                //  - bottom black pixels can be deep
                //  - if they are horizontally close to grey/white, depthFromEdge lowers this
                float depthFactor = pow(depthFromEdge * localDark, _DepthPower);

                // Deeper inside black region (horizontally) → closer to _CoreColor
                float3 baseColor = lerp(_Color.rgb, _CoreColor.rgb, depthFactor);

                // Random value for alpha decision
                float randAlpha = hash21(cell + timeIndex * float2(-11.73, 7.91));

                // Fully random probability from mask (black = 1, white = 0)
                float probOpaqueRandom = pow(1.0 - mask, _OpacityBias);

                // Stable interpretation: black-ish <0.5 → opaque, white-ish ≥0.5 → transparent
                float probOpaqueStable = step(mask, 0.5);

                // Blend between stable and random behavior
                float probOpaque = lerp(probOpaqueStable, probOpaqueRandom, _Randomness);

                // Binary alpha, 0 or 1
                float alphaBin = (randAlpha <= probOpaque) ? 1.0 : 0.0;

                fixed4 col;
                col.rgb = baseColor * flicker;
                col.a   = alphaBin;

                return col;
            }
            ENDCG
        }
    }
}
