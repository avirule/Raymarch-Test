int project1D(int3 coords, int edgeLength)
{
    return coords.x + (edgeLength * (coords.y + (edgeLength * coords.z)));
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

float raymarch(StructuredBuffer<float> accelerationData, int edgeLength, float epsilon, float3 rayOrigin, float3 rayDirection, out float depth)
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
        int3 hitPosition = int3((rayOrigin + (rayDirection * accumulatedDistance) + 0.5) * edgeLength);
        float jump = accelerationData[project1D(hitPosition, edgeLength)];

        if (jump >= 1.0)
        {
            depth = accumulatedDistance;
            return jump;
        }

        accumulatedDistance += max(epsilon, jump);
    }

    return 0.0;
}
