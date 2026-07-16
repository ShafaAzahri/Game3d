using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class RemoveFakePngBackground : Editor
{
    [MenuItem("Assets/Antigravity/Hapus Background Fake PNG", false, 10)]
    private static void CleanFakePngs()
    {
        var selectedObjects = Selection.objects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Hapus Background", "Pilih satu atau beberapa texture di Project View terlebih dahulu.", "OK");
            return;
        }

        int successCount = 0;

        foreach (var obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) continue;

            // Pastikan asset adalah file gambar
            string ext = Path.GetExtension(path).ToLower();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".tga") continue;

            // 1. Paksa import setting agar readable dan tipenya Default terlebih dahulu agar pixel bisa dibaca
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool wasReadable = importer.isReadable;
            TextureImporterType oldType = importer.textureType;

            importer.isReadable = true;
            importer.textureType = TextureImporterType.Default; // Harus Default agar bisa GetPixels
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            // 2. Load texture
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;

            // Buat texture salinan agar bisa dibaca tulis secara dinamis
            Texture2D readableTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            readableTex.SetPixels(tex.GetPixels());
            readableTex.Apply();

            // 3. Proses hapus background dengan flood fill
            RemoveBackground(readableTex);
            readableTex.Apply();

            // 4. Save kembali ke format PNG
            byte[] bytes = readableTex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            // 5. Kembalikan setting import, ubah tipe ke Sprite (2D and UI) dan aktifkan Alpha Is Transparency
            importer.isReadable = wasReadable;
            importer.textureType = TextureImporterType.Sprite; // Otomatis ubah jadi Sprite!
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            successCount++;
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Hapus Background Selesai", $"Berhasil memproses {successCount} gambar.", "OK");
    }

    private static void RemoveBackground(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        Color32[] pixels = tex.GetPixels32();
        bool[] visited = new bool[pixels.Length];
        
        Queue<int> queue = new Queue<int>();

        // Masukkan semua pixel di border ke queue flood fill
        for (int x = 0; x < w; x++)
        {
            AddPixel(x, 0, w, h, queue, visited, pixels);
            AddPixel(x, h - 1, w, h, queue, visited, pixels);
        }
        for (int y = 0; y < h; y++)
        {
            AddPixel(0, y, w, h, queue, visited, pixels);
            AddPixel(w - 1, y, w, h, queue, visited, pixels);
        }

        // Jalankan flood fill
        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            pixels[idx] = new Color32(0, 0, 0, 0); // Set alpha ke 0

            int px = idx % w;
            int py = idx / w;

            // Cek tetangga (4 arah)
            TryAddNeighbor(px + 1, py, w, h, queue, visited, pixels);
            TryAddNeighbor(px - 1, py, w, h, queue, visited, pixels);
            TryAddNeighbor(px, py + 1, w, h, queue, visited, pixels);
            TryAddNeighbor(px, py - 1, w, h, queue, visited, pixels);
        }

        tex.SetPixels32(pixels);
    }

    private static void AddPixel(int x, int y, int w, int h, Queue<int> queue, bool[] visited, Color32[] pixels)
    {
        int idx = y * w + x;
        if (idx < 0 || idx >= pixels.Length) return;
        if (visited[idx]) return;

        if (IsCheckerboardColor(pixels[idx]))
        {
            visited[idx] = true;
            queue.Enqueue(idx);
        }
    }

    private static void TryAddNeighbor(int x, int y, int w, int h, Queue<int> queue, bool[] visited, Color32[] pixels)
    {
        if (x < 0 || x >= w || y < 0 || y >= h) return;
        AddPixel(x, y, w, h, queue, visited, pixels);
    }

    private static bool IsCheckerboardColor(Color32 c)
    {
        // 1. Warna netral memiliki perbedaan R, G, B sangat kecil
        int d1 = Mathf.Abs(c.r - c.g);
        int d2 = Mathf.Abs(c.g - c.b);
        int d3 = Mathf.Abs(c.r - c.b);

        if (d1 > 8 || d2 > 8 || d3 > 8) return false;

        // 2. Cek warna putih (R, G, B > 235)
        if (c.r >= 235 && c.g >= 235 && c.b >= 235) return true;

        // 3. Cek warna abu-abu checkerboard (R, G, B sekitar 180-220)
        if (c.r >= 180 && c.r <= 220) return true;

        return false;
    }
}
