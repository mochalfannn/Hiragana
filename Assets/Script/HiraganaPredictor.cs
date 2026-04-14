using UnityEngine;
using Unity.Barracuda;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.IO;

public class HiraganaPredictor : MonoBehaviour
{
    public NNModel modelAsset;
    public DrawController drawController;
    public TMP_Text resultText;

    private IWorker worker;
    private string[] labels;

    private bool isLabelReady = false;

    void Start()
    {
        var model = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, model);

        StartCoroutine(LoadLabels());
    }

    IEnumerator LoadLabels()
    {
        string path = Application.streamingAssetsPath + "/labels2.json";

        UnityWebRequest www = UnityWebRequest.Get(path);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Gagal load labels: " + www.error);
        }
        else
        {
            string json = www.downloadHandler.text;
            labels = JsonHelper.FromJson<string>(json);

            isLabelReady = true;
        }
    }

    public void OnPredictButton()
    {
        if (!isLabelReady)
        {
            resultText.text = "Loading...";
            return;
        }

        Texture2D tex = drawController.GetDrawTexture();

        if (IsEmpty(tex))
        {
            resultText.text = "Gambar kosong";
            return;
        }

        Predict();
    }

    public void OnPredictFromFile()
    {
        if (!isLabelReady)
        {
            resultText.text = "Loading...";
            return;
        }

        string path = drawController.SaveTextureToFile();
        byte[] fileData = System.IO.File.ReadAllBytes(path);

        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(fileData);

        if (IsEmpty(tex))
        {
            resultText.text = "Gambar kosong";
            return;
        }

        PredictFromTexture(tex);
    }

    void PredictFromTexture(Texture2D tex)
    {
        // 🔥 1. Crop ke area tulisan
        Texture2D cropped = CropToContent(tex);

        // 🔥 2. Resize
        Texture2D resized = Resize(cropped, 32, 32);

        float[] inputData = new float[32 * 32];

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float gray = resized.GetPixel(x, y).grayscale;

                // 🔥 Threshold biar tegas
                gray = (gray > 0.3f) ? 1f : 0f;

                inputData[y * 32 + x] = gray;
            }
        }

        using (Tensor input = new Tensor(1, 32, 32, 1, inputData))
        {
            worker.Execute(input);
            Tensor output = worker.PeekOutput();

            float[] probs = output.ToReadOnlyArray();

            // 🔥 TOP 3 hasil
            int[] indices = new int[probs.Length];
            for (int i = 0; i < probs.Length; i++) indices[i] = i;

            System.Array.Sort(indices, (a, b) => probs[b].CompareTo(probs[a]));

            string result = "";
            for (int i = 0; i < 3; i++)
            {
                result += $"{labels[indices[i]]} ({probs[indices[i]] * 100f:F1}%)\n";
            }

            resultText.text = result;
        }
    }

    // =========================================================
    // 🔥 PREDICT (SUDAH DIPERBAIKI)
    // =========================================================
    void Predict()
    {
        Texture2D drawTex = drawController.GetDrawTexture();

        // 🔥 1. Crop ke area tulisan
        Texture2D cropped = CropToContent(drawTex);

        // 🔥 2. Resize
        Texture2D resized = Resize(cropped, 32, 32);

        float[] inputData = new float[32 * 32];

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float gray = resized.GetPixel(x, y).grayscale;

                // ❗ JANGAN INVERT (sudah sesuai training)
                // gray = 1.0f - gray;

                // 🔥 Threshold biar tegas
                gray = (gray > 0.3f) ? 1f : 0f;

                inputData[y * 32 + x] = gray;
            }
        }

        using (Tensor input = new Tensor(1, 32, 32, 1, inputData))
        {
            worker.Execute(input);
            Tensor output = worker.PeekOutput();

            float[] probs = output.ToReadOnlyArray();

            // 🔥 TOP 3 hasil
            int[] indices = new int[probs.Length];
            for (int i = 0; i < probs.Length; i++) indices[i] = i;

            System.Array.Sort(indices, (a, b) => probs[b].CompareTo(probs[a]));

            string result = "";
            for (int i = 0; i < 3; i++)
            {
                result += $"{labels[indices[i]]} ({probs[indices[i]] * 100f:F1}%)\n";
            }

            resultText.text = result;
        }
    }

    // =========================================================
    // 🔥 CROP (PALING PENTING)
    // =========================================================
    Texture2D CropToContent(Texture2D tex)
    {
        int minX = tex.width, minY = tex.height;
        int maxX = 0, maxY = 0;

        for (int y = 0; y < tex.height; y++)
        {
            for (int x = 0; x < tex.width; x++)
            {
                float g = tex.GetPixel(x, y).grayscale;

                if (g > 0.1f)
                {
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
        }

        int w = maxX - minX;
        int h = maxY - minY;

        if (w <= 0 || h <= 0)
            return tex;

        Texture2D cropped = new Texture2D(w, h, TextureFormat.RGB24, false);
        cropped.SetPixels(tex.GetPixels(minX, minY, w, h));
        cropped.Apply();

        return cropped;
    }

    // =========================================================
    // CLEAR
    // =========================================================
    public void OnClearButton()
    {
        drawController.ClearCanvas();
        resultText.text = "Hasil: -";
    }

    // =========================================================
    // 🔥 CHECK KOSONG (LEBIH AKURAT)
    // =========================================================
    bool IsEmpty(Texture2D tex)
    {
        for (int y = 0; y < tex.height; y += 8)
        {
            for (int x = 0; x < tex.width; x += 8)
            {
                if (tex.GetPixel(x, y).grayscale > 0.1f)
                    return false;
            }
        }
        return true;
    }

    // =========================================================
    // 🔥 RESIZE (LEBIH AMAN)
    // =========================================================
    Texture2D Resize(Texture2D tex, int w, int h)
    {
        RenderTexture rt = RenderTexture.GetTemporary(w, h);
        Graphics.Blit(tex, rt);

        RenderTexture.active = rt;

        Texture2D newTex = new Texture2D(w, h, TextureFormat.RGB24, false);
        newTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        newTex.Apply();

        RenderTexture.ReleaseTemporary(rt);
        return newTex;
    }
}