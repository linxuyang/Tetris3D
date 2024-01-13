using System;
using UnityEngine;

namespace Mc
{
    public class TestGesture : MonoBehaviour
    {
        public float rotateSpeed = 1.0f;
        public float zoomSpeed = 0.1f;
        private CameraFollow _follow;

        private void Start()
        {
            _follow = GetComponent<CameraFollow>();
            var ges = GetComponent<GestureUtil>();
            ges.SetHandlers(OnRotate, onZoom);
        }

        private void Update()
        {
            float delta = Input.GetAxis("Mouse ScrollWheel");
            var dis = _follow.distance - delta * zoomSpeed;
            _follow.distance = Mathf.Clamp(dis, 5, 30);
        }

        void OnRotate(Vector2 delta)
        {
            _follow.hAngle += delta.x * rotateSpeed;
            var vAngle = _follow.vAngle - delta.y * rotateSpeed;
            _follow.vAngle = Mathf.Clamp(vAngle, 0, 80);
        }

        void onZoom(float delta)
        {
            var dis = _follow.distance - delta * zoomSpeed;
            _follow.distance = Mathf.Clamp(dis, 2, 60);
        }
    }
}