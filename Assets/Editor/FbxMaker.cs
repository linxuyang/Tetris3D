using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FbxMaker
{
    [System.Serializable]
    class PolygonInput
    {
        public List<int> corners = new List<int>();
    }

    /// <summary>
    /// 与 Linefy PolygonalCube 相同的 8 个角点（[-halfExtent,halfExtent]³）。
    /// </summary>
    static void FillPolygonalCubeCorners(float halfExtent, Vector3[] dst)
    {
        float h = halfExtent;
        dst[0] = new Vector3(-h, -h, -h);
        dst[1] = new Vector3(h, -h, -h);
        dst[2] = new Vector3(h, h, -h);
        dst[3] = new Vector3(-h, h, -h);
        dst[4] = new Vector3(-h, -h, h);
        dst[5] = new Vector3(h, -h, h);
        dst[6] = new Vector3(h, h, h);
        dst[7] = new Vector3(-h, h, h);
    }

    /// <summary>
    /// PolygonalCube 中六个四边形面的顶点索引。
    /// </summary>
    static readonly int[,] PolygonalCubeFaceCorners =
    {
        { 0, 1, 2, 3 },
        { 7, 6, 5, 4 },
        { 4, 5, 1, 0 },
        { 5, 6, 2, 1 },
        { 7, 3, 2, 6 },
        { 4, 0, 3, 7 },
    };

    /// <summary>
    /// 仅生成立方体线框网格（不含六个实心面）：由六个四边形推出 12 条唯一边。
    /// 顶点布局与 Linefy Lines shader 匹配：POSITION 为当前端点，NORMAL 为线段另一端点。
    /// </summary>
    /// <param name="halfExtent">半边长，默认 0.5 对应 PolygonalCube 尺度。</param>
    /// <param name="lineWidth">Linefy 线宽属性，最终宽度还会乘材质的 _WidthMultiplier。</param>
    public static Mesh CreateCubeWireframeMesh(float halfExtent = 0.5f, float lineWidth = 1f)
    {
        var corners = new Vector3[8];
        FillPolygonalCubeCorners(halfExtent, corners);

        var edgeKeys = new HashSet<(int a, int b)>();
        for (int f = 0; f < 6; f++)
        {
            for (int c = 0; c < 4; c++)
            {
                int ca = PolygonalCubeFaceCorners[f, c];
                int cb = PolygonalCubeFaceCorners[f, (c + 1) % 4];
                if (ca > cb)
                {
                    int t = ca;
                    ca = cb;
                    cb = t;
                }

                edgeKeys.Add((ca, cb));
            }
        }

        var sortedEdges = new List<(int a, int b)>(edgeKeys);
        sortedEdges.Sort((x, y) =>
        {
            int cmp = x.a.CompareTo(y.a);
            return cmp != 0 ? cmp : x.b.CompareTo(y.b);
        });

        var vertices = new List<Vector3>(48);
        var oppositeEndpoints = new List<Vector3>(48);
        var colors = new List<Color>(48);
        var uvs = new List<Vector2>(48);
        var uv2s = new List<Vector2>(48);
        var triangles = new List<int>(72);

        foreach (var e in sortedEdges)
        {
            Vector3 p0 = corners[e.a];
            Vector3 p1 = corners[e.b];
            if ((p1 - p0).sqrMagnitude < 1e-8f)
                continue;

            int wi = vertices.Count;
            vertices.Add(p0);
            vertices.Add(p0);
            vertices.Add(p1);
            vertices.Add(p1);

            // Linefy Lines shader 将 NORMAL 当作线段另一端点使用，而不是表面法线。
            oppositeEndpoints.Add(p1);
            oppositeEndpoints.Add(p1);
            oppositeEndpoints.Add(p0);
            oppositeEndpoints.Add(p0);

            colors.Add(Color.white);
            colors.Add(Color.white);
            colors.Add(Color.white);
            colors.Add(Color.white);

            uvs.Add(new Vector2(0f, lineWidth));
            uvs.Add(new Vector2(0f, lineWidth));
            uvs.Add(new Vector2(1f, lineWidth));
            uvs.Add(new Vector2(1f, lineWidth));

            uv2s.Add(new Vector2(0f, 0f));
            uv2s.Add(new Vector2(1f, 0f));
            uv2s.Add(new Vector2(2f, 0f));
            uv2s.Add(new Vector2(3f, 0f));

            triangles.Add(wi);
            triangles.Add(wi + 1);
            triangles.Add(wi + 3);
            triangles.Add(wi + 3);
            triangles.Add(wi + 1);
            triangles.Add(wi + 2);
        }

        var mesh = new Mesh { name = "CubeWireframe" };
        mesh.indexFormat =
            vertices.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;

        mesh.SetVertices(vertices);
        mesh.SetNormals(oppositeEndpoints);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, uv2s);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// 根据 positions 和 polygons 的 position index 生成 Linefy Lines shader 兼容的线框网格。
    /// 每个 polygon 的相邻 corner 会生成一条无向边，重复边会自动去重。
    /// </summary>
    static Mesh CreatePolygonWireframeMesh(Vector3[] positions, List<PolygonInput> polygons, float lineWidth = 1f, string meshName = "PolygonWireframe")
    {
        var edgeKeys = new HashSet<(int a, int b)>();
        if (positions == null || polygons == null)
            return CreateLinefyWireframeMesh(positions, edgeKeys, lineWidth, meshName);

        for (int p = 0; p < polygons.Count; p++)
        {
            List<int> corners = polygons[p].corners;
            if (corners == null || corners.Count < 2)
                continue;

            for (int c = 0; c < corners.Count; c++)
            {
                int ca = corners[c];
                int cb = corners[(c + 1) % corners.Count];
                if (ca < 0 || ca >= positions.Length || cb < 0 || cb >= positions.Length || ca == cb)
                    continue;

                if (ca > cb)
                {
                    int t = ca;
                    ca = cb;
                    cb = t;
                }

                edgeKeys.Add((ca, cb));
            }
        }

        return CreateLinefyWireframeMesh(positions, edgeKeys, lineWidth, meshName);
    }

    static Mesh CreateLinefyWireframeMesh(Vector3[] positions, HashSet<(int a, int b)> edgeKeys, float lineWidth, string meshName)
    {
        var sortedEdges = new List<(int a, int b)>(edgeKeys);
        sortedEdges.Sort((x, y) =>
        {
            int cmp = x.a.CompareTo(y.a);
            return cmp != 0 ? cmp : x.b.CompareTo(y.b);
        });

        var vertices = new List<Vector3>(sortedEdges.Count * 4);
        var oppositeEndpoints = new List<Vector3>(sortedEdges.Count * 4);
        var colors = new List<Color>(sortedEdges.Count * 4);
        var uvs = new List<Vector2>(sortedEdges.Count * 4);
        var uv2s = new List<Vector2>(sortedEdges.Count * 4);
        var triangles = new List<int>(sortedEdges.Count * 6);

        if (positions == null)
            positions = new Vector3[0];

        foreach (var e in sortedEdges)
        {
            Vector3 p0 = positions[e.a];
            Vector3 p1 = positions[e.b];
            if ((p1 - p0).sqrMagnitude < 1e-8f)
                continue;

            int wi = vertices.Count;
            vertices.Add(p0);
            vertices.Add(p0);
            vertices.Add(p1);
            vertices.Add(p1);

            // Linefy Lines shader 将 NORMAL 当作线段另一端点使用，而不是表面法线。
            oppositeEndpoints.Add(p1);
            oppositeEndpoints.Add(p1);
            oppositeEndpoints.Add(p0);
            oppositeEndpoints.Add(p0);

            colors.Add(Color.white);
            colors.Add(Color.white);
            colors.Add(Color.white);
            colors.Add(Color.white);

            uvs.Add(new Vector2(0f, lineWidth));
            uvs.Add(new Vector2(0f, lineWidth));
            uvs.Add(new Vector2(1f, lineWidth));
            uvs.Add(new Vector2(1f, lineWidth));

            uv2s.Add(new Vector2(0f, 0f));
            uv2s.Add(new Vector2(1f, 0f));
            uv2s.Add(new Vector2(2f, 0f));
            uv2s.Add(new Vector2(3f, 0f));

            triangles.Add(wi);
            triangles.Add(wi + 1);
            triangles.Add(wi + 3);
            triangles.Add(wi + 3);
            triangles.Add(wi + 1);
            triangles.Add(wi + 2);
        }

        var mesh = new Mesh { name = meshName };
        mesh.indexFormat =
            vertices.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;

        mesh.SetVertices(vertices);
        mesh.SetNormals(oppositeEndpoints);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, uv2s);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    [MenuItem("MC/创建立方体线框网格")]
    public static void MakeCubeWireframe()
    {
        Mesh m = CreateCubeWireframeMesh();
        var go = new GameObject("CubeWireframeMesh");
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = m;
        var mr = go.AddComponent<MeshRenderer>();
        Shader s =
            Shader.Find("Custom/LinefyLinesPixelBillboardInstanced")
            ?? Shader.Find("Hidden/Linefy/LinesPixelBillboard")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Standard");
        if (s != null)
        {
            var mat = new Material(s);
            mat.SetColor("_Color", Color.black);
            mat.SetFloat("_WidthMultiplier", 2f);
            mat.enableInstancing = true;
            mr.sharedMaterial = mat;
        }

        Selection.activeGameObject = go;
    }

    [MenuItem("MC/打开线框网格生成面板")]
    public static void OpenWireframeGeneratorWindow()
    {
        WireframeGeneratorWindow.Open();
    }

    class WireframeGeneratorWindow : EditorWindow
    {
        readonly List<Vector3> positions = new List<Vector3>();
        readonly List<PolygonInput> polygons = new List<PolygonInput>();
        Vector2 scroll;
        float lineWidth = 1f;
        string meshName = "GeneratedWireframeMesh";

        public static void Open()
        {
            var window = GetWindow<WireframeGeneratorWindow>("线框网格生成");
            window.minSize = new Vector2(420f, 520f);
            window.EnsureDefaultCube();
            window.Show();
        }

        void OnEnable()
        {
            EnsureDefaultCube();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Positions / Polygons", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("polygons 中每个 corner 填 position 索引；生成时会把 polygon 相邻 corner 转成唯一无向边，并输出 Linefy Lines shader 兼容的线框 mesh。", MessageType.Info);

            meshName = EditorGUILayout.TextField("Mesh Name", meshName);
            lineWidth = EditorGUILayout.FloatField("Line Width", lineWidth);
            lineWidth = Mathf.Max(0.0001f, lineWidth);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("填入默认 Cube"))
                SetDefaultCube();
            if (GUILayout.Button("清空"))
            {
                positions.Clear();
                polygons.Clear();
            }
            EditorGUILayout.EndHorizontal();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawPositions();
            EditorGUILayout.Space(8f);
            DrawPolygons();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(positions.Count == 0 || polygons.Count == 0))
            {
                if (GUILayout.Button("生成线框网格", GUILayout.Height(32f)))
                    Generate();
            }
        }

        void DrawPositions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("positions", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(28f)))
                positions.Add(Vector3.zero);
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < positions.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                positions[i] = EditorGUILayout.Vector3Field(i.ToString(), positions[i]);
                if (GUILayout.Button("-", GUILayout.Width(24f)))
                {
                    positions.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        void DrawPolygons()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("polygons", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(28f)))
                polygons.Add(new PolygonInput { corners = new List<int> { 0, 1, 2 } });
            EditorGUILayout.EndHorizontal();

            for (int p = 0; p < polygons.Count; p++)
            {
                PolygonInput polygon = polygons[p];
                if (polygon.corners == null)
                    polygon.corners = new List<int>();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"polygon {p}", EditorStyles.boldLabel);
                if (GUILayout.Button("+ corner", GUILayout.Width(80f)))
                    polygon.corners.Add(0);
                if (GUILayout.Button("-", GUILayout.Width(24f)))
                {
                    polygons.RemoveAt(p);
                    p--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                for (int c = 0; c < polygon.corners.Count; c++)
                {
                    EditorGUILayout.BeginHorizontal();
                    polygon.corners[c] = EditorGUILayout.IntField($"corner {c} position", polygon.corners[c]);
                    if (GUILayout.Button("-", GUILayout.Width(24f)))
                    {
                        polygon.corners.RemoveAt(c);
                        c--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }

        void Generate()
        {
            Mesh mesh = CreatePolygonWireframeMesh(positions.ToArray(), polygons, lineWidth, meshName);
            var go = new GameObject(meshName);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            Shader s =
                Shader.Find("Custom/LinefyLinesPixelBillboardInstanced")
                ?? Shader.Find("Hidden/Linefy/LinesPixelBillboard")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Standard");
            if (s != null)
            {
                var mat = new Material(s);
                mat.SetColor("_Color", Color.black);
                mat.SetFloat("_WidthMultiplier", 2f);
                mat.enableInstancing = true;
                mr.sharedMaterial = mat;
            }

            Selection.activeGameObject = go;
        }

        void EnsureDefaultCube()
        {
            if (positions.Count == 0 && polygons.Count == 0)
                SetDefaultCube();
        }

        void SetDefaultCube()
        {
            positions.Clear();
            polygons.Clear();

            positions.Add(new Vector3(-0.5f, -0.5f, -0.5f));
            positions.Add(new Vector3(0.5f, -0.5f, -0.5f));
            positions.Add(new Vector3(0.5f, 0.5f, -0.5f));
            positions.Add(new Vector3(-0.5f, 0.5f, -0.5f));
            positions.Add(new Vector3(-0.5f, -0.5f, 0.5f));
            positions.Add(new Vector3(0.5f, -0.5f, 0.5f));
            positions.Add(new Vector3(0.5f, 0.5f, 0.5f));
            positions.Add(new Vector3(-0.5f, 0.5f, 0.5f));

            polygons.Add(new PolygonInput { corners = new List<int> { 0, 1, 2, 3 } });
            polygons.Add(new PolygonInput { corners = new List<int> { 7, 6, 5, 4 } });
            polygons.Add(new PolygonInput { corners = new List<int> { 4, 5, 1, 0 } });
            polygons.Add(new PolygonInput { corners = new List<int> { 5, 6, 2, 1 } });
            polygons.Add(new PolygonInput { corners = new List<int> { 7, 3, 2, 6 } });
            polygons.Add(new PolygonInput { corners = new List<int> { 4, 0, 3, 7 } });
        }
    }
}