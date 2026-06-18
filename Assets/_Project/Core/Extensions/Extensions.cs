using UnityEngine;

public static class TransformExtensions
{
    public static Transform FindDeepChild(this Transform parent, string id)
    {
        var result = parent.Find(id);

        if (result != null) 
            return result;

        foreach (Transform child in parent)
        {
            result = child.FindDeepChild(id);

            if (result != null) 
                return result;
        }

        return null;
    }
}
