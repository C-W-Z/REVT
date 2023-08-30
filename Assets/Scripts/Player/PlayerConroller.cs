using UnityEngine;

public class PlayerConroller : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform tf;
    [SerializeField] private Rigidbody2D rb;

#region Detects

    [System.Serializable]
    private struct Detect
    {
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private CheckBox upCheck, downCheck, leftCheck, rightCheck;
        public bool up, down, left, right;

        public void DetectAll()
        {
            up    = upCheck   .Detect(groundLayer);
            down  = downCheck .Detect(groundLayer);
            left  = leftCheck .Detect(groundLayer);
            right = rightCheck.Detect(groundLayer);
        }
    }

    [Header("Detects")]
    [SerializeField] private Detect detect;

#endregion
}