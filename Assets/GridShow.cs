using Linefy.Primitives;
using UnityEngine;

public class GridShow : MonoBehaviour
{
    public enum ForwardType
    {
        FORWARD,
        BACK,
        LEFT,
        RIGHT,
    }

    public Camera mainCamera;
    public Vector3 forward;
    public ForwardType forwardType = ForwardType.FORWARD;
    private LinefyGrid2d grid;

    void Start()
    {
        grid = GetComponent<LinefyGrid2d>();
    }

    void Update()
    {
        if (grid)
        {
            var forward = transform.forward;
            if (forwardType == ForwardType.FORWARD)
            {
            }
            else if (forwardType == ForwardType.BACK)
            {
                forward = -transform.forward;
            }
            else if (forwardType == ForwardType.LEFT)
            {
                forward = -transform.right;
            }
            else if (forwardType == ForwardType.RIGHT)
            {
                forward = transform.right;
            }

            grid.enabled = Vector3.Dot(mainCamera.transform.forward, forward) <= 0;
        }
    }
}