﻿using UnityEngine;

public class SetVertexColor : MonoBehaviour
{
    public Color colorA;
    public Color colorB;

    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        // create new colors array where the colors will be created.
        Color[] colors = new Color[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            colors[i] = Color.Lerp(colorA, colorB, vertices[i].y);
        }

        // assign the array of colors to the Mesh.
        mesh.colors = colors;
    }
    
}