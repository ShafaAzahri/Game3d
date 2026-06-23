using UnityEngine;
using TMPro;

/// <summary>
/// Penggerak progres cerita berbasis storyStep (tersimpan di SaveData).
/// Menampilkan objektif aktif, mengarahkan marker, dan memberi reward.
///
/// PROLOG : move → talk → plant → recipe
/// CHAPTER 1 : tanam Jahe → tanam Kunyit → panen → masak jamu → REWARD
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [System.Serializable]
    public class Objective
    {
        [Tooltip("Kunci pemicu: move, talk, plant, recipe, plantNamed, harvest, cook")]
        public string id;
        [Tooltip("Parameter opsional. Untuk 'talk' = npcId (mis. 'Nenek','Laras','Nisa'). " +
                 "Untuk plant/harvest/cook = nama tanaman/resep (mis. 'Jahe','Kunyit Asam').")]
        public string param;
        [TextArea] public string text;
        [Tooltip("Target marker '!' untuk objektif ini (opsional).")]
        public Transform marker;

        [Header("Reward saat objektif ini SELESAI (opsional)")]
        [Tooltip("Resep yang dibuka. Kosongkan kalau tidak ada.")]
        public string rewardRecipe;
        [Tooltip("GameObject yang diaktifkan sebagai reward (mis. area baru). Opsional.")]
        public GameObject rewardUnlockObject;
        [Tooltip("Pesan popup reward. Kalau kosong, popup tidak tampil.")]
        [TextArea] public string rewardMessage;
        [Tooltip("Judul popup reward (mis. 'REWARD CHAPTER 2').")]
        public string rewardTitle;
    }

    [Header("Daftar Objektif (urut)")]
    public Objective[] objectives = new Objective[]
    {
        // ── PROLOG ──
        new Objective { id = "move",       text = "Gunakan W A S D untuk bergerak" },
        new Objective { id = "talk", param = "Nenek", text = "Temui Nenek Rukmini — dekati lalu tekan [G]" },
        new Objective { id = "plant",      text = "Tanam tanaman pertamamu ([F] cangkul, [H] pilih bibit, [F] siram)" },
        new Objective { id = "recipe",     text = "Buka buku resep di tungku — tekan [G]" },

        // ── CHAPTER 1: Belajar Meracik Jamu ──
        new Objective { id = "plantNamed", param = "Jahe",   text = "Chapter 1: Tanam JAHE di kebun" },
        new Objective { id = "plantNamed", param = "Kunyit", text = "Chapter 1: Tanam KUNYIT di kebun" },
        new Objective { id = "harvest",    text = "Chapter 1: Panen tanaman pertamamu" },
        new Objective { id = "cook",       text = "Chapter 1: Buat jamu pertama bersama Nenek di tungku",
                        rewardRecipe = "level1",
                        rewardMessage = "Buku Resep Lv.1 terbuka \u2022 Inventory aktif \u2022 Kebun terbuka!",
                        rewardTitle = "REWARD CHAPTER 1" },

        // ── CHAPTER 2: Jamu untuk Warga Pertama (Laras, Nisa, Pak Darma) ──
        new Objective { id = "talk", param = "Laras", text = "Chapter 2: Temui Laras di peternakan" },
        new Objective { id = "talk", param = "Nisa",  text = "Chapter 2: Belanja bahan ke toko Nisa (gula aren, botol)" },
        new Objective { id = "cook", param = "Kunyit Asam", text = "Chapter 2: Racik Jamu Kunyit Asam untuk Pak Darma" },
        new Objective { id = "talk", param = "Darma", text = "Chapter 2: Antar jamu & sembuhkan Pak Darma",
                        rewardRecipe = "level2",
                        rewardMessage = "Buku Resep Lv.2 \u2022 Area Peternakan & Toko terbuka \u2022 Upgrade kandang!",
                        rewardTitle = "REWARD CHAPTER 2" },

        // ── CHAPTER 3: Menyembuhkan Desa (Sekar, Bahri, Ratri, Kepala Desa) ──
        new Objective { id = "talk", param = "Sekar", text = "Chapter 3: Temui Sekar — terima quest penyembuhan desa" },
        new Objective { id = "talk", param = "Bahri", text = "Chapter 3: Sembuhkan Pak Bahri (buka area sungai)" },
        new Objective { id = "talk", param = "Ratri", text = "Chapter 3: Temui Ratri (buka area hutan & bahan langka)" },
        new Objective { id = "cook", param = "Spesial", text = "Chapter 3: Racik Jamu Pemulihan Spesial" },
        new Objective { id = "talk", param = "Darsono", text = "Chapter 3: Sembuhkan Kepala Desa Darsono",
                        rewardRecipe = "level3",
                        rewardMessage = "Buku Resep Lv.3 \u2022 Seluruh desa pulih \u2022 Robby kini Tabib Desa!",
                        rewardTitle = "TAMAT \u2022 DESA PULIH" },
    };

    [Header("UI Objektif")]
    public GameObject objectivePanel;
    public TMP_Text   objectiveText;
    public TMP_Text   subText;        // teks jarak meter di bawah garis
    public string     completeText = "Chapter 1 selesai! Kebun nenek kini hidup kembali.";

    [Header("Marker")]
    public QuestMarker questMarker;

    [Header("Referensi")]
    public NPCDialog nenek;

    [Header("Reward (saat masak jamu pertama selesai)")]
    [Tooltip("Resep yang dibuka sebagai reward (nama bebas / id).")]
    public string[] rewardRecipes = new string[] { "level1" };
    [Tooltip("GameObject inventory yang diaktifkan sebagai reward (opsional).")]
    public GameObject unlockInventoryObject;
    [Tooltip("GameObject area kebun yang dibuka sebagai reward (opsional).")]
    public GameObject unlockGardenObject;
    public string rewardMessage = "Buku Resep Lv.1 terbuka • Inventory aktif • Kebun terbuka!";

    [Header("Deteksi Gerak")]
    public float moveDurationNeeded = 1.2f;

    private int   step;
    private float moveTimer;

    // ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        GardenPlot.OnAnyPlanted    += HandlePlanted;
        GardenPlot.OnAnyHarvested  += HandleHarvested;
        CookingTrigger.OnAnyOpened += HandleRecipeOpened;
        CookingUI.OnAnyCooked      += HandleCooked;
    }

    private void OnDisable()
    {
        GardenPlot.OnAnyPlanted    -= HandlePlanted;
        GardenPlot.OnAnyHarvested  -= HandleHarvested;
        CookingTrigger.OnAnyOpened -= HandleRecipeOpened;
        CookingUI.OnAnyCooked      -= HandleCooked;
    }

    private void Start()
    {
        Instance = this;
        step = (GameManager.Instance != null) ? GameManager.Instance.Data.storyStep : 0;
        RefreshUI();
    }

    private void Update()
    {
        string id = CurrentId();
        if (id == "move")
        {
            float m = Mathf.Abs(Input.GetAxisRaw("Horizontal")) + Mathf.Abs(Input.GetAxisRaw("Vertical"));
            if (m > 0.1f) moveTimer += Time.deltaTime;
            if (moveTimer >= moveDurationNeeded) Advance("move");
        }
        // Objektif 'talk' kini ditangani lewat NotifyTalked() yang dipanggil NPCDialog.

        // Update jarak ke target aktif
        UpdateDistanceText();
    }

    private void UpdateDistanceText()
    {
        if (subText == null) return;
        bool done = step >= objectives.Length;
        if (done) { subText.text = ""; return; }

        var target = objectives[step].marker;
        if (target == null) { subText.text = ""; return; }

        var player = GameObject.FindWithTag("Player");
        if (player == null) { subText.text = ""; return; }

        float dist = Vector3.Distance(player.transform.position, target.position);
        subText.text = dist < 1f ? "< 1m" : Mathf.RoundToInt(dist) + "m";
    }

    // ── Event handlers ──
    private void HandlePlanted(string plantName)
    {
        string id = CurrentId();
        if (id == "plant") Advance("plant");
        else if (id == "plantNamed" && Matches(plantName)) Advance("plantNamed");
    }

    private void HandleHarvested(string itemName)
    {
        if (CurrentId() == "harvest" && Matches(itemName)) Advance("harvest");
    }

    private void HandleRecipeOpened()
    {
        if (CurrentId() == "recipe") Advance("recipe");
    }

    private void HandleCooked(string recipeName)
    {
        if (CurrentId() == "cook" && Matches(recipeName)) Advance("cook");
    }

    /// <summary>
    /// Dipanggil NPCDialog saat pemain selesai bicara dengan sebuah NPC.
    /// Objektif 'talk' akan maju kalau param-nya cocok dengan npcId (atau param kosong).
    /// </summary>
    public void NotifyTalked(string npcId)
    {
        if (CurrentId() == "talk" && Matches(npcId)) Advance("talk");
    }

    /// <summary>Cocokkan param objektif aktif (kosong = terima apa saja).</summary>
    private bool Matches(string value)
    {
        if (step < 0 || step >= objectives.Length) return false;
        string p = objectives[step].param;
        if (string.IsNullOrEmpty(p)) return true;
        return !string.IsNullOrEmpty(value) &&
               value.Trim().ToLower().Contains(p.Trim().ToLower());
    }

    // ─────────────────────────────────────────────────────────────

    private string CurrentId()
        => (step >= 0 && step < objectives.Length) ? objectives[step].id : "";

    public void Advance(string id)
    {
        if (CurrentId() != id) return;

        Objective completed = objectives[step];
        step++;
        moveTimer = 0f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Data.storyStep = step;
            GameManager.Instance.SaveGame();
        }

        Debug.Log($"[QuestManager] Objektif '{completed.id}' ({completed.param}) selesai → step {step}.");

        // Reward per-objektif (dipakai semua chapter)
        GrantObjectiveReward(completed);

        RefreshUI();
    }

    /// <summary>
    /// Berikan reward yang menempel pada satu objektif (resep, unlock object, popup).
    /// Juga mengaktifkan inventory/kebun legacy saat objektif Chapter 1 (cook tanpa param) selesai.
    /// </summary>
    private void GrantObjectiveReward(Objective completed)
    {
        bool gaveSomething = false;

        // Unlock resep dari objektif
        if (!string.IsNullOrEmpty(completed.rewardRecipe) && GameManager.Instance != null)
        {
            GameManager.Instance.Data.UnlockRecipe(completed.rewardRecipe);
            gaveSomething = true;
        }

        // Aktifkan object reward dari objektif
        if (completed.rewardUnlockObject != null)
        {
            completed.rewardUnlockObject.SetActive(true);
            gaveSomething = true;
        }

        // Legacy Chapter 1: objektif 'cook' tanpa param membuka inventory & kebun
        if (completed.id == "cook" && string.IsNullOrEmpty(completed.param))
        {
            if (rewardRecipes != null && GameManager.Instance != null)
                foreach (var r in rewardRecipes)
                    GameManager.Instance.Data.UnlockRecipe(r);
            if (unlockInventoryObject != null) unlockInventoryObject.SetActive(true);
            if (unlockGardenObject    != null) unlockGardenObject.SetActive(true);
            gaveSomething = true;
        }

        // Popup reward
        if (!string.IsNullOrEmpty(completed.rewardMessage) && RewardPopup.Instance != null)
        {
            string title = string.IsNullOrEmpty(completed.rewardTitle) ? "REWARD" : completed.rewardTitle;
            RewardPopup.Instance.Show(completed.rewardMessage, title);
            gaveSomething = true;
        }

        if (gaveSomething && GameManager.Instance != null) GameManager.Instance.SaveGame();
    }

    private void RefreshUI()
    {
        bool done = step >= objectives.Length;

        if (objectivePanel != null) objectivePanel.SetActive(true);

        if (objectiveText != null)
            objectiveText.text = done ? completeText : objectives[step].text;

        // Reset subtext saat step baru (update jarak via Update())
        if (subText != null && done) subText.text = "";

        // Marker mengikuti objektif aktif
        if (questMarker != null)
            questMarker.target = done ? null : objectives[step].marker;

        if (done) Invoke(nameof(HidePanel), 5f);
    }

    private void HidePanel()
    {
        if (objectivePanel != null) objectivePanel.SetActive(false);
    }
}
