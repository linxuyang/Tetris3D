using System;
using System.Collections;
using System.Collections.Generic;
using Linefy;
using Linefy.Serialization;
using UnityEngine;

public class OneLine : Drawable
{
    private Vector3[] allPos;
    private Vector2Int[] lines;
    bool dTopology = true;
    bool dSize = true;

    Lines wireframe;
    public SerializationData_LinesBase wireframeProperties = new SerializationData_LinesBase();

    private int lineCount = 1;

    public OneLine(SerializationData_LinesBase wireframeProperties)
    {
        this.wireframeProperties = wireframeProperties;
    }

    void PreDraw()
    {
        if (wireframe == null)
        {
            wireframe = new Lines(1);
            wireframe.capacityChangeStep = 8;
        }

        if (dTopology)
        {
            int linesCount = lines.Length;
            wireframe.count = linesCount;

            for (int i = 0; i < wireframe.count; i++)
            {
                wireframe.SetTextureOffset(i, 0, 0);
            }

            dTopology = false;
            dSize = true;
        }

        wireframe.autoTextureOffset = true;

        if (dSize)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                Vector2Int line = lines[i];
                wireframe.SetPosition(i, allPos[line.x], allPos[line.y]);
            }

            Vector3 c0 = Vector3.zero;
            Vector3 c1 = new Vector3(3, 0, 0);

            dSize = false;
        }

        wireframe.LoadSerializationData(wireframeProperties);
    }

    public override void DrawNow(Matrix4x4 matrix)
    {
        PreDraw();
        wireframe.DrawNow(matrix);
    }

    public override void Draw(Matrix4x4 tm, Camera cam, int layer)
    {
        PreDraw();
        wireframe.Draw(tm, cam, layer);
    }

    public override void Dispose()
    {
        if (wireframe != null)
        {
            wireframe.Dispose();
        }
    }

    public void SetLines(Vector3[] poss, Vector2Int[] indexs)
    {
        if (allPos != null && poss.Length == allPos.Length && lines != null && lines.Length == indexs.Length)
        {
            return;
        }

        allPos = poss;
        lines = indexs;
        dTopology = true;
        dSize = true;
    }

    public override void GetStatistic(ref int linesCount, ref int totallinesCount, ref int dotsCount, ref int totalDotsCount, ref int polylinesCount, ref int totalPolylineVerticesCount)
    {
        if (wireframe != null)
        {
            linesCount += 1;
            totallinesCount += wireframe.count;
        }
    }
}

// [ExecuteInEditMode]
public class MyTestLine : DrawableComponent
{
    public Vector3[] allPos;
    public Vector2Int[] allIndex;
    public SerializationData_LinesBase wireframeProperties = new SerializationData_LinesBase(3, Color.white, 1);
    private OneLine _line;

    private OneLine line
    {
        get
        {
            if (_line == null)
            {
                _line = new OneLine(wireframeProperties);
            }

            return _line;
        }
    }

    public override Drawable drawable
    {
        get { return line; }
    }

    protected override void PreDraw()
    {
        line.SetLines(allPos, allIndex);
        line.wireframeProperties = wireframeProperties;
    }

    // public static MyTestLine CreateInstance() {
    //     GameObject go = new GameObject("New TestLine");
    //     MyTestLine result = go.AddComponent<MyTestLine>();
    //     return result;
    // }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.K))
        {
            allPos = new Vector3[2];
            allPos[0] = Vector3.zero;
            allPos[1] = Vector3.right;
            allIndex = new Vector2Int[1];
            allIndex[0].Set(0,1);
        }
    }
}