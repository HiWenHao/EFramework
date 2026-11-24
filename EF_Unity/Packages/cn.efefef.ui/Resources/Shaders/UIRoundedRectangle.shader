// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "UI/RoundedRectangle"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
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
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                //  圆角参数走顶点通道（需 Canvas 开启 TexCoord1/2/3），与材质解耦以便共享合批。
                float2 uv1      : TEXCOORD1;   // localPos：相对矩形中心的本地坐标
                float2 uv2      : TEXCOORD2;   // halfSize：半宽、半高
                float2 uv3      : TEXCOORD3;   // x=圆角半径，y=每像素本地单位
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex      : SV_POSITION;
                fixed4 color       : COLOR;
                float2 texcoord    : TEXCOORD0;
                float2 localPos    : TEXCOORD1;
                float2 halfSize    : TEXCOORD2;
                float2 radiusPixel : TEXCOORD3;
                half4  mask        : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            //  Gamma->Linear 转换，与 UI/Default 及灰度 shader 保持一致
            half3 GammaToLinear(half3 gammaColor)
            {
                return pow(gammaColor, 2.2);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                OUT.mask = half4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
                    0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                OUT.localPos = v.uv1;
                OUT.halfSize = v.uv2;
                OUT.radiusPixel = v.uv3;

                //  线性空间下顶点色若为 gamma 编码则先转换，与 UI/Default 及灰度 shader 一致
                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                    v.color.rgb = GammaToLinear(v.color.rgb);

                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                //  把插值器带来的 alpha 误差向上取整到 1/256，避免 HDR 混合时边缘发花
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision) * invAlphaPrecision;

                half4 color = IN.color * (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);

                //  安全降级：Canvas 未开 TexCoord1/2/3 时 uv1/2/3 全为 0，
                //  halfSize 兜到 1 后 dist 恒为负，圆角退化为"不裁剪"，图片仍完整可见。
                float2 halfSize = max(IN.halfSize, float2(1.0, 1.0));
                float radius = clamp(IN.radiusPixel.x, 0.0, min(halfSize.x, halfSize.y));
                float2 q = abs(IN.localPos) - halfSize + radius;
                float dist = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
                half aaWidth = clamp(half(IN.radiusPixel.y), half(0.0001), half(min(halfSize.x, halfSize.y) * 0.5));
                half cornerAlpha = saturate(half(0.5) - dist / aaWidth);
                color.a *= cornerAlpha;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                color.rgb *= color.a;
                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
