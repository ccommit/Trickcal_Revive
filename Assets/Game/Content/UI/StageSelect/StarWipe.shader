Shader "TrickcalRevive/StarWipe"
{
    // 화면 전환용 UGUI 셰이더. 풀스크린을 _Color(초록)로 덮되, _Center 기준 별 모양
    // 구멍 안쪽은 투명(뒤 화면이 보인다). _Progress 0 = 구멍 큼(화면 보임), 1 = 닫힘(전부 초록).
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.56, 0.82, 0.31, 1)
        _Progress ("Progress", Range(0,1)) = 0
        _CenterX ("Center X", Float) = 0.82
        _CenterY ("Center Y", Float) = 0.15
        _Aspect ("Aspect", Float) = 1.7778
        _MaxRadius ("Max Radius", Float) = 2.1
        _InnerRatio ("Star Inner Ratio", Float) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
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
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _Progress;
            float _CenterX;
            float _CenterY;
            float _Aspect;
            float _MaxRadius;
            float _InnerRatio;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv - float2(_CenterX, _CenterY);
                p.x *= _Aspect;
                float ang = atan2(p.y, p.x);
                float r = length(p);

                // 5-point rounded star edge (spikes where cos(5θ)=1)
                float star = 0.5 + 0.5 * cos(5.0 * ang);
                float edge = lerp(_InnerRatio, 1.0, star) * _MaxRadius;

                float holeScale = 1.0 - _Progress;      // 1 = 구멍 최대, 0 = 닫힘
                float threshold = edge * holeScale;

                // 별 구멍 안쪽이면 투명, 아니면 초록
                float a = step(threshold, r) * _Color.a;
                return fixed4(_Color.rgb, a) * i.color;
            }
            ENDCG
        }
    }
}
