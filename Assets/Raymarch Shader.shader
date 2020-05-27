Shader "Unlit/Raymarch Shader"
{
    Properties
    {
        _DepthTexture ("Depth Texture", 2D) = "white" {}
        _WorldEdgeLength("World Edge Length", Int) = 0
        _Epsilon ("Epsilon", Range(0.00001, 0.025)) = 0.0005
        _AOIntensity ("AO Intensity", Range(0.0, 10.0)) = 0.5
        _AOGrade ("AO Grade", Range(0.0, 50.0)) = 2.0
        _AORange ("AO Range", Range(0.0, 1.0)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        LOD 100

        Pass
        {
            ZWrite Off
            ZTest Always

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Raymarch.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 clip : SV_POSITION;
                float3 rayOrigin : TEXCOORD0;
                float3 rayDestination : TEXCOORD1;
            };

            uniform StructuredBuffer<float> _AccelerationData;
            uniform int _WorldEdgeLength;
            uniform float _Epsilon;

            v2f vert(appdata vert)
            {
                v2f frag;
                frag.clip = UnityObjectToClipPos(vert.vertex);
                frag.rayOrigin = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                frag.rayDestination = vert.vertex;
                return frag;
            }

            float frag(v2f frag) : SV_TARGET
            {
                float3 rayDirection = normalize(frag.rayDestination - frag.rayOrigin);
                frag.rayOrigin += rayDirection * _ProjectionParams.y;

                float depth;
                raymarch(_AccelerationData, _WorldEdgeLength, _Epsilon, frag.rayOrigin, rayDirection, depth);
                return depth;
            }

            ENDCG
        }
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Raymarch.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 clip : SV_POSITION;
                float3 rayOrigin : TEXCOORD0;
                float3 rayDestination : TEXCOORD1;
                float4 screen : TEXCOORD2;
            };

            uniform sampler2D_float _DepthTexture;
            uniform StructuredBuffer<float> _AccelerationData;

            uniform fixed4 _ColorPalette[32];

            uniform int _WorldEdgeLength;
            uniform float _Epsilon;
            uniform float _AOIntensity;
            uniform float _AOGrade;
            uniform float _AORange;

            v2f vert(appdata vert)
            {
                v2f frag;
                frag.clip = UnityObjectToClipPos(vert.vertex);
                frag.rayOrigin = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                frag.rayDestination = vert.vertex;
                frag.screen = ComputeScreenPos(frag.clip);
                return frag;
            }

            fixed4 frag(v2f frag) : SV_TARGET
            {
                float2 screen = frag.screen.xy / frag.screen.w;
                float3 rayDirection = normalize(frag.rayDestination - frag.rayOrigin);
                frag.rayOrigin += rayDirection * _ProjectionParams.y;

                float depth;
                float jump = raymarch(_AccelerationData, _WorldEdgeLength, _Epsilon, frag.rayOrigin, rayDirection, depth);

                if (jump < 1.0)
                {
                    discard;
                }

                float colorNoise = frac(jump);
                int paletteIndex = int(jump - 1.0);
                return _ColorPalette[paletteIndex] + colorNoise;
            }

            ENDCG
        }
    }
}
