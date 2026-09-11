Shader "Rosette/CockpitWindow"
{
 Properties {
  [PerRendererData] _MainTex("Frame", 2D) = "white" {}
  _Window("Window left,bottom,right,top", Vector) = (0.115,0.175,0.884,0.885)
 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct a { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   struct v { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   sampler2D _MainTex; float4 _Window;
   v vert(a i) { v o; o.vertex=UnityObjectToClipPos(i.vertex); o.uv=i.uv; o.color=i.color; return o; }
   fixed4 frag(v i):SV_Target {
    fixed4 c=tex2D(_MainTex,i.uv)*i.color;
    // This material cuts the generated preview fill away without changing its source image.
    if(i.uv.x>_Window.x && i.uv.x<_Window.z && i.uv.y>_Window.y && i.uv.y<_Window.w) c.a=0;
    return c;
   }
   ENDCG
  }
 }
}
