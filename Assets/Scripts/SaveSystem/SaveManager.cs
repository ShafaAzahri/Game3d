using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Bertanggung jawab nulis & baca file save (JSON) ke disk.
/// File disimpan di Application.persistentDataPath (folder save aman bawaan OS).
///
/// Mendukung beberapa slot: save_0.json, save_1.json, dst.
/// Kelas ini murni file I/O — tidak menyentuh state game.
/// Gunakan lewat GameManager, jangan langsung dari sistem lain.
/// </summary>
public static class SaveManager
{
    public const int MaxSlots = 3;

    private static string FileNameForSlot(int slot) => $"save_{slot}.json";

    private static string PathForSlot(int slot)
        => Path.Combine(Application.persistentDataPath, FileNameForSlot(slot));

    // ─────────────────────────────────────────────────────────────
    // WRITE
    // ─────────────────────────────────────────────────────────────

    /// <summary>Tulis SaveData ke slot tertentu. Return true kalau sukses.</summary>
    public static bool Save(int slot, SaveData data)
    {
        if (data == null) { Debug.LogError("[SaveManager] Save gagal: data null."); return false; }

        try
        {
            data.savedAtIso = DateTime.Now.ToString("o");
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(PathForSlot(slot), json);
            Debug.Log($"[SaveManager] Tersimpan ke slot {slot}: {PathForSlot(slot)}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Save slot {slot} gagal: {e.Message}");
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // READ
    // ─────────────────────────────────────────────────────────────

    /// <summary>Baca SaveData dari slot. Return null kalau tidak ada / gagal.</summary>
    public static SaveData Load(int slot)
    {
        string path = PathForSlot(slot);
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                Debug.LogError($"[SaveManager] Slot {slot} korup (parse null).");
                return null;
            }

            // Pastikan list tidak null (jaga-jaga save lama)
            data.inventory       ??= new System.Collections.Generic.List<ItemStack>();
            data.plots           ??= new System.Collections.Generic.List<PlotSave>();
            data.unlockedRecipes ??= new System.Collections.Generic.List<string>();
            data.completedSteps  ??= new System.Collections.Generic.List<string>();

            Debug.Log($"[SaveManager] Slot {slot} dimuat (storyStep={data.storyStep}, hari={data.dayCount}).");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Load slot {slot} gagal: {e.Message}");
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // QUERY / DELETE
    // ─────────────────────────────────────────────────────────────

    public static bool HasSave(int slot) => File.Exists(PathForSlot(slot));

    public static void Delete(int slot)
    {
        string path = PathForSlot(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SaveManager] Slot {slot} dihapus.");
        }
    }

    /// <summary>Ringkasan singkat sebuah slot untuk ditampilkan di menu Load (tanpa load penuh ke game).</summary>
    public static SaveSlotInfo GetSlotInfo(int slot)
    {
        var data = Load(slot);
        if (data == null) return new SaveSlotInfo { slot = slot, exists = false };

        return new SaveSlotInfo
        {
            slot      = slot,
            exists    = true,
            storyStep = data.storyStep,
            dayCount  = data.dayCount,
            sceneName = data.sceneName,
            savedAt   = data.savedAtIso
        };
    }
}

/// <summary>Info ringkas slot save buat tampilan menu.</summary>
public struct SaveSlotInfo
{
    public int    slot;
    public bool   exists;
    public int    storyStep;
    public int    dayCount;
    public string sceneName;
    public string savedAt;
}
