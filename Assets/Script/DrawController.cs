using UnityEngine;
using UnityEngine.UI;

public class DrawController : MonoBehaviour
{
    public RawImage drawArea;
    public int textureSize = 256;
    public int brushSize = 16; 

    private Texture2D drawTex;
    private bool drawing = false;
    private Vector2 prevPos;

    void Start()
    {
        if (drawArea == null)
        {
            Debug.LogError("DrawArea belum di assign!");
            return;
        }

        drawTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        drawTex.filterMode = FilterMode.Point;

        ClearTexture();
        drawArea.texture = drawTex;
    }

    void Update()
    {
        HandleTouch();
#if UNITY_EDITOR
        HandleMouse();
#endif
    }

    public void ClearCanvas()
    {
        drawing = false;
        ClearTexture();
    }

    // =========================================================
    // 🔥 KONVERSI KOORDINAT
    // =========================================================
    bool GetTextureCoord(Vector2 screenPos, out int tx, out int ty)
    {
        tx = ty = 0;

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawArea.rectTransform, screenPos, null, out local))
            return false;

        Rect r = drawArea.rectTransform.rect;

        float px = (local.x - r.x) / r.width;
        float py = (local.y - r.y) / r.height;

        if (px < 0 || px > 1 || py < 0 || py > 1)
            return false;

        tx = Mathf.Clamp((int)(px * textureSize), 0, textureSize - 1);
        ty = Mathf.Clamp((int)(py * textureSize), 0, textureSize - 1);

        return true;
    }

    // =========================================================
    // TOUCH INPUT
    // =========================================================
    void HandleTouch()
    {
        if (Input.touchCount == 0) return;

        Touch t = Input.GetTouch(0);

        if (GetTextureCoord(t.position, out int x, out int y))
        {
            if (t.phase == TouchPhase.Began)
            {
                drawing = true;
                prevPos = new Vector2(x, y);
            }
            else if (t.phase == TouchPhase.Moved && drawing)
            {
                DrawLineSmooth((int)prevPos.x, (int)prevPos.y, x, y);
                prevPos = new Vector2(x, y);
                drawTex.Apply();
            }
            else if (t.phase == TouchPhase.Ended)
            {
                drawing = false;
            }
        }
    }

    // =========================================================
    // MOUSE INPUT
    // =========================================================
    void HandleMouse()
    {
        Vector2 mp = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            if (GetTextureCoord(mp, out int x, out int y))
            {
                drawing = true;
                prevPos = new Vector2(x, y);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            drawing = false;
        }

        if (drawing && Input.GetMouseButton(0))
        {
            if (GetTextureCoord(mp, out int x, out int y))
            {
                DrawLineSmooth((int)prevPos.x, (int)prevPos.y, x, y);
                prevPos = new Vector2(x, y);
                drawTex.Apply();
            }
        }
    }

    // =========================================================
    // 🔥 LINE SMOOTH (ANTI PUTUS-PUTUS)
    // =========================================================
    void DrawLineSmooth(int x0, int y0, int x1, int y1)
    {
        float dist = Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1));
        int steps = Mathf.CeilToInt(dist);

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));

            DrawBrushCircle(x, y, brushSize);
        }
    }

    // =========================================================
    // 🔥 BRUSH BULAT (LEBIH NATURAL)
    // =========================================================
    void DrawBrushCircle(int cx, int cy, int size)
    {
        int r = size / 2;

        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y <= r * r)
                {
                    int px = cx + x;
                    int py = cy + y;

                    if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                    {
                        drawTex.SetPixel(px, py, Color.white);
                    }
                }
            }
        }
    }

    // =========================================================
    // CLEAR
    // =========================================================
    public void ClearTexture()
    {
        Color32[] cols = new Color32[textureSize * textureSize];

        for (int i = 0; i < cols.Length; i++)
            cols[i] = new Color32(0, 0, 0, 255);

        drawTex.SetPixels32(cols);
        drawTex.Apply();
    }

    public Texture2D GetDrawTexture()
    {
        return drawTex;
    }

    // =========================================================
    // 🔥 SAVE TEXTURE TO FILE
    // =========================================================
    public string SaveTextureToFile()
    {
        string path = Application.streamingAssetsPath + "/drawn_image.png";

        byte[] bytes = drawTex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);

        Debug.Log("Image saved to: " + path);
        return path;
    }
}