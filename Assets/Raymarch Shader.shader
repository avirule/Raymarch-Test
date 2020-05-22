Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _RaymarchTexture ("Raymarch Texture", 3D) = "white" {}
        _MaximumRaySteps ("Maximum Ray Steps", Range(0, 128)) = 32
        _MaximumRayDistance ("Maximum Ray Distance", Range(0, 128)) = 32
        _MinimumSurfaceDistance("Minimum Surface Distance", Range (0.0, 1.0)) = 0.001
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define SURF_DIST 0.001

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 rayOrigin : TEXCOORD0;
                float3 hitPosition : TEXCOORD1;
            };

            uniform uint _VoxelGridSize;

            sampler3D _RaymarchTexture;
            int _MaximumRaySteps;
            int _MaximumRayDistance;
            float _MinimumSurfaceDistance;

            v2f vert (appdata vertexIn)
            {
                v2f v2fOut;
                v2fOut.vertex = UnityObjectToClipPos(vertexIn.vertex);
                v2fOut.rayOrigin = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                v2fOut.hitPosition = vertexIn.vertex;
                return v2fOut;
            }

            const float epsilon = 0.0005;

            fixed4 raymarch(float3 position, float3 normal)
            {
                float3 gridOrigin = floor(position * _VoxelGridSize);
                float3 maxGridOrigin = gridOrigin + 1.0;
                float maxGridOriginComp = max(maxGridOrigin.x, max(maxGridOrigin.y, maxGridOrigin.z));
                // align position to voxel grid
                position = gridOrigin / _VoxelGridSize;

                float maximumRayDistance = 1.0;
                float minimumStepDistance = 1.0 / _VoxelGridSize;
                float accumulatedDistance = 0.0;

                [loop]
                while (accumulatedDistance < maxGridOriginComp)
                {
                    float3 currentPosition = position + (accumulatedDistance * normal);
                    fixed4 currentColor = tex3D(_RaymarchTexture, currentPosition);

                    if (currentColor.a == 1.0)
                    {
                        return currentColor;
                    }
                    else
                    {
                        accumulatedDistance += currentColor.a;
                    }
                }

                return 0.0;
            }


            fixed4 frag (v2f i) : COLOR
            {
                float3 rayNormal = normalize(i.hitPosition - i.rayOrigin);
                fixed4 color = raymarch(i.rayOrigin, rayNormal);

                if (color.a == 0.0)
                {
                    discard;
                }

                return color;
            }

            ENDCG
        }
    }
}
