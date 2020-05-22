Shader "Unlit/Raymarch Shader"
{
    Properties
    {
        _RaymarchTexture ("Raymarch Texture", 3D) = "white" {}
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
                float4 clip : SV_POSITION;
                float3 rayOrigin : TEXCOORD0;
                float3 rayDirection : TEXCOORD1;
            };

            uniform uint _VoxelGridSize;
            uniform sampler3D _RaymarchTexture;

            v2f vert (appdata vert)
            {
                v2f frag;
                frag.clip = UnityObjectToClipPos(vert.vertex);
                frag.rayOrigin = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                frag.rayDirection = normalize(vert.vertex - frag.rayOrigin);
                return frag;
            }

            bool cubeRayIntersection(float3 rayOrigin, float3 rayDirection, float3 cubeMinimum, float3 cubeMaximum,
                out float nearIntersectionDistance, out float farIntersectionDistance)
            {
                // take inverse to avoid slow floating point division in pure ray-aabb function
                float3 inverseDirection = 1.0 / rayDirection;

                // calculate raw distance parameters, effectively distance along ray to intersect axes
                float3 distanceParameter1 = inverseDirection * (cubeMinimum - rayOrigin);
                float3 distanceParameter2 = inverseDirection * (cubeMaximum - rayOrigin);

                float3 minimumDistanceParameter = min(distanceParameter1, distanceParameter2);
                float3 maximumDistanceParameter = max(distanceParameter1, distanceParameter2);

                nearIntersectionDistance = max(minimumDistanceParameter.x, min(minimumDistanceParameter.y,
                    minimumDistanceParameter.z));
                farIntersectionDistance = min(maximumDistanceParameter.x, min(maximumDistanceParameter.y,
                    maximumDistanceParameter.z));

                return nearIntersectionDistance <= farIntersectionDistance;
            }

            fixed4 raymarch(float3 rayOrigin, float3 rayDirection)
            {
                float nearIntersectionDistance, farIntersectionDistance;
                cubeRayIntersection(rayOrigin, rayDirection, -0.5, 0.5, nearIntersectionDistance, farIntersectionDistance);

                // if near intersection is less than zero (we're inside cube), then raycast from zero
                nearIntersectionDistance *= (nearIntersectionDistance >= 0.0);

                float accumulatedDistance = nearIntersectionDistance;
                float maximumAccumulatedDistance = farIntersectionDistance;

                [loop]
                while (accumulatedDistance <= maximumAccumulatedDistance)
                {
                    float3 accumulatedRay = rayOrigin + (rayDirection * accumulatedDistance) + 0.5;
                    fixed4 color = tex3D(_RaymarchTexture, accumulatedRay);

                    if (color.a == 1.0)
                    {
                        return color;
                    }

                    accumulatedDistance += max(1.0 / _VoxelGridSize, color.a);
                }

                return 0.0;
            }


            fixed4 frag (v2f frag) : SV_TARGET
            {
                fixed4 color = raymarch(frag.rayOrigin, frag.rayDirection);

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
