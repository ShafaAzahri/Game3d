using UnityEngine;
using UnityEditor;

public class QuestTesterWindow : EditorWindow
{
    private Vector2 scrollPos;

    [MenuItem("Window/Quest Tester")]
    public static void ShowWindow()
    {
        GetWindow<QuestTesterWindow>("Quest Tester");
    }

    private void OnGUI()
    {
        GUILayout.Label("Quest Tester & Debug Panel", EditorStyles.boldLabel);
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Silakan jalankan Play Mode di Unity Editor untuk menggunakan tool ini.", MessageType.Info);
            return;
        }

        if (QuestManager.Instance == null)
        {
            EditorGUILayout.HelpBox("QuestManager tidak ditemukan di scene aktif.", MessageType.Warning);
            return;
        }

        int currentStep = QuestManager.Instance.CurrentStep;
        GUILayout.Label($"Quest Step Saat Ini: {currentStep}", EditorStyles.boldLabel);
        if (currentStep < QuestManager.Instance.objectives.Length)
        {
            GUILayout.Label($"Teks Objektif: \"{QuestManager.Instance.objectives[currentStep].text}\"");
        }
        else
        {
            GUILayout.Label("Status: Mode Bebas (Freeplay / Game Selesai)");
        }

        EditorGUILayout.Space();

        GUILayout.Label("Lompat ke Step Spesifik (Semua Objektif):", EditorStyles.boldLabel);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
        for (int i = 0; i < QuestManager.Instance.objectives.Length; i++)
        {
            var obj = QuestManager.Instance.objectives[i];
            GUILayout.BeginHorizontal();
            
            if (i == currentStep)
            {
                GUI.color = Color.green;
                GUILayout.Label($"[STEP {i}] {obj.text}", GUILayout.Width(position.width - 90));
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Label($"[Step {i}] {obj.text}", GUILayout.Width(position.width - 90));
            }

            if (GUILayout.Button("Lompat", GUILayout.Width(60)))
            {
                JumpTo(i);
            }
            GUILayout.EndHorizontal();
        }
        
        // Mode Bebas (Endless)
        GUILayout.BeginHorizontal();
        if (currentStep >= QuestManager.Instance.objectives.Length)
        {
            GUI.color = Color.green;
            GUILayout.Label("[FREEPLAY] Mode Bebas / Endless", GUILayout.Width(position.width - 90));
            GUI.color = Color.white;
        }
        else
        {
            GUILayout.Label("[Freeplay] Mode Bebas / Endless", GUILayout.Width(position.width - 90));
        }
        if (GUILayout.Button("Lompat", GUILayout.Width(60)))
        {
            JumpTo(QuestManager.Instance.objectives.Length);
        }
        GUILayout.EndHorizontal();
        
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        GUILayout.Label("Lompat Cepat ke Fase Cerita:", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("0. Prolog (Awal)")) JumpTo(0);
        if (GUILayout.Button("1. Bicara Laras (Ch 1)")) JumpTo(8);
        if (GUILayout.Button("2. Temui Pak Darma (Ch 1)")) JumpTo(9);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("3. Kabar Ratri (Ch 2)")) JumpTo(14);
        if (GUILayout.Button("4. Pergi ke Hutan (Ch 2)")) JumpTo(15);
        if (GUILayout.Button("5. Temui Ratri (Ch 2)")) JumpTo(16);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("6. Kabar Bahri (Ch 3)")) JumpTo(19);
        if (GUILayout.Button("7. Pergi ke Pantai (Ch 3)")) JumpTo(20);
        if (GUILayout.Button("8. Temui Sekar (Ch 3)")) JumpTo(21);
        if (GUILayout.Button("9. Temui Pak Bahri (Ch 3)")) JumpTo(22);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("10. Kabar Darsono (Ch 4)")) JumpTo(25);
        if (GUILayout.Button("11. Balai Desa (Ch 4)")) JumpTo(26);
        if (GUILayout.Button("12. Temui Kades (Ch 4)")) JumpTo(27);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("13. Nasihat Nenek (Ch 5)")) JumpTo(30);
        if (GUILayout.Button("14. Kencan Laras (Ch 5)")) JumpTo(31);
        if (GUILayout.Button("15. Melamar (Ch 6)")) JumpTo(32);
        if (GUILayout.Button("16. Freeplay (Ch 7)")) JumpTo(33);
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        GUILayout.Label("Cheat & Bantuan Hubungan Laras:", EditorStyles.boldLabel);
        
        int currentLove = (GameManager.Instance != null) ? GameManager.Instance.Data.larasLovePoints : 0;
        GUILayout.Label($"Poin Hati Laras: {currentLove}/100");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Hati Laras = 0"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Data.larasLovePoints = 0;
                GameManager.Instance.SaveGame();
                Debug.Log("[Cheat] Poin hati Laras di-set ke 0.");
            }
        }
        if (GUILayout.Button("Set Hati Laras = 95"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Data.larasLovePoints = 95;
                GameManager.Instance.SaveGame();
                Debug.Log("[Cheat] Poin hati Laras di-set ke 95 (tinggal ajak bicara sekali ke Merah).");
            }
        }
        if (GUILayout.Button("Set Hati Laras = 100"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Data.larasLovePoints = 100;
                GameManager.Instance.SaveGame();
                Debug.Log("[Cheat] Poin hati Laras di-set ke 100.");
            }
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        GUILayout.Label("Cheat Ekonomi & Inventory:", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+500 G"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Data.money += 500;
                GameManager.Instance.SaveGame();
                if (GoldUI.Instance != null) GoldUI.Instance.Refresh();
                Debug.Log("[Cheat] +500 Gold ditambahkan.");
            }
        }
        if (GUILayout.Button("+1 Bulu Biru"))
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem("Bulu Biru", 1);
                Debug.Log("[Cheat] 1 Bulu Biru ditambahkan ke inventory.");
            }
        }
        if (GUILayout.Button("+5 Pakan Ternak"))
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem("Pakan Ternak", 5);
                Debug.Log("[Cheat] 5 Pakan Ternak ditambahkan ke inventory.");
            }
        }
        if (GUILayout.Button("+1 Jamu Jahe"))
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem("Jamu Jahe", 1);
                Debug.Log("[Cheat] 1 Jamu Jahe ditambahkan ke inventory.");
            }
        }
        GUILayout.EndHorizontal();
    }

    private void JumpTo(int step)
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.DebugSetStep(step);
            Debug.Log($"[QuestTester] Meloncat ke Quest Step {step}.");
        }
    }
}
