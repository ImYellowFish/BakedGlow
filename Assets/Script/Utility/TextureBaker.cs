using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bake mesh color data to a texture.
/// </summary>
public class TextureBaker
{
    private int[] triangles;
    private Color[] colors;
    private Vector2[] uv;

    public TextureBaker(int[] triangles, Color[] colors, Vector2[] uv)
    {
        this.triangles = triangles;
        this.colors = colors;
        this.uv = uv;
    }

    public void Bake(int width, int height, string path)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Step(tex, i, width, height);
        }

        tex.Apply();
        WriteToPng("Assets/generated.png", tex);
    }

    /// <summary>
    /// Paint a triangle of a specified index
    /// </summary>
    private void Step(Texture2D tex, int index, int width, int height)
    {
        var ca = colors[triangles[index]];
        var cb = colors[triangles[index + 1]];
        var cc = colors[triangles[index + 2]];

        var uva = Repeat01(uv[triangles[index]]);
        var uvb = Repeat01(uv[triangles[index + 1]]);
        var uvc = Repeat01(uv[triangles[index + 2]]);

        var ta = new Vector2(uva.x * width, uva.y * height);
        var tb = new Vector2(uvb.x * width, uvb.y * height);
        var tc = new Vector2(uvc.x * width, uvc.y * height);

        FillInTextureTriangle(tex, ta, tb, tc, ca, cb, cc);
    }

    // Helpers
    private static void WriteToPng(string path, Texture2D texture)
    {
        var pngData = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, pngData);
    }

    /// <summary>
    /// Fill a triangle area on the texture, lerping vertex colors.
    /// </summary>
    /// 
    private static void FillInTextureTriangle(Texture2D tex,
        Vector2 coord_a, Vector2 coord_b, Vector2 coord_c,
        Color color_a, Color color_b, Color color_c)
    {
        int xmin = Mathf.RoundToInt(Mathf.Min(coord_a.x, coord_b.x, coord_c.x));
        int xmax = Mathf.RoundToInt(Mathf.Max(coord_a.x, coord_b.x, coord_c.x));
        int ymin = Mathf.RoundToInt(Mathf.Min(coord_a.y, coord_b.y, coord_c.y));
        int ymax = Mathf.RoundToInt(Mathf.Max(coord_a.y, coord_b.y, coord_c.y));

        Color color = Color.clear;
        for (int x = xmin; x <= xmax; x++)
        {
            for (int y = ymin; y <= ymax; y++)
            {
                var baryCoord = new Barycentric(coord_a, coord_b, coord_c, new Vector2(x, y));
                if (baryCoord.IsInside)
                {
                    color = baryCoord.Interpolate(color_a, color_b, color_c);
                    tex.SetPixel(x, y, color);
                }
            }
        }
    }

    private static Vector2 Repeat01(Vector2 v)
    {
        v.x = Mathf.Repeat(v.x, 1);
        v.y = Mathf.Repeat(v.y, 1);
        return v;
    }
}
