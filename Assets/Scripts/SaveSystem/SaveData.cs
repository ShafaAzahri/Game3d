using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wadah SEMUA data yang disimpan ke file save (JSON).
/// Satu objek SaveData = satu "kondisi permainan" lengkap.
///
/// Tambah field baru di sini kalau ada sistem baru yang perlu disimpan.
/// JANGAN ganti tipe/urutan field lama tanpa menaikkan saveVersion (biar save lama tetap kebaca).
/// </summary>
[Serializable]
public class SaveData
{
    // ── Meta ──
    // Versi format save. Naikkan kalau struktur berubah besar.
    public int saveVersion = 1;
    public string savedAtIso = "";   // kapan terakhir disimpan (ISO 8601)

    // ── Progress cerita / tutorial ──
    public int storyStep = 0;                                   // sampai mana cerita
    public List<string> completedSteps = new List<string>();    // flag tutorial / objektif selesai

    // ── Lokasi player ──
    public string sceneName = "Dunia";
    public bool hasPlayerPosition = false;
    public float playerX = 0f;
    public float playerY = 0f;
    public float playerZ = 0f;
    public float playerRotY = 0f;

    // ── Ekonomi & waktu ──
    public int   money     = 0;
    public int   dayCount  = 1;
    public float timeOfDay = 0f;     // jam 0-24 (atau sesuai TimeSystem)

    // ── Inventory ──
    public List<ItemStack> inventory = new List<ItemStack>();

    // ── Kebun ──
    public List<PlotSave> plots = new List<PlotSave>();

    // ── Resep yang sudah dibuka ──
    public List<string> unlockedRecipes = new List<string>();

    // ── Quest counters (paralel quest) ──
    public SerializableDictionary questCounters = new SerializableDictionary();
    
    // ── Hubungan (Love Meter) ──
    public int larasLovePoints = 0;

    // ─────────────────────────────────────────────────────────────
    // FACTORY
    // ─────────────────────────────────────────────────────────────

    /// <summary>Buat save baru yang bersih (kondisi awal New Game).</summary>
    public static SaveData CreateDefault()
    {
        return new SaveData
        {
            saveVersion       = 1,
            savedAtIso        = "",
            storyStep         = 0,
            sceneName         = "Dunia",
            hasPlayerPosition = false,
            money             = 0,
            dayCount          = 1,
            timeOfDay         = 0f,
            inventory         = new List<ItemStack>(),
            plots             = new List<PlotSave>(),
            unlockedRecipes   = new List<string>(),
            completedSteps    = new List<string>(),
            questCounters     = new SerializableDictionary(),
            larasLovePoints   = 0
        };
    }

    // ─────────────────────────────────────────────────────────────
    // HELPER — Tutorial / Step
    // ─────────────────────────────────────────────────────────────

    public bool IsStepDone(string id) => completedSteps.Contains(id);

    public void MarkStepDone(string id)
    {
        if (!completedSteps.Contains(id)) completedSteps.Add(id);
    }

    // ─────────────────────────────────────────────────────────────
    // HELPER — Resep
    // ─────────────────────────────────────────────────────────────

    public bool IsRecipeUnlocked(string id) => unlockedRecipes.Contains(id);

    public void UnlockRecipe(string id)
    {
        if (!unlockedRecipes.Contains(id)) unlockedRecipes.Add(id);
    }
}

/// <summary>Satu jenis item + jumlahnya di inventory.</summary>
[Serializable]
public class ItemStack
{
    public string itemName;
    public int    amount;

    public ItemStack() { }
    public ItemStack(string name, int amt) { itemName = name; amount = amt; }
}

/// <summary>State satu petak kebun.</summary>
[Serializable]
public class PlotSave
{
    public string plotId;       // nama GameObject petak (mis. "Plot_01")
    public int    state;        // (int) GardenPlot.PlotState
    public string plantName;    // nama tanaman yang sedang tumbuh ("" kalau kosong)
    public float  timer;        // progress tumbuh (detik)
}


/// <summary>Dictionary string→int yang bisa diserialisasi JSON (untuk quest counters).</summary>
[Serializable]
public class SerializableDictionary : ISerializationCallbackReceiver
{
    [SerializeField] private List<string> keys = new List<string>();
    [SerializeField] private List<int> values = new List<int>();

    private Dictionary<string, int> dict = new Dictionary<string, int>();

    public int this[string key]
    {
        get => dict.ContainsKey(key) ? dict[key] : 0;
        set => dict[key] = value;
    }

    public bool ContainsKey(string key) => dict.ContainsKey(key);

    public Dictionary<string, int>.Enumerator GetEnumerator() => dict.GetEnumerator();

    // Tambah property Key & Value agar bisa di-foreach
    public string Key { get; private set; }
    public int Value { get; private set; }

    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (var kv in dict)
        {
            keys.Add(kv.Key);
            values.Add(kv.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        dict = new Dictionary<string, int>();
        for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
            dict[keys[i]] = values[i];
    }
}
