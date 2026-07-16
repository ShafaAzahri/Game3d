using UnityEngine;

/// <summary>
/// Panel Cheat/Developer untuk testing chapter tanpa mulai dari awal.
/// Muncul otomatis saat game berjalan.
/// Tekan F10 atau klik tombol kecil "Cheat [F10]" di kiri atas untuk membuka.
/// </summary>
public class QuestTestPanel : MonoBehaviour
{
    private bool showPanel = false;
    private Rect windowRect = new Rect(20, 20, 280, 520);
    private Vector2 scrollPos = Vector2.zero;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        // Instansiasi otomatis saat scene di-load (tanpa perlu drag ke hierarchy)
        GameObject go = new GameObject("QuestTestPanel");
        go.AddComponent<QuestTestPanel>();
        DontDestroyOnLoad(go);
    }

    void Update()
    {
        // Tekan F10 untuk toggle cheat panel
        if (Input.GetKeyDown(KeyCode.F10))
        {
            showPanel = !showPanel;
            if (!showPanel)
            {
                RestoreCursor();
            }
        }
    }

    private void RestoreCursor()
    {
        bool otherUIActive = (DialogManager.Instance != null && DialogManager.Instance.IsDialogActive)
                             || (ChoiceDialogUI.Instance != null && ChoiceDialogUI.Instance.choicePanel != null && ChoiceDialogUI.Instance.choicePanel.activeSelf)
                             || CookingTrigger.IsAnyOpen;
        
        if (!otherUIActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnGUI()
    {
        if (!showPanel) return;

        // Tombol kecil toggle di kiri atas agar tidak mengganggu UI utama
        if (GUI.Button(new Rect(10, 10, 120, 25), "Close Cheat (F10)"))
        {
            showPanel = false;
            RestoreCursor();
        }

        // Unlock cursor saat panel terbuka agar mudah diklik
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        windowRect = GUI.Window(999, windowRect, DrawWindow, "DEV CHEAT PANEL");
    }

    void DrawWindow(int windowID)
    {
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("=== JUMP CHAPTER ===", GUILayout.ExpandWidth(true));

        if (GUILayout.Button("Prolog: Gerak WASD (Step 0)")) JumpToStep(0);
        if (GUILayout.Button("Prolog: Bicara Nenek (Step 1)")) JumpToStep(1);
        if (GUILayout.Button("Prolog: Cangkul Kebun (Step 2)")) JumpToStep(2);
        if (GUILayout.Button("Prolog: Masak Jamu (Step 6)")) JumpToStep(6);
        if (GUILayout.Button("Ch 1: Bicara Laras (Step 8)")) JumpToStep(8);
        if (GUILayout.Button("Ch 1: Temui Pak Darma (Step 9)")) JumpToStep(9);
        if (GUILayout.Button("Ch 1: Masak Pegal Linu (Step 12)")) JumpToStep(12);
        if (GUILayout.Button("Ch 2: Kabar Ratri (Step 14)")) JumpToStep(14);
        if (GUILayout.Button("Ch 2: Temui Ratri Hutan (Step 16)")) JumpToStep(16);
        if (GUILayout.Button("Ch 3: Temui Sekar Pantai (Step 21)")) JumpToStep(21);
        if (GUILayout.Button("Ch 3: Temui Pak Bahri (Step 22)")) JumpToStep(22);
        if (GUILayout.Button("Ch 4: Temui Kades (Step 27)")) JumpToStep(27);
        if (GUILayout.Button("Ch 5: Nasihat Nenek (Step 30)")) JumpToStep(30);
        if (GUILayout.Button("Ch 5: Kencan Laras (Step 31)")) JumpToStep(31);
        if (GUILayout.Button("Ch 6: Melamar Laras (Step 32)")) JumpToStep(32);
        if (GUILayout.Button("Ch 7: Mode Bebas (Step 33)")) JumpToStep(33);

        GUILayout.Space(10);
        GUILayout.Label("=== LARAS LOVE POINTS ===", GUILayout.ExpandWidth(true));
        int currentLove = (GameManager.Instance != null) ? GameManager.Instance.Data.larasLovePoints : 0;
        GUILayout.Label($"Poin Hati Laras: {currentLove}/100");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Set 0"))
        {
            if (GameManager.Instance != null) { GameManager.Instance.Data.larasLovePoints = 0; GameManager.Instance.SaveGame(); }
        }
        if (GUILayout.Button("Set 95"))
        {
            if (GameManager.Instance != null) { GameManager.Instance.Data.larasLovePoints = 95; GameManager.Instance.SaveGame(); }
        }
        if (GUILayout.Button("Set 100"))
        {
            if (GameManager.Instance != null) { GameManager.Instance.Data.larasLovePoints = 100; GameManager.Instance.SaveGame(); }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("=== UTILITY CHEATS ===", GUILayout.ExpandWidth(true));

        if (GUILayout.Button("+1,000 Gold"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Data.money += 1000;
                GameManager.Instance.SaveGame();
                if (GoldUI.Instance != null) GoldUI.Instance.Refresh();
                Debug.Log("[Cheat] +1000 Gold added.");
            }
        }

        if (GUILayout.Button("Unlock Semua Resep"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Data.UnlockRecipe("level1");
                GameManager.Instance.Data.UnlockRecipe("level2");
                GameManager.Instance.Data.UnlockRecipe("level3");
                GameManager.Instance.SaveGame();
                Debug.Log("[Cheat] All recipes unlocked.");
            }
        }

        if (GUILayout.Button("Dapatkan Semua Item (+5)"))
        {
            var items = Resources.FindObjectsOfTypeAll<InventoryItem>();
            int count = 0;
            foreach (var item in items)
            {
                if (item != null && !string.IsNullOrEmpty(item.itemName))
                {
                    InventoryManager.Instance.AddItem(item, 5);
                    count++;
                }
            }
            if (GameManager.Instance != null) GameManager.Instance.SaveGame();
            Debug.Log($"[Cheat] Added 5 of each of the {count} inventory items found.");
        }

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }

    private void JumpToStep(int targetStep)
    {
        // Force close dialogue if running to prevent player freeze
        if (DialogManager.Instance != null && DialogManager.Instance.IsDialogActive)
        {
            DialogManager.Instance.ForceEndDialog();
        }

        // Force close choice panel if open
        if (ChoiceDialogUI.Instance != null && ChoiceDialogUI.Instance.choicePanel != null)
        {
            ChoiceDialogUI.Instance.choicePanel.SetActive(false);
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.DebugSetStep(targetStep);
        }
        else
        {
            Debug.LogWarning("[Cheat] QuestManager tidak ditemukan di scene aktif!");
        }

        // Close panel and lock cursor back
        showPanel = false;
        RestoreCursor();
    }
}
