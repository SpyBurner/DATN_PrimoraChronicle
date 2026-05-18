using UnityEngine;

public class HexTile : MonoBehaviour
{
    [Header("Axial Coordinates")]
    public int p;
    public int q;

    public void SetCoordinates(int pCoord, int qCoord)
    {
        p = pCoord;
        q = qCoord;
        gameObject.name = $"HexTile_P{p}_Q{q}";
    }
}
