using UnityEngine;

/// <summary>
/// ScriptableObject data untuk tiap jenis tanaman.
/// Buat via: klik kanan di Project → Create → Herbal Haven → Plant Data
///
/// CARA PAKAI:
/// 1. Buat PlantData asset untuk tiap tanaman (Jahe, Kunyit, dll.)
/// 2. Buat InventoryItem asset untuk bibit dan hasil panennya
/// 3. Drag referensi ke PlantData
/// 4. Assign PlantData ke GardenPlot di Inspector
/// </summary>
[CreateAssetMenu(fileName = "New Plant", menuName = "Herbal Haven/Plant Data")]
public class PlantData : ScriptableObject
{
    [Header("Identitas")]
    public string plantName = "Tanaman";

    [Header("Item")]
    [Tooltip("Item bibit yang dikonsumsi saat menanam")]
    public InventoryItem seedItem;

    [Tooltip("Item yang didapat saat panen")]
    public InventoryItem harvestItem;

    [Tooltip("Jumlah item hasil panen")]
    [Range(1, 10)]
    public int harvestAmount = 1;

    [Header("Pertumbuhan")]
    [Tooltip("Waktu tumbuh dalam detik (60 = 1 menit)")]
    public float growthTimeSeconds = 60f;

    [Header("Visual (opsional)")]
    [Tooltip("Prefab 3D model tanaman yang muncul saat Ready. Kosongkan jika tidak punya.")]
    public GameObject grownPrefab;

    [Tooltip("Tinggi spawn prefab di atas tanah")]
    public float spawnHeight = 0.1f;
}
