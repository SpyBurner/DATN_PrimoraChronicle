using UnityEngine;

class Helper
{
    public static void PrintChildrenRecursively(Transform parent, int depth = 0)
    {
        string indent = new string(' ', depth * 2);
        foreach (Transform child in parent)
        {
            Debug.Log($"{indent}- {child.name}");
            PrintChildrenRecursively(child, depth + 1);
        }
    }
}