using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetNormalAsVertexColor : MonoBehaviour {
    
    // Use this for initialization
    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        var normals = mesh.normals;
        var tangents = mesh.tangents;
        Color[] colors = new Color[normals.Length];

        
        for (int i = 0; i < normals.Length; i++)
        {
            var binormal = Vector3.Cross(normals[i], tangents[i]) * tangents[i].w;
            Vector4 t = tangents[i];
            Vector4 b = binormal;
            Vector4 n = normals[i];
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

    private Color PackVector3(Vector3 v)
    {
        v = v / 2 + 0.5f * Vector3.one;
        Debug.Log("n: " + v);
        return new Color(v.x, v.y, v.z, 1);
    }
	
}
