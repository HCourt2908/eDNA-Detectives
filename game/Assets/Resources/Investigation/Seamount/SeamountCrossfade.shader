Shader "EDNA/UI/Seamount Crossfade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Natural", 2D) = "white" {}
        _IllustratedTex ("Illustrated", 2D) = "white" {}
        _BathymetryTex ("Bathymetry", 2D) = "white" {}
        _SurfaceMask ("Terrain silhouette", 2D) = "white" {}
        _BlendB ("Illustrated blend", Range(0,1)) = 0
        _BlendC ("Bathymetry blend", Range(0,1)) = 0
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 localPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _MainTex;
            sampler2D _IllustratedTex;
            sampler2D _BathymetryTex;
            sampler2D _SurfaceMask;
            half4 _Color;
            float4 _ClipRect;
            half _BlendB;
            half _BlendC;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.localPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 a = tex2D(_MainTex, i.uv);
                half4 b = tex2D(_IllustratedTex, i.uv);
                half4 c = tex2D(_BathymetryTex, i.uv);
                half4 result = lerp(lerp(a, b, _BlendB), c, _BlendC) * i.color;
                // One terrain mask after blending keeps the silhouette constant
                // while the mountain changes appearance.
                result.a *= tex2D(_SurfaceMask, i.uv).r;
                result.a *= smoothstep(0, 0.05, i.uv.x) * smoothstep(0, 0.05, 1 - i.uv.x)
                    * smoothstep(0, 0.08, i.uv.y);
                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(i.localPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
