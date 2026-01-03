Shader "SHD_PSX_DistandFog"
{
    Properties
    {
        // No color picker needed; it will always use the global fog color
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }

        // Only render the inner faces of the cube
        Cull Front

        Lighting Off
        ZWrite On
        ZTest LEqual
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            // Global fog color provided by Unity
            // (declared in UnityCG.cginc)
            // fixed4 unity_FogColor; // no need to redeclare, just use it

            fixed4 frag (v2f i) : SV_Target
            {
                // The cube is literally “made of fog”:
                // always render using the global fog color.
                return unity_FogColor;
            }
            ENDCG
        }
    }
}
