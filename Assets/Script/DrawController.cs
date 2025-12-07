using UnityEngine;
using UnityEngine.UI;

public class DrawController : MonoBehaviour
{
    public RawImage drawArea;
    public int textureSize = 1024;

    private Texture2D drawTex;
    private bool drawing = false;
    private Vector2 prevPos;

    void Start()
    {
        drawTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
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

    // -------------------------------------------------------------------
    // CONVERT SCREEN TO TEXTURE COORD
    // -------------------------------------------------------------------
    bool GetTextureCoord(Vector2 screenPos, out int tx, out int ty)
    {
        tx = ty = 0;

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawArea.rectTransform, screenPos, null, out local)) return false;

        Rect r = drawArea.rectTransform.rect;

        float px = (local.x - r.x) / r.width;
        float py = (local.y - r.y) / r.height;

        if (px < 0 || px > 1 || py < 0 || py > 1) return false;

        tx = Mathf.Clamp((int)(px * textureSize), 0, textureSize - 1);
        ty = Mathf.Clamp((int)(py * textureSize), 0, textureSize - 1);
        return true;
    }

    // -------------------------------------------------------------------
    // TOUCH HANDLING
    // -------------------------------------------------------------------
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
                DrawLine((int)prevPos.x, (int)prevPos.y, x, y, 8);
                prevPos = new Vector2(x, y);
                drawTex.Apply();
            }
            else if (t.phase == TouchPhase.Ended)
            {
                drawing = false;
            }
        }
    }

    // -------------------------------------------------------------------
    // MOUSE HANDLING (EDITOR)
    // -------------------------------------------------------------------
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
                DrawLine((int)prevPos.x, (int)prevPos.y, x, y, 8);
                prevPos = new Vector2(x, y);
                drawTex.Apply();
            }
        }
    }

    // -------------------------------------------------------------------
    // DRAW LINE (Bresenham + brush width)
    // -------------------------------------------------------------------
    void DrawLine(int x0, int y0, int x1, int y1, int width)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = (dx > dy ? dx : -dy) / 2, e2;

        while (true)
        {
            for (int wx = -width / 2; wx <= width / 2; wx++)
            {
                for (int wy = -width / 2; wy <= width / 2; wy++)
                {
                    int px = x0 + wx;
                    int py = y0 + wy;

                    if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                        drawTex.SetPixel(px, py, Color.black);
                }
            }

            if (x0 == x1 && y0 == y1) break;

            e2 = err;
            if (e2 > -dx) { err -= dy; x0 += sx; }
            if (e2 < dy) { err += dx; y0 += sy; }
        }
    }

    // -------------------------------------------------------------------
    public void ClearTexture()
    {
        Color32[] cols = new Color32[textureSize * textureSize];
        for (int i = 0; i < cols.Length; i++)
            cols[i] = new Color32(255, 255, 255, 255);

        drawTex.SetPixels32(cols);
        drawTex.Apply();
    }

    public Texture2D GetDrawTexture()
    {
        return drawTex;
    }
}
