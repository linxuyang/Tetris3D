using System.Collections;
using System.Collections.Generic;
using Linefy;
using Linefy.Serialization;
using UnityEngine;

public class PixelLine : DrawableComponent
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
            allIndex[0].Set(0, 1);
        }
    }
}