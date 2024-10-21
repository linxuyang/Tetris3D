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

    public void SetupPoints(Vector3[] points,Vector2Int[] indexs)
    {
        allPos = points;
        allIndex = indexs;
    }
    
    public void SetupPoints(Vector3[] points)
    {
        allPos = points;
        allIndex = new Vector2Int[points.Length - 1];
        for (int i = 0; i < allIndex.Length; i++)
        {
            allIndex[i].Set(i, i + 1);
        }
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.K))
        {
            SetupPoints(new Vector3[2] { Vector3.zero, Vector3.right });
        }
    }
}