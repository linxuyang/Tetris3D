using UnityEngine;

namespace Mc
{
    public class DrawAxes : MonoBehaviour
    {
        public float axisLength = 1f;
        public float lineWidth = 0.02f;
        public Color xColor = Color.red;
        public Color yColor = Color.green;
        public Color zColor = Color.blue;

        private void OnDrawGizmos()
        {
            Gizmos.color = xColor;
            Gizmos.DrawRay(transform.position, transform.right * axisLength);

            Gizmos.color = yColor;
            Gizmos.DrawRay(transform.position, transform.up * axisLength);

            Gizmos.color = zColor;
            Gizmos.DrawRay(transform.position, transform.forward * axisLength);

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, lineWidth);
        }
    }
}