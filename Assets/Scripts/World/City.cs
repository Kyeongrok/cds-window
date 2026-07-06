using UnityEngine;

// One city record, matching cds-helper's cities.json field names so
// JsonUtility can map them directly.
[System.Serializable]
public class City
{
    public int id;
    public string name;
    public float latitude;
    public float longitude;
    public bool hasLibrary;
    public float pixelX;      // original game world-map pixel coords
    public float pixelY;
    public bool hasShipyard;
    public bool hasGuild;
    public string culturalSphere;
}

[System.Serializable]
class CityList
{
    public City[] cities;
}

public static class CityLoader
{
    // Loads Assets/Resources/cities.json (a TextAsset) into City[].
    public static City[] LoadFromResources(string resourceName = "cities")
    {
        var asset = Resources.Load<TextAsset>(resourceName);
        if (asset == null)
        {
            Debug.LogError("CityLoader: Resources/" + resourceName + ".json not found.");
            return new City[0];
        }
        // JsonUtility can't parse a top-level array, so wrap it in an object.
        string wrapped = "{\"cities\":" + asset.text + "}";
        var list = JsonUtility.FromJson<CityList>(wrapped);
        return list != null && list.cities != null ? list.cities : new City[0];
    }
}
