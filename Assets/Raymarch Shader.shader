Shader "Unlit/Raymarch Shader"
{
    Properties
    {
        _RaymarchTexture ("Raymarch Texture", 3D) = "white" {}
        _Epsilon ("_Epsilon", Range(0.00001, 0.025)) = 0.0005
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        LOD 100

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
                float4 clip : SV_POSITION;
                float3 rayOrigin : TEXCOORD0;
                float3 rayDestination : TEXCOORD1;
            };

            uniform sampler3D _RaymarchTexture;
            uniform float _Epsilon;
            uniform int _VoxelGridSize;

            v2f vert (appdata vert)
            {
                v2f frag;
                frag.clip = UnityObjectToClipPos(vert.vertex);
                frag.rayOrigin = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                frag.rayDestination = vert.vertex;
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

                nearIntersectionDistance = max(minimumDistanceParameter.x, max(minimumDistanceParameter.y, minimumDistanceParameter.z));
                farIntersectionDistance = min(maximumDistanceParameter.x, min(maximumDistanceParameter.y, maximumDistanceParameter.z));

                return nearIntersectionDistance <= farIntersectionDistance;
            }

            fixed4 raymarch(float3 rayOrigin, float3 rayDirection)
            {
                float nearIntersectionDistance, farIntersectionDistance;
                cubeRayIntersection(rayOrigin, rayDirection, -0.5, 0.5, nearIntersectionDistance, farIntersectionDistance);

                // if near intersection is less than zero (we're inside cube), then raycast from zero
                //nearIntersectionDistance *= (nearIntersectionDistance >= 0.0);

                float accumulatedDistance = nearIntersectionDistance;
                float maximumAccumulatedDistance = farIntersectionDistance;

                // float3 accumulatedRay = rayOrigin + (rayDirection * accumulatedDistance) + 0.5;
                // return tex3D(_RaymarchTexture, accumulatedRay);

                [loop]
                while (accumulatedDistance < maximumAccumulatedDistance)
                {
                    float3 accumulatedRay = rayOrigin + (rayDirection * accumulatedDistance) + 0.5;
                    fixed4 color = tex3D(_RaymarchTexture, accumulatedRay);

                    if (color.a == 1.0)
                    {
                        return color;
                    }

                    accumulatedDistance += max(_Epsilon, color.a);
                }

                return 0.0;
            }


            fixed4 frag (v2f frag) : SV_TARGET
            {
                float3 rayDirection = normalize(frag.rayDestination - frag.rayOrigin);
                frag.rayOrigin += rayDirection * _ProjectionParams.y;
                fixed4 color = raymarch(frag.rayOrigin, rayDirection);

                if (color.a < 1.0)
                {
                    discard;
                }

                return color;
            }

            ENDCG
        }
    }
}
