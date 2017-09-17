using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalSmoother : MonoBehaviour {
    public bool showNormalInLocalSpace;
    public bool useDistanceBasedProx;
    public int subDivisionLevel = 1;

    public float proxFactor = 1;
    public int proxDist = 2;

    private Mesh mesh;
    private float proximitySize;

    [HideInInspector]
    private Vector3[] vertices;
    [HideInInspector]
    private Vector3[] normals;
    [HideInInspector]
    private int[] triangles;
    
    [HideInInspector]
    private Vector3[] initNormals;
    [HideInInspector]
    private Vector4[] initTangents;
    
    private void Start()
    {
        Init();

        if(showNormalInLocalSpace)
            SetLocalSpaceNormalToVertexColor(mesh);
        else
            SetTangentSpaceNormalToVertexColor(mesh, initNormals, initTangents);
        
    }

    [ContextMenu("process")]
    public void Process()
    {
        FlattenNormal();

        if (showNormalInLocalSpace)
            SetLocalSpaceNormalToVertexColor(mesh);
        else
            SetTangentSpaceNormalToVertexColor(mesh, initNormals, initTangents);
    }

    
    private void Init()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        MeshSubdivide.Subdivide(mesh, subDivisionLevel);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        vertices = mesh.vertices;
        triangles = mesh.triangles;
        normals = mesh.normals;
        initNormals = mesh.normals;
        initTangents = mesh.tangents;
        ComputeProximitySize();
    }

    private void ComputeProximitySize()
    {
        float sum = 0;

        for(int i = 0; i < triangles.Length; i+=3)
        {
            var a = vertices[triangles[i]];
            var b = vertices[triangles[i+1]];
            var c = vertices[triangles[i+2]];
            sum += Vector3.Distance(a, b);
            sum += Vector3.Distance(a, c);
            sum += Vector3.Distance(b, c);
        }

        proximitySize = sum / triangles.Length;
    }

    private List<int> prox = new List<int>();
    private void FlattenNormal()
    {
        var newNormals = new Vector3[normals.Length];

        for(int i = 0; i < vertices.Length; i++)
        {
            if (useDistanceBasedProx)
                GetNearByVertices(vertices[i], proximitySize * proxFactor, ref prox);
            else
            {
                GetNeighborVertices(i, proxDist, ref prox);
            }
            newNormals[i] = ComputeAverageNormal(i, prox);
        }

        normals = newNormals;
        mesh.normals = newNormals;        
        mesh.RecalculateTangents();
    }

    [ContextMenu("Bake")]
    public void BakeMeshColorsToTexture()
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        var colors = mesh.colors;
        var uv = mesh.uv;

        for(int i = 0; i < triangles.Length; i += 3)
        {
            Debug.Log("progress: " + (float)i / triangles.Length);
            
            var ca = colors[triangles[i]];
            var cb = colors[triangles[i + 1]];
            var cc = colors[triangles[i + 2]];

            var uva = Repeat01(uv[triangles[i]]);
            var uvb = Repeat01(uv[triangles[i + 1]]);
            var uvc = Repeat01(uv[triangles[i + 2]]);

            var ta = uva * size;
            var tb = uvb * size;
            var tc = uvc * size;

            FillInTextureTriangle(tex, ta, tb, tc, ca, cb, cc);
        }

        var pngData = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes("Assets/generated.png", pngData);
    }

    // helpers
    private Vector2 Repeat01(Vector2 v)
    {
        v.x = Mathf.Repeat(v.x, 1);
        v.y = Mathf.Repeat(v.y, 1);
        return v;
    }

    private void FillInTextureTriangle(Texture2D tex, 
        Vector2 ta, Vector2 tb, Vector2 tc,
        Color ca, Color cb, Color cc)
    {
        int xmin = Mathf.RoundToInt(Mathf.Min(ta.x, tb.x, tc.x));
        int xmax = Mathf.RoundToInt(Mathf.Max(ta.x, tb.x, tc.x));
        int ymin = Mathf.RoundToInt(Mathf.Min(ta.y, tb.y, tc.y));
        int ymax = Mathf.RoundToInt(Mathf.Max(ta.y, tb.y, tc.y));

        Color color = Color.clear;
        for(int x = xmin; x <= xmax; x++)
        {
            for(int y = ymin; y <= ymax; y++)
            {
                var baryCoord = new Barycentric(ta, tb, tc, new Vector2(x, y));
                if (baryCoord.IsInside)
                {
                    color = baryCoord.Interpolate(ca, cb, cc);
                    tex.SetPixel(x, y, color);
                }
            }
        }
    }

    private Vector3 ComputeAverageNormal(int index, List<int> prox)
    {
        Vector3 sum = Vector3.zero;
        float sumWeight = 0;
        for(int i = 0; i < prox.Count; i++)
        {
            float sqrDist = Vector3.SqrMagnitude(vertices[index] - vertices[prox[i]]);
            float weight = 1 / (sqrDist + 1);
            sum += normals[prox[i]] * weight;
            sumWeight += weight;
        }
        return (sum / sumWeight).normalized;
    }

    private void ValidateVerticesMaxDist(float maxDist, int index, ref List<int> prox)
    {
        for(int i = 0; i < prox.Count; i++)
        {
            if(Vector3.Distance(vertices[index], vertices[prox[i]]) > maxDist)
            {
                prox.RemoveAt(i);
            }

        }
    }

    private List<int> scannedVertices = new List<int>();
    private void GetNeighborVertices(int index, int maxDist, ref List<int> prox, bool firstSearch = true)
    {
        if (firstSearch)
        {
            prox.Clear();
            scannedVertices.Clear();
        }

        if (scannedVertices.Contains(index))
        {
            return;
        }
        
        // prox.Add(index);
        scannedVertices.Add(index);

        for (int i = 0; i < triangles.Length; i+=3)
        {
            
            if(triangles[i] == index || triangles[i+1] == index || triangles[i+2] == index)
            {
                if(!prox.Contains(triangles[i]))
                    prox.Add(triangles[i]);
            }
        }

        if(maxDist > 1)
        {
            for (int i = 0; i < prox.Count; i++)
            {
                GetNeighborVertices(prox[i], maxDist - 1, ref prox, false);
            }
        }
    }


    private void GetNearByVertices(Vector3 v, float maxDist, ref List<int> prox)
    {
        float sqrDist = maxDist * maxDist;
        prox.Clear();
        scannedVertices.Clear();

        for(int i = 0; i < vertices.Length; i++)
        {
            if((vertices[i] - v).sqrMagnitude < sqrDist)
            {
                prox.Add(i);
            }
        }
    }

    private static void SetTangentSpaceNormalToVertexColor(Mesh mesh, Vector3[] initNormals, Vector4[] initTangents)
    {
        var normals = mesh.normals;
        Color[] colors = new Color[normals.Length];

        for (int i = 0; i < normals.Length; i++)
        {
            var binormal = Vector3.Cross(initNormals[i], initTangents[i]) * initTangents[i].w;
            Vector4 t = initTangents[i];
            Vector4 b = binormal;
            Vector4 n = initNormals[i];
            Vector4 l = new Vector4(0, 0, 0, 1);
            t.w = 1;
            b.w = 1;
            n.w = 1;

            Matrix4x4 m = new Matrix4x4(t, b, n, l);
            //colors[i] = PackVector3(normals[i]);
            colors[i] = PackVector3(Matrix4x4.Transpose(m).MultiplyVector(normals[i]));
        }
        mesh.colors = colors;
    }

    private static void SetLocalSpaceNormalToVertexColor(Mesh mesh)
    {
        var normals = mesh.normals;
        var tangents = mesh.tangents;
        Color[] colors = new Color[normals.Length];

        for (int i = 0; i < normals.Length; i++)
        {
            var binormal = Vector3.Cross(normals[i], tangents[i]) * tangents[i].w;
            Vector4 t = tangents[i];
            Vector4 b = binormal;
            Vector4 n = normals[i];
            t.w = 1;
            b.w = 1;
            n.w = 1;

            colors[i] = PackVector3(normals[i]);
        }
        mesh.colors = colors;
    }

    private static Color PackVector3(Vector3 v)
    {
        v = v / 2 + 0.5f * Vector3.one;
        return new Color(v.x, v.y, v.z, 1);
    }

    
}
