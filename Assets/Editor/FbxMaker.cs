using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FbxMaker
{
    [MenuItem("MC/创建立方体网格")]
    public static void MakeMesh()
    {
        //顺序：下表面左下顺时针，上表面左下顺时针
        Mesh m = new Mesh();
        // m.vertices = new Vector3[8];
        m.vertices = new Vector3[8]
        {
            Vector3.zero,
            Vector3.forward,
            new Vector3(1, 0, 1),
            Vector3.right,
            Vector3.up,
            new Vector3(0, 1, 1),
            Vector3.one,
            new Vector3(1, 1, 0)
        };
        //需要顺时针
        m.triangles = new int[36]
        {
            0, 3, 1, 1, 3, 2, //下
            4, 5, 6, 4, 6, 7, //上
            0, 4, 7, 0, 7, 3, //前
            1, 6, 5, 1, 2, 6, //后
            1, 5, 4, 1, 4, 0, //左
            3, 7, 6, 3, 6, 2 //右
        };
        m.uv = new Vector2[8] {Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero};
        //a为下表面顶点，b为上表面顶点，左下、左上、右上、右下
        var a0 = new Vector3(-1, -1, -1);
        var a1 = new Vector3(-1, -1, 1);
        var a2 = new Vector3(1, -1, -1);
        var a3 = new Vector3(1, -1, -1);
        var b0 = new Vector3(-1, 1, -1);
        var b1 = new Vector3(-1, 1, 1);
        var b2 = new Vector3(1, 1, -1);
        var b3 = new Vector3(1, 1, -1);
        m.normals = new Vector3[8] {a0.normalized, a1.normalized, a2.normalized, a3.normalized, b0.normalized, b1.normalized, b2.normalized, b3.normalized};

        GameObject go = new GameObject("CubeMesh");
        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = m;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
    }

    [MenuItem("MC/创建立方体网格2")]
    public static void MakeMesh2()
    {
        //顺序：下表面左下顺时针，上表面左下顺时针
        float d = .1f;
        Mesh m = new Mesh();
        var a0 = Vector3.zero;
        var a1 = Vector3.forward;
        var a2 = new Vector3(1, 0, 1);
        var a3 = Vector3.right;
        var b0 = Vector3.up;
        var b1 = new Vector3(0, 1, 1);
        var b2 = Vector3.one;
        var b3 = new Vector3(1, 1, 0);
        var a00 = a0 + Vector3.right * d;
        var a01 = a0 + Vector3.up * d;
        var a02 = a0 + Vector3.forward * d;
        
        var a10 = a0 + Vector3.right * d;
        var a11 = a0 + Vector3.up * d;
        var a12 = a0 - Vector3.forward * d;
        
        var a20 = a0 - Vector3.right * d;
        var a21 = a0 + Vector3.up * d;
        var a22 = a0 - Vector3.forward * d;
        
        var a30 = a0 - Vector3.right * d;
        var a31 = a0 + Vector3.up * d;
        var a32 = a0 + Vector3.forward * d;
        
        var b00 = a0 + Vector3.right * d;
        var b01 = a0 - Vector3.up * d;
        var b02 = a0 + Vector3.forward * d;
        
        var b10 = a0 + Vector3.right * d;
        var b11 = a0 - Vector3.up * d;
        var b12 = a0 - Vector3.forward * d;
        
        var b20 = a0 - Vector3.right * d;
        var b21 = a0 - Vector3.up * d;
        var b22 = a0 - Vector3.forward * d;
        
        var b30 = a0 - Vector3.right * d;
        var b31 = a0 - Vector3.up * d;
        var b32 = a0 + Vector3.forward * d;
        m.vertices = new Vector3[24]
        {
            a00,
            a01,
            a02,
            a10,
            a11,
            a12,
            a20,
            a21,
            a22,
            a30,
            a31,
            a32,
            b00,
            b01,
            b02,
            b10,
            b11,
            b12,
            b20,
            b21,
            b22,
            b30,
            b31,
            b32
        };
        //需要顺时针
        m.triangles = new int[36]
        {
            0, 3, 1, 1, 3, 2, //下
            4, 5, 6, 4, 6, 7, //上
            0, 4, 7, 0, 7, 3, //前
            1, 6, 5, 1, 2, 6, //后
            1, 5, 4, 1, 4, 0, //左
            3, 7, 6, 3, 6, 2 //右
        };
        m.uv = new Vector2[8] {Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero};
        // var a0 = new Vector3(-1, -1, -1);
        // var a1 = new Vector3(-1, -1, 1);
        // var a2 = new Vector3(1, -1, -1);
        // var a3 = new Vector3(1, -1, -1);
        // var b0 = new Vector3(-1, 1, -1);
        // var b1 = new Vector3(-1, 1, 1);
        // var b2 = new Vector3(1, 1, -1);
        // var b3 = new Vector3(1, 1, -1);
        m.normals = new Vector3[8] {a0.normalized, a1.normalized, a2.normalized, a3.normalized, b0.normalized, b1.normalized, b2.normalized, b3.normalized};

        GameObject go = new GameObject("CubeMesh");
        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = m;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
    }
}