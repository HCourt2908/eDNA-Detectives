Shader "Rosette/OceanSurfaceRefraction"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Tint ("Refraction Tint", Color) = (0.65, 0.94, 1, 1)
        _SurfaceFactor ("Surface Factor", Range(0, 1)) = 1
        _WaveTime ("Wave Time", Float) = 0
        _WaveStrength ("Wave Strength", Range(0, 1)) = 0.18
        _Opacity ("Opacity", Range(0, 1)) = 0.34
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Tint;
            float _SurfaceFactor;
            float _WaveTime;
            float _WaveStrength;
            float _Opacity;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 source = tex2D(_MainTex, input.texcoord) * input.color;
                float surfaceMask = smoothstep(0.48, 1.0, input.texcoord.y);
                float wave = sin(input.texcoord.x * 34.0 + input.texcoord.y * 5.0 + _WaveTime) * 0.5 + 0.5;
                float ripple = smoothstep(0.72, 0.98, wave) * _WaveStrength;
                fixed4 colour = _Tint;
                colour.rgb *= (0.85 + ripple);
                colour.a = source.a * surfaceMask * ripple * _Opacity * _SurfaceFactor;
                return colour;
            }
            ENDCG
        }
    }
}
