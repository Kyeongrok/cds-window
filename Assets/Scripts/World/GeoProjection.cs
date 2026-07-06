using UnityEngine;

// Shared lat/lon -> world projection so cities and the land mesh line up.
// Simple equirectangular: X = east(lon), Z = north(lat). Origin = Lisbon,
// so the boat starts near (0,0).
public static class GeoProjection
{
    public const float OriginLat = 38f;   // Lisbon
    public const float OriginLon = -9f;
    public const float UnitsPerDegree = 8f;

    public static Vector3 LatLonToWorld(float lat, float lon)
    {
        return new Vector3((lon - OriginLon) * UnitsPerDegree, 0f, (lat - OriginLat) * UnitsPerDegree);
    }

    // UV into an equirectangular world texture (lon -180..180, lat -90..90).
    public static Vector2 UV(float lat, float lon)
    {
        return new Vector2((lon + 180f) / 360f, (lat + 90f) / 180f);
    }

    // Inverse: world position -> (lat, lon) stored as (x=lat, y=lon).
    public static Vector2 WorldToLatLon(Vector3 world)
    {
        return new Vector2(world.z / UnitsPerDegree + OriginLat, world.x / UnitsPerDegree + OriginLon);
    }
}
