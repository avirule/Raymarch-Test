Shader "Unlit/Raymarch Shader"
{
    CGINCLUDE

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

    float raymarchDepth(sampler3D raymarchTexture, float epsilon, float3 rayOrigin, float3 rayDirection)
    {
        float nearIntersectionDistance, farIntersectionDistance;
        cubeRayIntersection(rayOrigin, rayDirection, -0.5, 0.5, nearIntersectionDistance, farIntersectionDistance);

        // if near intersection is less than zero (we're inside cube), then raycast from zero distance
        nearIntersectionDistance *= (nearIntersectionDistance >= 0.0);

        float accumulatedDistance = nearIntersectionDistance;
        float maximumAccumulatedDistance = farIntersectionDistance;

        [loop]
        while (accumulatedDistance < maximumAccumulatedDistance)
        {
            float3 accumulatedRay = rayOrigin + (rayDirection * accumulatedDistance) + 0.5;
            fixed4 color = tex3D(raymarchTexture, accumulatedRay);

            if (color.a == 1.0)
            {
                return accumulatedDistance;
            }

            accumulatedDistance += max(epsilon, color.a);
        }

        return 0.0;
    }

    fixed4 raymarchColor(sampler3D raymarchTexture, float epsilon, float3 rayOrigin, float3 rayDirection)
    {
        float nearIntersectionDistance, farIntersectionDistance;
        cubeRayIntersection(rayOrigin, rayDirection, -0.5, 0.5, nearIntersectionDistance, farIntersectionDistance);

        // if near intersection is less than zero (we're inside cube), then raycast from zero distance
        nearIntersectionDistance *= (nearIntersectionDistance >= 0.0);

        float accumulatedDistance = nearIntersectionDistance;
        float maximumAccumulatedDistance = farIntersectionDistance;

        [loop]
        while (accumulatedDistance < maximumAccumulatedDistance)
        {
            float3 accumulatedRay = rayOrigin + (rayDirection * accumulatedDistance) + 0.5;
            fixed4 color = tex3D(raymarchTexture, accumulatedRay);

            if (color.a == 1.0)
            {
                return color;
            }

            accumulatedDistance += max(epsilon, color.a);
        }

        return 0.0;
    }

    ENDCG

    Properties
    {
        _RaymarchTexture ("Raymarch Texture", 3D) = "white" {}
        _Epsilon ("Epsilon", Range(0.00001, 0.025)) = 0.0005
        _AOIntensity ("AO Intensity", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Blend Off
        Cull Front
        LOD 100

        Pass
        {
            ZTest Always
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
                return raymarchDepth(_RaymarchTexture, _Epsilon, frag.rayOrigin, rayDirection);
            }

            ENDCG
        }
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
                float2 screen : TEXCOORD2;
            };

            uniform sampler2D _DepthTexture;
            uniform sampler3D _RaymarchTexture;
            uniform float _Epsilon;
            uniform int _AOIntensity;

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
                //float3 rayDirection = normalize(frag.rayDestination - frag.rayOrigin);
                //frag.rayOrigin += rayDirection * _ProjectionParams.y;

                //fixed4 color = raymarchColor(_RaymarchTexture, _Epsilon, frag.rayOrigin, rayDirection);

                //if (color.a < 1.0)
                //{
                //    discard;
                //}

                //return color;

                float depth = tex2D(_DepthTexture, frag.screen.xy);

                if (depth == 0.0)
                {
                    discard;
                }

                return depth;
            }

            ENDCG
        }
    }
}
