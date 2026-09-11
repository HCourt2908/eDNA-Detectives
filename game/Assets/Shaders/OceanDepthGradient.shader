Shader "Rosette/OceanDepthGradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _TopColor ("Surface Color", Color) = (0.20, 0.60, 0.86, 1)
        _BottomColor ("Deep Color", Color) = (0.01, 0.04, 0.14, 1)
        _Depth01 ("Depth", Range(0, 1)) = 0
        _NoiseTime ("Noise Time", Float) = 0
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.15
        _Opacity ("Opacity", Range(0, 1)) = 0.45
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
            fixed4 _TopColor;
            fixed4 _BottomColor;
            float _Depth01;
            float _NoiseTime;
            float _NoiseStrength;
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
                float waveA = sin(input.texcoord.x * 18.0 + input.texcoord.y * 7.0 + _NoiseTime) * 0.5;
                float waveB = cos(input.texcoord.x * 7.0 - input.texcoord.y * 21.0 + _NoiseTime * 0.67) * 0.5;
                float noise = (waveA + waveB) * _NoiseStrength;
                float depthBlend = saturate(input.texcoord.y + noise + _Depth01 * 0.08);
                fixed4 gradient = lerp(_TopColor, _BottomColor, depthBlend);
                float dim = lerp(1.0, 0.62, _Depth01);
                gradient.rgb *= dim;
                gradient.a *= source.a * _Opacity;
                return gradient;
            }
            ENDCG
        }
    }
}
