using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Quest Manager v2 — Support linear quest, counter quest (paralel), dan branching.
///
/// PROLOG  : move → cangkul kebun → talk Nenek (dapat bibit) → openBag → openRecipe
///           → cook Jamu Jahe → giveItem Nenek → PROLOG SELESAI
/// CH 1   : talk Laras → talk Darma → openRecipe → talk Nisa → cook Pegal Linu
///           → giveItem Darma → CH1 SELESAI
/// CH 2   : (paralel) sembuhkan Ratri + Bahri + Darsono (counter 0/3) → CH2 SELESAI
/// CH 3   : talk Ratri/Laras/Nisa → pilih pacar → TAMAT
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────
    // OBJECTIVE DEFINITION
    // ─────────────────────────────────────────────────────────────

    public enum ObjType
    {
        Move,           // Gerak WASD
        Hoe,            // Cangkul plot kebun
        Talk,           // Bicara NPC (param = npcId)
        OpenBag,        // Tekan B buka tas
        OpenRecipe,     // Tekan Tab buka resep
        Plant,          // Tanam (param = nama tanaman, kosong = apa saja)
        Harvest,        // Panen
        Cook,           // Masak (param = nama resep)
        GiveItem,       // Serahkan item ke NPC (param = npcId, itemNeeded = nama item)
        Counter,        // Paralel: selesaikan N sub-goal (param = counterId)
        Choice,         // Branching choice (param = choiceGroup)
    }

    [System.Serializable]
    public class Objective
    {
        public ObjType type;
        [Tooltip("Parameter utama: npcId / nama resep / counterId / choiceGroup")]
        public string param;
        [Tooltip("Untuk GiveItem: nama item yang harus ada di inventory")]
        public string itemNeeded;
        [TextArea] public string text;
        [Tooltip("Target marker navigasi (opsional)")]
        public Transform marker;

        [Header("Reward (opsional)")]
        public string rewardRecipe;
        public GameObject rewardUnlockObject;
        [TextArea] public string rewardMessage;
        public string rewardTitle;
        [Tooltip("Gold yang diberikan saat objektif ini selesai. 0 = tidak ada.")]
        public int rewardGold;
    }

    [Header("Daftar Objektif (urut)")]
    public Objective[] objectives;

    [Header("UI")]
    public GameObject objectivePanel;
    public TMP_Text objectiveText;
    public TMP_Text subText;
    [Tooltip("Garis kuning di bawah teks objektif")]
    public RectTransform lineRect;
    [Tooltip("RectTransform dari SubText (jarak meter)")]
    public RectTransform subTextRect;
    [Tooltip("Jarak (pixel) antara bawah teks objektif dan garis kuning")]
    public float lineGap = 4f;

    [Header("Chapter Title")]
    public ChapterTitleUI chapterTitleUI;

    [Header("Marker")]
    public QuestMarker questMarker;

    [Header("Chapter Boundaries (step index pertama tiap chapter)")]
    [Tooltip("Step index dimulainya Chapter 1, 2, 3. Dipakai untuk title card.")]
    public int ch1Start = 7;
    public int ch2Start = 13;
    public int ch3Start = 16;

    [Header("Counter Quest (Chapter 2)")]
    [Tooltip("Berapa pasien yang harus disembuhkan untuk menyelesaikan counter 'heal3'")]
    public int healCounterTarget = 3;

    [Header("Deteksi Gerak")]
    public float moveDurationNeeded = 1.2f;

    [Header("Legacy Reward Refs")]
    public string[] rewardRecipes;
    public GameObject unlockInventoryObject;
    public GameObject unlockGardenObject;

    // State
    private int step;
    public int CurrentStep => step;
    private float moveTimer;
    private bool panelShouldShow = true;
    private Dictionary<string, int> counters = new Dictionary<string, int>();

    // ─────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        GardenPlot.OnAnyPlanted   += HandlePlanted;
        GardenPlot.OnAnyHarvested += HandleHarvested;
        CookingTrigger.OnAnyOpened += HandleRecipeBookOpened;
        CookingUI.OnAnyCooked     += HandleCooked;
    }

    private void OnDisable()
    {
        GardenPlot.OnAnyPlanted   -= HandlePlanted;
        GardenPlot.OnAnyHarvested -= HandleHarvested;
        CookingTrigger.OnAnyOpened -= HandleRecipeBookOpened;
        CookingUI.OnAnyCooked     -= HandleCooked;
    }

    private void Start()
    {
        Instance = this;
        
        InitializeQuestObjectives();

        step = (GameManager.Instance != null) ? GameManager.Instance.Data.storyStep : 0;

        // Restore counters dari save
        if (GameManager.Instance != null)
        {
            foreach (var kv in GameManager.Instance.Data.questCounters)
                counters[kv.Key] = kv.Value;
        }

        // Siapkan NPC untuk step saat ini (restore dari save)
        PrepareNextStep();
        RefreshUI();
    }

    private void Update()
    {
        if (step >= objectives.Length) return;

        var obj = objectives[step];

        switch (obj.type)
        {
            case ObjType.Move:
                float m = Mathf.Abs(Input.GetAxisRaw("Horizontal")) + Mathf.Abs(Input.GetAxisRaw("Vertical"));
                if (m > 0.1f) moveTimer += Time.deltaTime;
                if (moveTimer >= moveDurationNeeded) Advance();
                break;

            case ObjType.OpenBag:
                if (Input.GetKeyDown(KeyCode.B)) Advance();
                break;

            case ObjType.OpenRecipe:
                if (Input.GetKeyDown(KeyCode.Tab)) Advance();
                break;
        }

        UpdatePanelVisibility();
        UpdateDistanceText();
    }

    // ─────────────────────────────────────────────────────────────
    // EVENT HANDLERS
    // ─────────────────────────────────────────────────────────────

    private void HandlePlanted(string plantName)
    {
        if (step >= objectives.Length) return;
        var obj = objectives[step];
        if (obj.type == ObjType.Hoe) Advance();
        else if (obj.type == ObjType.Plant && Matches(plantName)) Advance();
    }

    private void HandleHarvested(string itemName)
    {
        if (step >= objectives.Length) return;
        if (objectives[step].type == ObjType.Harvest) Advance();
    }

    private void HandleRecipeBookOpened()
    {
        // OpenRecipe juga bisa ter-trigger oleh event ini (selain Tab key)
        if (step >= objectives.Length) return;
        if (objectives[step].type == ObjType.OpenRecipe) Advance();
    }

    private void HandleCooked(string recipeName)
    {
        if (step >= objectives.Length) return;
        var obj = objectives[step];
        if (obj.type == ObjType.Cook && Matches(recipeName)) Advance();
    }

    /// <summary>Dipanggil NPCDialog saat selesai bicara. Cek Talk atau GiveItem.</summary>
    public void NotifyTalked(string npcId)
    {
        if (step >= objectives.Length) return;
        var obj = objectives[step];

        if (obj.type == ObjType.Talk && Matches(npcId))
        {
            Advance();
        }
        else if (obj.type == ObjType.GiveItem && Matches(npcId))
        {
            // Cek apakah punya item di inventory
            if (!string.IsNullOrEmpty(obj.itemNeeded) && InventoryManager.Instance != null)
            {
                if (InventoryManager.Instance.HasItem(obj.itemNeeded, 1))
                {
                    InventoryManager.Instance.RemoveItem(obj.itemNeeded, 1);
                    Advance();
                }
                else
                {
                    Debug.Log($"[QuestManager] Belum punya '{obj.itemNeeded}' untuk diserahkan ke '{npcId}'.");
                }
            }
            else
            {
                Advance(); // kalau itemNeeded kosong, langsung lanjut
            }
        }
        else if (obj.type == ObjType.Counter)
        {
            // Cek apakah npcId ini termasuk counter target (Chapter 2 heal)
            NotifyCounterProgress(obj.param, npcId);
        }
    }

    /// <summary>Dipanggil ChoiceDialogUI saat player memilih.</summary>
    public void NotifyChoice(string choiceGroup, string chosenOption)
    {
        if (step >= objectives.Length) return;
        var obj = objectives[step];
        if (obj.type == ObjType.Choice && obj.param == choiceGroup)
        {
            // Simpan pilihan ke save data
            if (GameManager.Instance != null)
                GameManager.Instance.Data.MarkStepDone("choice_" + choiceGroup + "_" + chosenOption);
            
            Advance();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // COUNTER (PARALEL QUEST)
    // ─────────────────────────────────────────────────────────────

    /// <summary>Naikkan counter dan cek apakah sudah tercapai target.</summary>
    public void NotifyCounterProgress(string counterId, string subGoalId)
    {
        if (step >= objectives.Length) return;
        var obj = objectives[step];
        if (obj.type != ObjType.Counter || obj.param != counterId) return;

        // Cek sudah pernah dihitung belum (jangan double-count)
        string key = counterId + "_" + subGoalId;
        if (GameManager.Instance != null && GameManager.Instance.Data.IsStepDone(key)) return;

        // Catat
        if (GameManager.Instance != null) GameManager.Instance.Data.MarkStepDone(key);

        if (!counters.ContainsKey(counterId)) counters[counterId] = 0;
        counters[counterId]++;

        // Simpan counter ke save
        if (GameManager.Instance != null)
            GameManager.Instance.Data.questCounters[counterId] = counters[counterId];

        Debug.Log($"[QuestManager] Counter '{counterId}' = {counters[counterId]}/{healCounterTarget}");

        RefreshUI(); // update teks "2/3"

        if (counters[counterId] >= healCounterTarget)
            Advance();
    }

    /// <summary>Untuk NPC pasien: serahkan item lalu hitung ke counter.</summary>
    public void NotifyHealPatient(string npcId, string itemNeeded)
    {
        if (step >= objectives.Length) return;
        var obj = objectives[step];

        // Delegasikan ke NotifyTalked jika step saat ini adalah Talk atau GiveItem
        if (obj.type == ObjType.Talk || obj.type == ObjType.GiveItem)
        {
            NotifyTalked(npcId);
            return;
        }

        if (obj.type != ObjType.Counter) return;

        // Cek item
        if (!string.IsNullOrEmpty(itemNeeded) && InventoryManager.Instance != null)
        {
            if (!InventoryManager.Instance.HasItem(itemNeeded, 1))
            {
                Debug.Log($"[QuestManager] Belum punya '{itemNeeded}' untuk menyembuhkan '{npcId}'.");
                return;
            }
            InventoryManager.Instance.RemoveItem(itemNeeded, 1);
        }

        NotifyCounterProgress(obj.param, npcId);
    }

    // ─────────────────────────────────────────────────────────────
    // ADVANCE & REWARD
    // ─────────────────────────────────────────────────────────────

    public void Advance()
    {
        if (step >= objectives.Length) return;

        Objective completed = objectives[step];
        int prevStep = step;
        step++;
        moveTimer = 0f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Data.storyStep = step;
            GameManager.Instance.SaveGame();
        }

        Debug.Log($"[QuestManager] Objektif '{completed.type}' ({completed.param}) selesai → step {step}.");

        GrantObjectiveReward(completed);
        ShowChapterTransition(prevStep, step);

        // Prepare NPC untuk step berikutnya (mis. ganti dialog Nenek)
        PrepareNextStep();

        RefreshUI();
    }

    public void DebugSetStep(int newStep)
    {
        if (newStep < 0 || newStep > objectives.Length) return;
        
        int prevStep = step;
        step = newStep;
        moveTimer = 0f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Data.storyStep = step;
            GameManager.Instance.SaveGame();
        }

        // Reset all dialogue flags on all NPCs in scene to ensure they adapt to new step
        var npcs = Object.FindObjectsByType<NPCDialog>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            npc.HasTalked = false;
            npc.canTalkAgain = true;
        }

        PrepareNextStep();
        RefreshUI();
        Debug.Log($"[QuestManager] Debug jump to step {step}.");
    }

    private void GrantObjectiveReward(Objective completed)
    {
        bool gaveSomething = false;

        if (!string.IsNullOrEmpty(completed.rewardRecipe) && GameManager.Instance != null)
        {
            GameManager.Instance.Data.UnlockRecipe(completed.rewardRecipe);
            gaveSomething = true;
        }

        if (completed.rewardUnlockObject != null)
        {
            completed.rewardUnlockObject.SetActive(true);
            gaveSomething = true;
        }

        // Gold reward
        if (completed.rewardGold > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.Data.money += completed.rewardGold;
            gaveSomething = true;
            Debug.Log($"[QuestManager] +{completed.rewardGold}G → total {GameManager.Instance.Data.money}G");
        }

        if (!string.IsNullOrEmpty(completed.rewardMessage) && RewardPopup.Instance != null)
        {
            string title = string.IsNullOrEmpty(completed.rewardTitle) ? "REWARD" : completed.rewardTitle;
            RewardPopup.Instance.Show(completed.rewardMessage, title);
            gaveSomething = true;
        }

        if (gaveSomething && GameManager.Instance != null) GameManager.Instance.SaveGame();

        // Update gold UI
        if (GoldUI.Instance != null) GoldUI.Instance.Refresh();
    }

    private void ShowChapterTransition(int fromStep, int toStep)
    {
        if (chapterTitleUI == null) return;

        if (fromStep < 8 && toStep >= 8)
            chapterTitleUI.Show("Prolog Selesai", "Chapter 1: Laras & Pak Darma");
        else if (fromStep < 14 && toStep >= 14)
            chapterTitleUI.Show("Chapter 1 Selesai", "Chapter 2: Penyelamatan Ratri");
        else if (fromStep < 19 && toStep >= 19)
            chapterTitleUI.Show("Chapter 2 Selesai", "Chapter 3: Nelayan Pantai");
        else if (fromStep < 25 && toStep >= 25)
            chapterTitleUI.Show("Chapter 3 Selesai", "Chapter 4: Kepala Desa");
        else if (fromStep < 30 && toStep >= 30)
            chapterTitleUI.Show("Chapter 4 Selesai", "Chapter 5: Melamar Laras");
        else if (toStep >= objectives.Length)
            chapterTitleUI.Show("Kisah Cinta Selesai", "Mode Bebas (Freeplay) Dimulai!");
    }

    // ─────────────────────────────────────────────────────────────
    // UI
    // ─────────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        bool done = step >= objectives.Length;
        panelShouldShow = true;

        if (objectivePanel != null) objectivePanel.SetActive(true);

        if (objectiveText != null)
        {
            if (done)
                objectiveText.text = "Semua quest selesai! Selamat!";
            else
            {
                objectiveText.text = objectives[step].text;
            }
        }

        if (subText != null && done) subText.text = "";

        // Cari marker secara dinamis saat refresh jika belum ada
        if (!done && objectives[step].marker == null)
        {
            objectives[step].marker = FindMarker(objectives[step].type, objectives[step].param);
        }

        if (questMarker != null)
            questMarker.target = done ? null : objectives[step].marker;

        if (done) Invoke(nameof(HidePanel), 5f);
    }

    private void UpdatePanelVisibility()
    {
        if (objectivePanel == null) return;
        bool busy = (DialogManager.Instance != null && DialogManager.Instance.IsDialogActive)
                    || CookingTrigger.IsAnyOpen;
        objectivePanel.SetActive(panelShouldShow && !busy);
    }

    private void LateUpdate()
    {
        AdjustLinePosition();
        UpdateDistanceText();
    }

    private void AdjustLinePosition()
    {
        if (objectiveText == null || lineRect == null) return;

        float textHeight = objectiveText.preferredHeight;

        var textRect = objectiveText.rectTransform;
        float textY = textRect.anchoredPosition.y;

        float lineY = textY - textHeight - lineGap;
        lineRect.anchoredPosition = new Vector2(lineRect.anchoredPosition.x, lineY);

        if (subTextRect != null)
            subTextRect.anchoredPosition = new Vector2(subTextRect.anchoredPosition.x, lineY - 4f);
    }

    private void UpdateDistanceText()
    {
        if (subText == null) return;
        if (step >= objectives.Length) { subText.text = ""; return; }

        var target = objectives[step].marker;
        if (target == null) { subText.text = ""; return; }

        var player = GameObject.FindWithTag("Player");
        if (player == null) { subText.text = ""; return; }

        float dist = Vector3.Distance(player.transform.position, target.position);
        subText.text = dist < 1f ? "< 1m" : Mathf.RoundToInt(dist) + "m";
    }

    private void HidePanel()
    {
        panelShouldShow = false;
        if (objectivePanel != null) objectivePanel.SetActive(false);
    }

    private void PrepareNextStep()
    {
        if (step >= objectives.Length) return;
        var obj = objectives[step];

        // Hanya perlu prepare kalau step ini butuh bicara ke NPC
        if (obj.type != ObjType.Talk && obj.type != ObjType.GiveItem) return;

        string targetNpcId = obj.param;
        if (string.IsNullOrEmpty(targetNpcId)) return;

        var npcs = Object.FindObjectsByType<NPCDialog>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            if (npc.npcId != targetNpcId) continue;

            if (npc.HasTalked)
            {
                npc.HasTalked = false;
                npc.canTalkAgain = false;
            }
            break;
        }
    }

    private void InitializeQuestObjectives()
    {
        var list = new List<Objective>();

        // ─────────────────────────────────────────────────────────────
        // PROLOG: Belajar Meracik Jamu
        // ─────────────────────────────────────────────────────────────
        list.Add(new Objective { type = ObjType.Talk, param = "Nenek", text = "Bicara dengan Nenek Rukmini." });
        list.Add(new Objective { type = ObjType.Move, text = "Gerakkan karaktermu dengan WASD." });
        list.Add(new Objective { type = ObjType.Hoe, text = "Cangkul plot tanah di kebun." });
        list.Add(new Objective { type = ObjType.Talk, param = "Nenek", text = "Bicara dengan Nenek Rukmini." });
        list.Add(new Objective { type = ObjType.OpenBag, text = "Buka tas penyimpananmu dengan menekan tombol [B]." });
        list.Add(new Objective { type = ObjType.OpenRecipe, text = "Buka buku resep jamumu dengan menekan tombol [Tab]." });
        list.Add(new Objective { type = ObjType.Cook, param = "Jamu Jahe", text = "Masak Jamu Jahe di tungku masak kebun." });
        list.Add(new Objective { 
            type = ObjType.GiveItem, 
            param = "Nenek", 
            itemNeeded = "Jamu Jahe", 
            text = "Berikan Jamu Jamu Jahe ke Nenek Rukmini.",
            rewardGold = 100,
            rewardRecipe = "level1",
            rewardMessage = "Prolog Selesai! Kamu sekarang paham dasar pembuatan Jamu. Dapatkan +100 Gold!"
        });

        // ─────────────────────────────────────────────────────────────
        // CHAPTER 1: Laras & Pak Darma
        // ─────────────────────────────────────────────────────────────
        list.Add(new Objective { type = ObjType.Talk, param = "Laras", text = "Bicara dengan Laras di peternakan." });
        list.Add(new Objective { type = ObjType.Talk, param = "Darma", text = "Temui Pak Darma di rumahnya." });
        list.Add(new Objective { type = ObjType.OpenRecipe, text = "Buka buku resep [Tab] untuk mempelajari Jamu Pegal Linu." });
        list.Add(new Objective { type = ObjType.Talk, param = "Nisa", text = "Beli Bibit Temulawak dari Nisa." });
        list.Add(new Objective { type = ObjType.Cook, param = "Jamu Pegal Linu", text = "Tanam temulawak, panen, lalu masak Jamu Pegal Linu." });
        list.Add(new Objective { 
            type = ObjType.GiveItem, 
            param = "Darma", 
            itemNeeded = "Jamu Pegal Linu", 
            text = "Berikan Jamu Pegal Linu ke Pak Darma.",
            rewardGold = 150,
            rewardRecipe = "level2",
            rewardMessage = "Chapter 1 Selesai! Pak Darma telah sembuh. Resep Jamu Level 2 terbuka!"
        });

        // ─────────────────────────────────────────────────────────────
        // CHAPTER 2: Ratri di Hutan
        // ─────────────────────────────────────────────────────────────
        list.Add(new Objective { type = ObjType.Talk, param = "Laras", text = "Bicara dengan Laras di peternakan." });
        list.Add(new Objective { type = ObjType.Move, param = "Hutan", text = "Pergi ke area Hutan perbatasan desa." });
        list.Add(new Objective { type = ObjType.Talk, param = "Ratri", text = "Bicara dengan Ratri di Hutan." });
        list.Add(new Objective { type = ObjType.Cook, param = "Ramuan Penurun Panas", text = "Masak Ramuan Penurun Panas di tungku." });
        list.Add(new Objective { 
            type = ObjType.GiveItem, 
            param = "Ratri", 
            itemNeeded = "Ramuan Penurun Panas", 
            text = "Serahkan Ramuan Penurun Panas ke Ratri.",
            rewardGold = 200,
            rewardMessage = "Chapter 2 Selesai! Ratri telah sembuh dari demamnya. Dapatkan +200 Gold!"
        });

        // ─────────────────────────────────────────────────────────────
        // CHAPTER 3: Pak Bahri di Pantai
        // ─────────────────────────────────────────────────────────────
        list.Add(new Objective { type = ObjType.Talk, param = "Ratri", text = "Bicara dengan Ratri di Hutan." });
        list.Add(new Objective { type = ObjType.Move, param = "Pantai", text = "Pergi ke area Pantai desa." });
        list.Add(new Objective { type = ObjType.Talk, param = "Istri Nelayan", text = "Tanyakan kondisi Pak Bahri ke istrinya (Sekar) di Pantai." });
        list.Add(new Objective { type = ObjType.Talk, param = "Bahri", text = "Temui Pak Bahri di gubuknya." });
        list.Add(new Objective { type = ObjType.Cook, param = "Ramuan Anti Mual", text = "Masak Ramuan Anti Mual di tungku." });
        list.Add(new Objective { 
            type = ObjType.GiveItem, 
            param = "Bahri", 
            itemNeeded = "Ramuan Anti Mual", 
            text = "Serahkan Ramuan Anti Mual ke Pak Bahri.",
            rewardGold = 250,
            rewardMessage = "Chapter 3 Selesai! Pak Bahri telah sembuh dari mual-mualnya. Dapatkan +250 Gold!"
        });

        // ─────────────────────────────────────────────────────────────
        // CHAPTER 4: Kepala Desa (Pak Darsono)
        // ─────────────────────────────────────────────────────────────
        list.Add(new Objective { type = ObjType.Talk, param = "Bahri", text = "Bicara dengan Pak Bahri di Pantai." });
        list.Add(new Objective { type = ObjType.Move, param = "Balai Desa", text = "Pergi ke Balai Desa." });
        list.Add(new Objective { type = ObjType.Talk, param = "Darsono", text = "Bicara dengan Kepala Desa (Pak Darsono) di Balai Desa." });
        list.Add(new Objective { type = ObjType.Cook, param = "Jamu Sehat Desa", text = "Masak Jamu Sehat Desa di tungku." });
        list.Add(new Objective { 
            type = ObjType.GiveItem, 
            param = "Darsono", 
            itemNeeded = "Jamu Sehat Desa", 
            text = "Serahkan Jamu Sehat Desa ke Pak Darsono.",
            rewardGold = 300,
            rewardRecipe = "level3",
            rewardMessage = "Chapter 4 Selesai! Pak Kades telah sembuh. Resep Jamu Level 3 terbuka!"
        });

        // ─────────────────────────────────────────────────────────────
        // CHAPTER 5: Pendekatan Laras (Love Meter)
        // ─────────────────────────────────────────────────────────────
        list.Add(new Objective { type = ObjType.Talk, param = "Nenek", text = "Bicara dengan Nenek Rukmini tentang masa depanmu." });
        list.Add(new Objective { type = ObjType.Talk, param = "Laras", text = "Dekati Laras (Gunakan hadiah Pakan Ternak atau Jamu untuk menaikkan hatinya ke warna Merah)." });

        // ─────────────────────────────────────────────────────────────
        // CHAPTER 6 & 7: Bulu Biru & Lamaran Pernikahan
        // ─────────────────────────────────────────────────────────────
        list.Add(new Objective { 
            type = ObjType.GiveItem, 
            param = "Laras", 
            itemNeeded = "Bulu Biru", 
            text = "Lamar Laras dengan memberikan Bulu Biru di Peternakan (Beli di Toko Nisa seharga 500 G).",
            rewardTitle = "CONGRATULATIONS",
            rewardMessage = "Selamat! Kamu telah resmi melamar Laras. Mode Bebas (Freeplay) dan kehidupan rumah tangga baru saja dimulai!"
        });

        objectives = list.ToArray();

        // Cari marker transform secara dinamis
        foreach (var obj in objectives)
        {
            obj.marker = FindMarker(obj.type, obj.param);
        }

        ch1Start = 8;
        ch2Start = 14;
        ch3Start = 19;
    }

    private Transform FindMarker(ObjType type, string param)
    {
        if (type == ObjType.Talk || type == ObjType.GiveItem)
        {
            return FindNpcTransform(param);
        }
        
        if (param == "Hutan") return FindNpcTransform("Ratri");
        if (param == "Pantai") return FindNpcTransform("Istri Nelayan") ?? FindNpcTransform("Bahri");
        if (param == "Balai Desa") return FindNpcTransform("Darsono");
        if (param == "Kebun") return FindNpcTransform("Nenek");
        if (param == "Toko Nisa") return FindNpcTransform("Nisa");
        
        return null;
    }

    private Transform FindNpcTransform(string npcId)
    {
        var npcs = Object.FindObjectsByType<NPCDialog>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            if (npc.npcId == npcId) return npc.transform;
        }
        return null;
    }

    private bool Matches(string value)
    {
        if (step < 0 || step >= objectives.Length) return false;
        string p = objectives[step].param;
        if (string.IsNullOrEmpty(p)) return true;
        return !string.IsNullOrEmpty(value) &&
               value.Trim().ToLower().Contains(p.Trim().ToLower());
    }
}
