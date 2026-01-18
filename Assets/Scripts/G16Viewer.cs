using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class G16Viewer : MonoBehaviour
{
    [Header("Path to G16 file")]
    public string g16Path = "";

    [Header("Load automatically on Start")]
    public bool loadOnStart = true;

    [Header("Quad scale multiplier")]
    public float scale = 1.0f;

    private Texture2D tex;
    private GameObject quad;

    void Start()
    {
        return; // временно: не грузим g16 в Unity
        //if (loadOnStart)
         //   LoadG16();
    }

    [DllImport("Kozak_FalGraphics", CallingConvention = CallingConvention.Cdecl)]
    public static extern int FG_LoadG16(
        [MarshalAs(UnmanagedType.LPStr)] string path,
        out int width,
        out int height,
        out int frames,
        out IntPtr rgbaBuffer);

    [DllImport("Kozak_FalGraphics", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FG_FreeBuffer(IntPtr ptr);

    public void LoadG16()
    {
        int w, h, frames;
        IntPtr ptr;

        Debug.Log("[G16Viewer] Calling FG_LoadG16...");

        int res = FG_LoadG16(g16Path, out w, out h, out frames, out ptr);

        Debug.Log($"[G16Viewer] FG_LoadG16 returned code: {res}");

        if (res != 0)
        {
            Debug.LogError("[G16Viewer] Failed to load G16. Error = " + res);
            return;
        }

        int size = w * h * 4 * frames;
        byte[] rgba = new byte[size];
        Marshal.Copy(ptr, rgba, 0, size);

        FG_FreeBuffer(ptr);

        tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.LoadRawTextureData(rgba);
        tex.Apply();

        Debug.Log($"[G16Viewer] Loaded texture: {w}x{h}, frames={frames}");

        if (quad == null)
        {
            quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(transform);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localScale = new Vector3(w / 64f * scale, h / 64f * scale, 1);
        }

        quad.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit/Texture"));
        quad.GetComponent<MeshRenderer>().material.mainTexture = tex;
    }
}