using UnityEngine;

public class EmptyTileGizmos : MonoBehaviour
{
    [SerializeField] private Color color = Color.yellow;
    [SerializeField] private Vector3 position;

    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(position, new Vector3(1, 1, 1));
    }
}
