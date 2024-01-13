using System.Collections;
using System.Collections.Generic;
using LinefyExamples;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TetrisGame : MonoBehaviour
{
    public float interval = 1.0f;
    private int row = 7;
    private int col = 7;
    private int h = 12;
    private int score;
    public Text textScore;
    public Text gameOver;
    public Text textData;
    public Transform container;
    public Transform spawnPoint;
    public Transform nextPoint;
    public GameObject[] prefabs;
    public Transform[] gosts;
    private GameObject currentTetromino;
    private GameObject nextTetromino;
    private Transform theGost;
    private int treIndex;
    private bool isGameOver = true;
    private bool[,,] all;
    private Transform[,,] allObj;
    private Transform camera;
    private Vector3[] dir4 = new Vector3[4] {Vector3.forward, Vector3.back, Vector3.left, Vector3.right};
    private Vector3[] axis4 = new Vector3[4] {Vector3.forward, Vector3.back, Vector3.left, Vector3.right};
    private float lastMoveTime;

    void Start()
    {
        camera = GameObject.Find("MainCamera").transform;
        isGameOver = true;
    }

    void Update()
    {
        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                isGameOver = false;
                gameOver.gameObject.SetActive(false);
                while (container.childCount > 0)
                {
                    var child = container.GetChild(0);
                    DestroyImmediate(child.gameObject);
                }

                all = new bool[h, col, row];
                allObj = new Transform[h, col, row];
                SpawnTetromino();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            isGameOver = true;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            var left = GetCameraDir(-camera.right);
            MoveTetromino(left);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            var right = GetCameraDir(camera.right);
            MoveTetromino(right);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            var back = GetCameraDir(-camera.forward);
            MoveTetromino(back);
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            var forward = GetCameraDir(camera.forward);
            MoveTetromino(forward);
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            // RotateTetromino(Vector3.forward);
            var forward = GetCameraDir(-camera.forward);
            var axis = currentTetromino.transform.InverseTransformDirection(forward);
            RotateTetromino(axis);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            // RotateTetromino(Vector3.up);
            var axis = currentTetromino.transform.InverseTransformDirection(Vector3.up);
            RotateTetromino(axis);
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            // RotateTetromino(Vector3.right);
            var right = GetCameraDir(camera.right);
            var axis = currentTetromino.transform.InverseTransformDirection(right);
            RotateTetromino(axis);
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            HardDrop();
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            DestryLayer(0);
        }

        AutoMove();
    }

    private void AutoMove()
    {
        var now = Time.realtimeSinceStartup;
        if (now - lastMoveTime >= interval)
        {
            lastMoveTime = now;
            MoveTetromino(Vector3.down);
        }
    }

    private Vector3 GetCameraDir(Vector3 camDir)
    {
        float minAngle = 1000;
        int minIndex = 0;
        for (int i = 0; i < dir4.Length; i++)
        {
            float ang = Vector3.Angle(camDir, dir4[i]);
            if (ang < minAngle)
            {
                minAngle = ang;
                minIndex = i;
            }
        }

        return dir4[minIndex];
    }

    void SpawnTetromino()
    {
        if (theGost)
        {
            theGost.position = Vector3.up * 1000;
        }

        if (nextTetromino == null)
        {
            int randomIndex = Random.Range(0, prefabs.Length);
            // randomIndex = 2;
            currentTetromino = Instantiate(prefabs[randomIndex], container, false);
            currentTetromino.transform.position = spawnPoint.position;
            currentTetromino.transform.rotation = Quaternion.identity;
            theGost = gosts[randomIndex];
        }
        else
        {
            currentTetromino = nextTetromino;
            currentTetromino.transform.SetParent(container);
            nextTetromino.transform.position = spawnPoint.position;
            nextTetromino.transform.rotation = Quaternion.identity;
            theGost = gosts[treIndex];
        }

        int randomIndex2 = Random.Range(0, prefabs.Length);
        // randomIndex2 = 2;
        nextTetromino = Instantiate(prefabs[randomIndex2], nextPoint, false);
        nextTetromino.transform.localPosition = Vector3.zero;
        nextTetromino.transform.localRotation = Quaternion.identity;
        treIndex = randomIndex2;
    }

    void MoveTetromino(Vector3 direction)
    {
        Vector3 oldPos = currentTetromino.transform.position;
        currentTetromino.transform.position = oldPos + direction;

        if (IsValidPosition(currentTetromino.transform) == false)
        {
            currentTetromino.transform.position = oldPos;
            if (direction.y < 0)
            {
                isGameOver = UpdateData();
                if (isGameOver == false)
                {
                    SpawnTetromino();
                    UpdateScore(4);
                }
                else
                {
                    gameOver.text = "Game Over";
                    gameOver.gameObject.SetActive(true);
                }
            }
        }

        SetGostPos();
    }

    private void SetGostPos()
    {
        theGost.rotation = currentTetromino.transform.rotation;
        theGost.position = currentTetromino.transform.position;
        while (true)
        {
            Vector3 oldPos = theGost.position;
            theGost.position = oldPos + Vector3.down;

            if (IsValidPosition(theGost) == false)
            {
                theGost.position = oldPos;
                break;
            }
        }
    }

    void HardDrop()
    {
        while (true)
        {
            Vector3 oldPos = currentTetromino.transform.position;
            currentTetromino.transform.position = oldPos + Vector3.down;

            if (IsValidPosition(currentTetromino.transform) == false)
            {
                currentTetromino.transform.position = oldPos;
                break;
            }
        }

        isGameOver = UpdateData();
        if (isGameOver == false)
        {
            SpawnTetromino();
            UpdateScore(4);
        }
        else
        {
            gameOver.text = "Game Over";
            gameOver.gameObject.SetActive(true);
        }
    }

    void RotateTetromino(Vector3 axis)
    {
        var oldRotate = currentTetromino.transform.rotation;
        currentTetromino.transform.Rotate(axis, 90f);

        if (IsValidPosition(currentTetromino.transform) == false)
        {
            currentTetromino.transform.rotation = oldRotate;
        }

        SetGostPos();
    }

    bool IsValidPosition(Transform t)
    {
        foreach (Transform child in t)
        {
            Vector3 childPosition = child.position;
            childPosition.x = Mathf.RoundToInt(childPosition.x);
            childPosition.z = Mathf.RoundToInt(childPosition.z);
            childPosition.y = Mathf.RoundToInt(childPosition.y);
            if (!IsWithinBorders(childPosition))
            {
                return false;
            }

            if (IsExsitTetri(childPosition))
            {
                return false;
            }
        }

        return true;
    }

    bool IsWithinBorders(Vector3 position)
    {
        return position.x >= 0 && position.x < col && position.z >= 0 && position.z < row && position.y >= 0;
    }

    //每个月小立方体的位置
    bool IsExsitTetri(Vector3 p)
    {
        int x = Mathf.RoundToInt(p.x);
        int y = Mathf.RoundToInt(p.y);
        int z = Mathf.RoundToInt(p.z);
        var v = all.GetLength(0);
        if (y < v)
        {
            if (all[y, x, z])
            {
                return true;
            }
        }

        return false;
    }

    private bool UpdateData()
    {
        var v = all.GetLength(0);
        foreach (Transform child in currentTetromino.transform)
        {
            Vector3 p = child.position;
            int x = Mathf.RoundToInt(p.x);
            int y = Mathf.RoundToInt(p.y);
            int z = Mathf.RoundToInt(p.z);
            if (y >= 0 && y < v)
            {
                all[y, x, z] = true;
                allObj[y, x, z] = child;
            }

            if (y >= 13)
            {
                return true;
            }
        }

        ShowData();
        while (true)
        {
            var fullLayer = CheckKill();
            if (fullLayer == -1)
            {
                break;
            }

            DestryLayer(fullLayer);
        }

        return false;
    }

    private int CheckKill()
    {
        var v = all.GetLength(0);
        for (int i = 0; i < v; i++)
        {
            var layer = i;
            var isFull = CheckKillByLayer(layer);
            if (isFull)
            {
                return layer;
            }
        }

        return -1;
    }

    private bool CheckKillByLayer(int layer)
    {
        for (int j = 0; j < col; j++)
        {
            for (int k = 0; k < row; k++)
            {
                if (all[layer, j, k] == false)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void DestryLayer(int layer)
    {
        for (int j = 0; j < col; j++)
        {
            for (int k = 0; k < row; k++)
            {
                var obj = allObj[layer, j, k];
                if (obj)
                {
                    Destroy(obj.gameObject);
                }

                all[layer, j, k] = false;
            }
        }

        var v = all.GetLength(0);
        for (int i = layer; i < v - 1; i++)
        {
            for (int j = 0; j < col; j++)
            {
                for (int k = 0; k < row; k++)
                {
                    var obj = allObj[i + 1, j, k];
                    if (obj)
                    {
                        obj.position += Vector3.down;
                    }

                    var data = all[i + 1, j, k];
                    allObj[i, j, k] = obj;
                    all[i, j, k] = data;
                    allObj[i + 1, j, k] = null;
                    all[i + 1, j, k] = false;
                }
            }
        }


        UpdateScore(79);
    }

    private void UpdateScore(int addScore)
    {
        this.score += addScore;
        textScore.text = "Score:  " + this.score;
    }

    private void ShowData()
    {
        var str = "";
        for (int i = 0; i < col; i++)
        {
            for (int j = 0; j < row; j++)
            {
                var b = all[0, i, j];
                str += b ? "1" : "0";
            }

            str += "\n";
        }

        textData.text = str;
    }
}