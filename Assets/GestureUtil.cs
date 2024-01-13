using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GestureUtil : MonoBehaviour
{
    private Dictionary<int, Vector2> posMap;
    private List<Touch> allTouch;
    private Action<float> _onZoom;
    private Action<Vector2> _onRotate;
    private bool touchSupported;

    private void OnEnable()
    {
        touchSupported = Input.touchSupported;
        posMap = new Dictionary<int, Vector2>();
        allTouch = new List<Touch>();
        Debug.Log("touchSupported:" + touchSupported);
    }

    public void SetHandlers(Action<Vector2> onRotate, Action<float> onZoom)
    {
        _onZoom = onZoom;
        _onRotate = onRotate;
    }

    void Update()
    {
        if (touchSupported)
        {
            UpdateByTouch();
        }
        else
        {
            UpdateByMouse();
        }
    }

    bool CheckOnUI(Vector2 position)
    {
        PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = position;

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, raycastResults);

        if (raycastResults.Count > 0)
        {
            foreach (var go in raycastResults)
            {
                if (go.gameObject.layer == 5)
                {
                    return true;
                }
            }
        }

        return false;
    }


    void UpdateByTouch()
    {
        int count = Input.touchCount;
        if (count < 1 || count > 2)
        {
            posMap.Clear();
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var t = Input.GetTouch(i);
            switch (t.phase)
            {
                case TouchPhase.Began:
                    // Debug.Log("TouchBegan");
                    var isUI = CheckOnUI(t.position);
                    if (isUI)
                    {
                        continue;
                    }

                    // Debug.Log("isUI：" + isUI);
                    if (posMap.ContainsKey(t.fingerId))
                    {
                        posMap.Remove(t.fingerId);
                    }

                    posMap.Add(t.fingerId, t.position);
                    break;
                case TouchPhase.Moved:
                    if (posMap.ContainsKey(t.fingerId) == false)
                    {
                        continue;
                    }

                    if (posMap.Count == 1)
                    {
                        // Debug.Log("Rotate");
                        _onRotate?.Invoke(t.deltaPosition);
                    }
                    else if (posMap.Count == 2)
                    {
                        allTouch.Clear();
                        for (int j = 0; j < count; j++)
                        {
                            var touch = Input.GetTouch(j);
                            if (posMap.ContainsKey(touch.fingerId))
                            {
                                allTouch.Add(touch);
                            }
                        }

                        if (allTouch.Count < 2)
                        {
                            return;
                        }

                        // Debug.Log("Zoom");
                        var t1 = allTouch[0];
                        var t2 = allTouch[1];
                        var prePos1 = posMap[t1.fingerId];
                        var prePos2 = posMap[t2.fingerId];
                        var d1 = Vector2.Distance(prePos1, prePos2);
                        var d2 = Vector2.Distance(t1.position, t2.position);
                        if (Mathf.Abs(d2 - d1) > 0)
                        {
                            _onZoom?.Invoke(d2 - d1);
                        }
                    }
                    posMap[t.fingerId] = t.position; 
                    break;

                case TouchPhase.Ended:
                    if (posMap.ContainsKey(t.fingerId))
                    {
                        posMap.Remove(t.fingerId);
                    }

                    break;
                case TouchPhase.Canceled:
                    if (posMap.ContainsKey(t.fingerId))
                    {
                        posMap.Remove(t.fingerId);
                    }

                    break;
                default:
                    break;
            }
        }
    }

    void UpdateByMouse()
    {
        int ctrlId = 0;
        int mouseId = 1;
        bool ctrl = Input.GetKey(KeyCode.LeftControl);
        bool ctrlDown = Input.GetKeyDown(KeyCode.LeftControl);
        bool mouse = Input.GetMouseButton(0);
        bool mouseDown = Input.GetMouseButtonDown(0);

        if (ctrl == false && mouse == false)
        {
            posMap.Clear();
            return;
        }

        //按下ctrl时记录鼠标位置，当作第一个手指
        if (ctrlDown)
        {
            if (posMap.ContainsKey(ctrlId))
            {
                posMap.Remove(ctrlId);
            }

            posMap.Add(ctrlId, Input.mousePosition);
        }

        //按下鼠标时，记录位置并return，不在此帧做移动判断
        if (mouseDown)
        {
            // Debug.Log("mouseDown");
            // var isUI = CheckOnUI(Input.mousePosition);
            // if (isUI == false)
            // {
                if (posMap.ContainsKey(mouseId))
                {
                    posMap.Remove(mouseId);
                }
            
                posMap.Add(mouseId, Input.mousePosition);
            // }

            return;
        }

        //鼠标没按就不处理
        if (mouse == false || posMap.ContainsKey(mouseId) == false)
        {
            return;
        }

        //处理已经按下的mouseId
        var curPos = (Vector2) Input.mousePosition;
        if (posMap.ContainsKey(mouseId))
        {
            if (ctrl == false)
            {
                var prePos = posMap[mouseId];
                if (curPos.Equals(prePos) == false)
                {
                    var deltaPos = curPos - prePos;
                    _onRotate?.Invoke(deltaPos);
                }
            }
            else
            {
                var prePos1 = posMap[ctrlId];
                var prePos2 = posMap[mouseId];
                var d1 = Vector2.Distance(prePos1, prePos2);
                var d2 = Vector2.Distance(prePos1, curPos);
                if (Mathf.Abs(d2 - d1) > 0)
                {
                    _onZoom?.Invoke(d2 - d1);
                }
            }

            posMap[mouseId] = curPos;
        }
    }
}