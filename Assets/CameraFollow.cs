using System;
using UnityEngine;

namespace Mc
{
    public class CameraFollow : MonoBehaviour
    {
        public float mapWidth = 61.44f; //10000地图的
        public float mapHeight = 40.96f;
        private float screenW = 0f;
        private float screenH = 0f;
        public float hAngle; //水平角度
        public float vAngle = 30; //跟随角度
        public float distance = 20; //距离

        public Transform target; //主角

        // public Transform map2d; //地图容器
        public float smooth = 0f;
        public Vector3 offset; //观看点偏移
        public Vector3 offsetPos; //最终位置偏移

        private Camera camera;
        private Vector3 forward;
        private Quaternion quaternion;
        private Vector3 pos; //摄像机目标位置
        private Vector3 look; //当前观看位置

        private void Start()
        {
            quaternion = new Quaternion();
            camera = GetComponentInChildren<Camera>();
            //screenW = Screen.width * 0.01f;
            // screenH = Screen.height * 0.01f;
            // screenW = UIRoot.Width * 0.01f;
            // screenH = UIRoot.Height * 0.01f;
            // camera.orthographicSize = screenH * 0.5f; //高度的一半
            // transform.localEulerAngles = new Vector3(vAngle, 0, 0);
        }

        private void Update()
        {
            if (target != null)
            {
                float ang = hAngle / 2 * Mathf.Deg2Rad;
                quaternion.Set(0, Mathf.Sin(ang), 0, Mathf.Cos(ang));
                forward = quaternion * Vector3.back;
                float d = distance * Mathf.Cos(vAngle * Mathf.Deg2Rad);
                float h = distance * Mathf.Sin(vAngle * Mathf.Deg2Rad);
                look = target.position + offset;

                pos = look + forward * d + Vector3.up * h;
                if (smooth != 0)
                {
                    transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * smooth);
                }
                else
                {
                    transform.position = pos;
                    transform.LookAt(look);
                }

                //位置偏移
                pos = transform.position;
                pos += transform.right * offsetPos.x;
                pos += transform.up * offsetPos.y;
                pos += transform.forward * offsetPos.z;
                transform.position = pos;
            }

            //更新地图朝向和位置
            // UpdateMap2d();
            //限制镜头
            // LimitCamera();
        }

        public void UpdateImmediately()
        {
            if (target != null)
            {
                float ang = hAngle / 2 * Mathf.Deg2Rad;
                quaternion.Set(0, Mathf.Sin(ang), 0, Mathf.Cos(ang));
                forward = quaternion * Vector3.back;
                float d = distance * Mathf.Cos(vAngle * Mathf.Deg2Rad);
                float h = distance * Mathf.Sin(vAngle * Mathf.Deg2Rad);
                look = target.position + offset;
                pos = look + forward * d + Vector3.up * h;
                transform.position = pos;
            }

            //更新地图朝向和位置
            // UpdateMap2d();
            //限制镜头
            LimitCamera();
        }

        //更新地图朝向和位置
        // private void UpdateMap2d() {
        //     var euler = transform.localEulerAngles;
        //     var angle = euler.x - 90;
        //     map2d.localEulerAngles = new Vector3(angle, 0, 0);
        //     var y = map2d.position.y;
        //     var z = -map2d.position.y / Mathf.Tan(euler.x * Mathf.Deg2Rad);
        //     map2d.position = new Vector3(0, y, z);
        // }

        private Vector2 min;
        private Vector2 max;

        private void LimitCamera()
        {
            // var euler = transform.localEulerAngles;
            // var p = transform.position;
            // float camZ = p.y / Mathf.Tan(euler.x * Mathf.Deg2Rad) - camera.orthographicSize / Mathf.Sin(euler.x * Mathf.Deg2Rad);
            // min.x = screenW * 0.5f; //camX最小值
            // //镜头z最小值，看到地图的【0，0】点
            // min.y = -camZ;
            // max.x = mapWidth - screenW * 0.5f; //最大x
            // max.y = min.y + (mapHeight - screenH) / Mathf.Sin(euler.x * Mathf.Deg2Rad); //最大z
            // float camX = Mathf.Clamp(p.x, min.x, max.x);
            // camZ = Mathf.Clamp(p.z, min.y, max.y);
            // if (mapWidth < screenW) {
            //     camX = min.x;
            // }
            //
            // if (mapHeight < screenH) {
            //     camZ = min.y;
            // }
            //
            // transform.position = new Vector3(camX, p.y, camZ);
        }
    }
}