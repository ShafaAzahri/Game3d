using System.Collections;
using UnityEngine;

/// <summary>
/// Pasang di setiap petak kebun (Plot_01, Plot_02, dst.)
/// Butuh BoxCollider dengan isTrigger = true.
///
/// STATE MACHINE:
///   Empty → (F) → Hoed → (F + punya bibit) → Planted → (F) → Watered → [timer] → Ready → (F) → Panen → Hoed
///
/// SETUP DI INSPECTOR:
/// 1. Drag PlantData yang kamu buat
/// 2. Pastikan BoxCollider isTrigger = true
/// 3. Terrain dan layer "tanah" otomatis dicari
///
/// TOMBOL: F untuk semua aksi kebun
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class GardenPlot : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // ENUM
    // ─────────────────────────────────────────────

    public enum PlotState { Empty, Hoed, Planted, Watered, Ready }

    // ─────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Tanaman yang Bisa Ditanam")]
    [Tooltip("Daftar tanaman yang bisa ditanam di petak ini. Player bisa pilih saat menanam.")]
    public PlantData[] availablePlants;

    [Header("Terrain Paint")]
    [Tooltip("Radius (meter) area terrain yang dicat saat mencangkul")]
    public float paintRadius = 1.8f;

    [Tooltip("Nama layer terrain untuk tanah. Harus cocok (case-insensitive) dengan nama TerrainLayer asset.")]
    public string soilLayerName = "tanah";

    [Header("Pertumbuhan")]
    [Tooltip("Override waktu tumbuh (detik). Isi 0 untuk pakai dari PlantData.")]
    public float growthTimeOverride = 0f;

    [Header("Status (Read Only)")]
    [SerializeField] private PlotState currentState = PlotState.Empty;
    [SerializeField] private float     growTimer    = 0f;

    // ─────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────

    private bool        playerInRange   = false;
    private PlantData   currentPlant;      // tanaman yang sedang tumbuh
    private GameObject  spawnedPlant;      // prefab 3D yang sudah di-spawn
    private Terrain     terrain;
    private int         soilLayerIndex  = -1;
    private bool        terrainCached   = false;
    private bool        waitingForSeed  = false; // sedang di UI pilih bibit

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        // Pastikan BoxCollider isTrigger
        var col = GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;

        // Cache terrain
        CacheTerrain();

        // Load state yang tersimpan
        LoadState();
    }

    void Update()
    {
        // ── Timer tumbuh: selalu jalan, TIDAK tergantung player di dekat plot ──
        if (currentState == PlotState.Watered)
        {
            growTimer += Time.deltaTime;
            float growTime = GetGrowthTime();
            if (growTimer >= growTime)
            {
                growTimer = growTime;
                SetState(PlotState.Ready);
            }
        }

        // ── Interaksi: hanya saat player ada di dalam plot ──
        if (!playerInRange) return;
        if (DialogManager.Instance != null && DialogManager.Instance.IsDialogActive) return;
        if (waitingForSeed) return;

        UpdatePrompt();

        // F = cangkul / siram / panen
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (currentState != PlotState.Hoed)
                HandleInteraction();
        }

        // H = buka popup pilih bibit (saat sudah dicangkul)
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (currentState == PlotState.Hoed)
                HandleInteraction();
        }
    }


    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        UpdatePrompt();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (FarmingPromptUI.Instance != null) FarmingPromptUI.Instance.Hide();
    }

    // ─────────────────────────────────────────────
    // INTERACTION
    // ─────────────────────────────────────────────

    private void HandleInteraction()
    {
        switch (currentState)
        {
            case PlotState.Empty:
                Hoe();
                break;

            case PlotState.Hoed:
                TryPlant();
                break;

            case PlotState.Planted:
                Water();
                break;

            case PlotState.Watered:
                // Sedang tumbuh — tidak ada aksi
                break;

            case PlotState.Ready:
                Harvest();
                break;
        }
    }

    // ─────────────────────────────────────────────
    // ACTIONS
    // ─────────────────────────────────────────────

    private void Hoe()
    {
        SetState(PlotState.Hoed);
        PaintSoilTexture();
        Debug.Log($"[GardenPlot] {name}: Tanah dicangkul!");
    }

    private void TryPlant()
    {
        if (availablePlants == null || availablePlants.Length == 0)
        {
            Debug.LogWarning($"[GardenPlot] {name}: Tidak ada PlantData di array 'Available Plants'!");
            FarmingPromptUI.Instance?.Show("Tidak ada tanaman yang bisa ditanam di sini.");
            return;
        }

        // Buka UI pilih bibit
        if (SeedSelectionUI.Instance == null)
        {
            Debug.LogWarning("[GardenPlot] SeedSelectionUI tidak ditemukan di scene!");
            return;
        }

        waitingForSeed = true;
        FarmingPromptUI.Instance?.Hide();

        var options = new System.Collections.Generic.List<PlantData>(availablePlants);
        SeedSelectionUI.Instance.Show(options, OnSeedChosen);
    }

    /// <summary>Dipanggil SeedSelectionUI setelah player memilih bibit.</summary>
    private void OnSeedChosen(PlantData chosen)
    {
        waitingForSeed = false;

        if (chosen == null) return; // Player batal (ESC)

        // Cek dan konsumsi bibit
        if (chosen.seedItem != null)
        {
            if (InventoryManager.Instance == null || !InventoryManager.Instance.HasItem(chosen.seedItem.itemName))
            {
                FarmingPromptUI.Instance?.Show($"Tidak punya bibit {chosen.seedItem.itemName}!");
                return;
            }
            InventoryManager.Instance.RemoveItem(chosen.seedItem.itemName, 1);
        }

        currentPlant = chosen;
        SetState(PlotState.Planted);
        Debug.Log($"[GardenPlot] {name}: {chosen.plantName} ditanam!");
    }

    private void Water()
    {
        growTimer = 0f;
        SetState(PlotState.Watered);
        Debug.Log($"[GardenPlot] {name}: Tanaman disiram. Tumbuh dalam {GetGrowthTime()}s...");
    }

    private void Harvest()
    {
        if (currentPlant != null && currentPlant.harvestItem != null && InventoryManager.Instance != null)
        {
            string itemName = currentPlant.harvestItem.itemName;
            int    amount   = currentPlant.harvestAmount;
            InventoryManager.Instance.AddItem(currentPlant.harvestItem, amount);
            int total = InventoryManager.Instance.GetAmount(itemName);
            Debug.Log($"[PANEN] {amount}x {itemName} masuk inventory! Total: {total}x — Tekan B untuk lihat.");
        }
        else
        {
            Debug.LogWarning($"[PANEN] Gagal — plant:{currentPlant?.plantName ?? "null"} " +
                $"item:{currentPlant?.harvestItem?.itemName ?? "null"} " +
                $"inv:{InventoryManager.Instance != null}");
        }

        if (spawnedPlant != null) { Destroy(spawnedPlant); spawnedPlant = null; }
        currentPlant = null;
        growTimer    = 0f;
        SetState(PlotState.Hoed);
    }


    // ─────────────────────────────────────────────
    // STATE
    // ─────────────────────────────────────────────

    private void SetState(PlotState newState)
    {
        currentState = newState;

        // Spawn prefab tanaman saat Ready
        if (newState == PlotState.Ready && currentPlant != null && currentPlant.grownPrefab != null)
        {
            if (spawnedPlant != null) Destroy(spawnedPlant);
            Vector3 spawnPos = transform.position + Vector3.up * currentPlant.spawnHeight;
            spawnedPlant = Instantiate(currentPlant.grownPrefab, spawnPos, Quaternion.identity, transform);
        }

        // Simpan state setiap kali berubah
        SaveState();

        // Update prompt jika player masih di range
        if (playerInRange) UpdatePrompt();
    }

    // ─────────────────────────────────────────────
    // PROMPT
    // ─────────────────────────────────────────────

    private void UpdatePrompt()
    {
        if (FarmingPromptUI.Instance == null) return;

        switch (currentState)
        {
            case PlotState.Empty:
                FarmingPromptUI.Instance.Show("[F] Cangkul");
                break;

            case PlotState.Hoed:
                if (waitingForSeed)
                    FarmingPromptUI.Instance?.Hide();
                else
                    FarmingPromptUI.Instance?.Show("[H] Pilih & Tanam Bibit");
                break;

            case PlotState.Planted:
                FarmingPromptUI.Instance.Show("[F] Siram");
                break;

            case PlotState.Watered:
                float pct = Mathf.Clamp01(growTimer / GetGrowthTime()) * 100f;
                FarmingPromptUI.Instance.Show($"Tumbuh... {pct:F0}%");
                break;

            case PlotState.Ready:
                FarmingPromptUI.Instance.Show($"[F] Panen {currentPlant?.plantName ?? "Tanaman"}!");
                break;
        }
    }

    // ─────────────────────────────────────────────
    // TERRAIN PAINT
    // ─────────────────────────────────────────────

    private void PaintSoilTexture()
    {
        if (!terrainCached) CacheTerrain();
        if (terrain == null || soilLayerIndex < 0)
        {
            Debug.LogWarning("[GardenPlot] Terrain / layer tanah tidak ditemukan. Skip paint.");
            return;
        }

        TerrainData td = terrain.terrainData;

        // Konversi posisi world ke koordinat alphamap
        Vector3 relPos  = transform.position - terrain.transform.position;
        float   normX   = relPos.x / td.size.x;
        float   normZ   = relPos.z / td.size.z;

        int alphaW = td.alphamapWidth;
        int alphaH = td.alphamapHeight;

        int   centerX  = Mathf.RoundToInt(normX * alphaW);
        int   centerZ  = Mathf.RoundToInt(normZ * alphaH);
        int   radius   = Mathf.RoundToInt(paintRadius / td.size.x * alphaW);

        int   startX   = Mathf.Clamp(centerX - radius, 0, alphaW - 1);
        int   startZ   = Mathf.Clamp(centerZ - radius, 0, alphaH - 1);
        int   width    = Mathf.Clamp(radius * 2, 1, alphaW - startX);
        int   height   = Mathf.Clamp(radius * 2, 1, alphaH - startZ);

        float[,,] alphas     = td.GetAlphamaps(startX, startZ, width, height);
        int        layerCount = alphas.GetLength(2);

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                // Hitung jarak dari center (lingkaran)
                float dx   = (startX + x) - centerX;
                float dz   = (startZ + z) - centerZ;
                if (dx * dx + dz * dz > radius * radius) continue;

                for (int l = 0; l < layerCount; l++)
                    alphas[z, x, l] = (l == soilLayerIndex) ? 1f : 0f;
            }
        }

        td.SetAlphamaps(startX, startZ, alphas);
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    private void CacheTerrain()
    {
        terrain = Terrain.activeTerrain;
        if (terrain == null) { terrainCached = true; return; }

        TerrainData td = terrain.terrainData;
        soilLayerIndex = -1;

        for (int i = 0; i < td.terrainLayers.Length; i++)
        {
            if (td.terrainLayers[i] != null &&
                td.terrainLayers[i].name.ToLower().Contains(soilLayerName.ToLower()))
            {
                soilLayerIndex = i;
                Debug.Log($"[GardenPlot] Layer '{soilLayerName}' ditemukan di index {i}.");
                break;
            }
        }

        if (soilLayerIndex < 0)
            Debug.LogWarning($"[GardenPlot] Layer terrain '{soilLayerName}' tidak ditemukan! Pastikan nama TerrainLayer asset mengandung '{soilLayerName}'.");

        terrainCached = true;
    }

    private float GetGrowthTime()
    {
        if (growthTimeOverride > 0f) return growthTimeOverride;
        return currentPlant?.growthTimeSeconds ?? 60f;
    }

    // ─────────────────────────────────────────────
    // SAVE / LOAD (PlayerPrefs)
    // ─────────────────────────────────────────────

    private string SaveKey => $"GardenPlot_{gameObject.name}";

    private void SaveState()
    {
        PlayerPrefs.SetInt   (SaveKey + "_state", (int)currentState);
        PlayerPrefs.SetString(SaveKey + "_plant", currentPlant?.plantName ?? "");
        PlayerPrefs.SetFloat (SaveKey + "_timer", growTimer);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        if (!PlayerPrefs.HasKey(SaveKey + "_state")) return; // belum pernah disimpan

        int    savedStateInt = PlayerPrefs.GetInt   (SaveKey + "_state", 0);
        string savedPlant   = PlayerPrefs.GetString (SaveKey + "_plant", "");
        float  savedTimer   = PlayerPrefs.GetFloat  (SaveKey + "_timer", 0f);

        // Restore tanaman dari nama
        if (!string.IsNullOrEmpty(savedPlant) && availablePlants != null)
        {
            foreach (var pd in availablePlants)
            {
                if (pd != null && pd.plantName == savedPlant)
                {
                    currentPlant = pd;
                    break;
                }
            }
        }

        growTimer    = savedTimer;
        currentState = (PlotState)savedStateInt;

        // Repaint tanah jika sudah dicangkul
        if (currentState >= PlotState.Hoed)
            PaintSoilTexture();

        // Spawn prefab jika sudah siap panen
        if (currentState == PlotState.Ready && currentPlant?.grownPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * currentPlant.spawnHeight;
            spawnedPlant = Instantiate(currentPlant.grownPrefab, spawnPos, Quaternion.identity, transform);
        }

        Debug.Log($"[GardenPlot] {name}: State dimuat → {currentState} (plant: {savedPlant}, timer: {savedTimer:F1}s)");
    }

    /// <summary>Hapus data save plot ini (dipanggil saat panen reset).</summary>
    private void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey + "_state");
        PlayerPrefs.DeleteKey(SaveKey + "_plant");
        PlayerPrefs.DeleteKey(SaveKey + "_timer");
        PlayerPrefs.Save();
    }

    void OnApplicationQuit() => SaveState();

    // ─────────────────────────────────────────────
    // GIZMOS (Editor)
    // ─────────────────────────────────────────────

    void OnDrawGizmos()
    {
        Color c = currentState switch
        {
            PlotState.Empty   => new Color(0.6f, 0.4f, 0.2f, 0.3f),
            PlotState.Hoed    => new Color(0.4f, 0.2f, 0.1f, 0.5f),
            PlotState.Planted => new Color(0.5f, 0.8f, 0.3f, 0.4f),
            PlotState.Watered => new Color(0.2f, 0.5f, 1.0f, 0.4f),
            PlotState.Ready   => new Color(1.0f, 0.8f, 0.0f, 0.5f),
            _                 => Color.white
        };
        Gizmos.color = c;

        var col = GetComponent<BoxCollider>();
        if (col != null)
            Gizmos.DrawCube(transform.position + col.center, col.size);
    }
}
