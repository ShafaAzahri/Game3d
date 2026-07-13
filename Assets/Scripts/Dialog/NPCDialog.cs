using UnityEngine;

/// <summary>
/// Pasang di GameObject NPC (Nenek MC, dll.).
/// Tag NPC harus sudah diset di Inspector.
///
/// Cara pakai:
/// - Tambah DialogLine baru di array "dialogLines"
/// - Set speakerName, subtitle, text, isPlayerLine
/// - Susun urutan NPC → MC → NPC dst.
/// - Tekan G untuk mulai dan lanjut dialog
/// </summary>
public class NPCDialog : MonoBehaviour
{
    [Header("Identitas NPC")]
    [Tooltip("ID NPC untuk dicocokkan oleh QuestManager pada objektif 'talk'. " +
             "Contoh: 'Nenek', 'Laras', 'Nisa', 'Sekar', 'Bahri', 'Ratri', 'Darma', 'Darsono'.")]
    public string npcId = "Nenek";

    [Header("Healing Item (Chapter 2 — Counter Quest)")]
    [Tooltip("Item jamu yang harus diserahkan player saat bicara untuk menyembuhkan NPC ini (kosongkan jika bukan pasien).")]
    public string healItemNeeded;

    [Header("Dialog Content")]
    [Tooltip("Daftar baris dialog UTAMA (stage 0). Gunakan isPlayerLine=true untuk baris MC/Player.")]
    public DialogLine[] dialogLines = new DialogLine[] { };

    [Header("Multi-Stage Dialog")]
    [Tooltip("Dialog tambahan per-stage (stage 1, 2, dst). Dipakai saat QuestManager set stage NPC ini.")]
    public DialogLine[][] extraStages;

    /// <summary>Stage dialog aktif. 0 = dialogLines utama, 1+ = dari extraStages.</summary>
    [HideInInspector] public int dialogStage = 0;

    /// <summary>Set oleh QuestManager: apakah NPC ini boleh diajak bicara lagi walaupun HasTalked.</summary>
    [HideInInspector] public bool canTalkAgain = false;

    [Header("Interaction Settings")]
    [Tooltip("Radius interaksi dalam meter")]
    public float interactRadius = 3f;

    [Header("Portrait Siluet (Visual Novel)")]
    [Tooltip("Siluet NPC ini (tampil di KANAN saat NPC TIDAK sedang bicara).")]
    public Sprite npcSilhouette;
    [Tooltip("Siluet Player/MC (tampil di KIRI saat MC TIDAK sedang bicara).")]
    public Sprite playerSilhouette;

    [Header("Post-Dialog Shop (opsional)")]
    [Tooltip("Kalau diisi, setelah dialog selesai muncul pilihan 'Lihat Toko' / 'Tinggalkan'.")]
    public ShopUI shopUI;

    private bool playerInRange = false;
    private Transform playerTransform;

    /// <summary>True setelah pemain selesai ngobrol dengan NPC ini minimal sekali.</summary>
    public bool HasTalked { get; set; }

    /// <summary>Dipanggil sekali saat dialog NPC ini pertama kali selesai.</summary>
    public event System.Action OnTalked;

    private void HandleDialogComplete()
    {
        bool cured = false;
        if (!string.IsNullOrEmpty(healItemNeeded) && InventoryManager.Instance != null)
        {
            if (InventoryManager.Instance.HasItem(healItemNeeded, 1))
            {
                cured = true;
            }
        }

        bool isFreeplay = QuestManager.Instance != null && QuestManager.Instance.CurrentStep >= QuestManager.Instance.objectives.Length;

        if (isFreeplay)
        {
            HasTalked = false;
            canTalkAgain = true;
        }
        else if (cured || string.IsNullOrEmpty(healItemNeeded))
        {
            // Cek apakah ini quest serah item (GiveItem) yang belum terpenuhi
            bool isPendingGiveItem = false;
            if (QuestManager.Instance != null)
            {
                int currentStep = QuestManager.Instance.CurrentStep;
                if (currentStep < QuestManager.Instance.objectives.Length)
                {
                    var obj = QuestManager.Instance.objectives[currentStep];
                    if (obj.type == QuestManager.ObjType.GiveItem && obj.param == npcId)
                    {
                        if (InventoryManager.Instance != null && !string.IsNullOrEmpty(obj.itemNeeded))
                        {
                            if (!InventoryManager.Instance.HasItem(obj.itemNeeded, 1))
                            {
                                isPendingGiveItem = true;
                            }
                        }
                    }
                }
            }

            if (isPendingGiveItem)
            {
                HasTalked = false;
                canTalkAgain = true;
            }
            else if (!HasTalked)
            {
                HasTalked = true;
                OnTalked?.Invoke();
            }
        }
        else
        {
            // Belum sembuh: biarkan bisa diajak bicara lagi
            HasTalked = false;
            canTalkAgain = true;
        }

        // Lapor ke QuestManager
        if (QuestManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(healItemNeeded))
                QuestManager.Instance.NotifyHealPatient(npcId, healItemNeeded);
            else
                QuestManager.Instance.NotifyTalked(npcId);
        }

        // Post-dialog: tawarin toko kalau ada shopUI
        if (shopUI != null && ChoiceDialogUI.Instance != null)
        {
            string[] options = new string[] { "Beli Barang", "Jual Barang", "Tinggalkan" };
            ChoiceDialogUI.Instance.Show("Pilih transaksi untuk Toko:", options, (chosen) => {
                if (chosen == "Beli Barang")
                {
                    shopUI.isSellingMode = false;
                    shopUI.Open();
                }
                else if (chosen == "Jual Barang")
                {
                    shopUI.isSellingMode = true;
                    shopUI.Open();
                }
            });
        }
    }

    void Start()
    {
        CachePlayer();
    }

    void Update()
    {
        if (playerTransform == null) { CachePlayer(); return; }

        float dist   = Vector3.Distance(transform.position, playerTransform.position);
        bool  isNear = dist <= interactRadius;

        if (isNear != playerInRange)
        {
            playerInRange = isNear;
            UpdatePrompt();
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.G))
        {
            // Intercept jika ini adalah quest kencan Laras (step 31)
            if (QuestManager.Instance != null && QuestManager.Instance.CurrentStep == 31 && npcId == "Laras")
            {
                if (DialogManager.Instance != null && !DialogManager.Instance.IsDialogActive)
                {
                    ShowLarasGiftingChoice();
                }
                return;
            }

            bool isFreeplay = QuestManager.Instance != null && QuestManager.Instance.CurrentStep >= QuestManager.Instance.objectives.Length;

            bool canStart = (!HasTalked || canTalkAgain || isFreeplay)
                            && DialogManager.Instance != null
                            && !DialogManager.Instance.IsDialogActive
                            && Time.frameCount > DialogManager.Instance.LastEndFrame;

            if (canStart)
            {
                // Reset canTalkAgain SEBELUM mulai dialog — jangan bisa trigger lagi
                if (!isFreeplay)
                    canTalkAgain = false;

                DialogManager.Instance.SetPortraitSilhouettes(playerSilhouette, npcSilhouette);
                DialogManager.Instance.StartDialog(GetCurrentDialogLines(), HandleDialogComplete);
            }
        }
    }

    private void ShowLarasGiftingChoice()
    {
        if (ChoiceDialogUI.Instance == null) return;
        
        string[] options = new string[] {
            "Ajak Ngobrol",
            "Beri Pakan Ternak (Butuh 1 Pakan)",
            "Beri Jamu Jahe (Butuh 1 Jamu Jahe)",
            "Tinggalkan"
        };

        ChoiceDialogUI.Instance.Show("Pilih interaksi dengan Laras:", options, (chosen) => {
            if (chosen.StartsWith("Ajak Ngobrol"))
            {
                AddLovePoints(5, "Terima kasih sudah menemaniku mengobrol, Robby. Aku merasa senang sekali!");
            }
            else if (chosen.StartsWith("Beri Pakan Ternak"))
            {
                if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem("Pakan Ternak", 1))
                {
                    InventoryManager.Instance.RemoveItem("Pakan Ternak", 1);
                    AddLovePoints(20, "Wah, Pakan Ternak! Ini sangat membantu sapi-sapiku. Terima kasih banyak ya, Robby!");
                }
                else
                {
                    ShowLarasShortDialog("Laras", "Kamu tidak memiliki Pakan Ternak di inventory.");
                }
            }
            else if (chosen.StartsWith("Beri Jamu Jahe"))
            {
                if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem("Jamu Jahe", 1))
                {
                    InventoryManager.Instance.RemoveItem("Jamu Jahe", 1);
                    AddLovePoints(25, "Jamu Jahe buatanmu hangat sekali, Robby! Lelahku langsung hilang.");
                }
                else
                {
                    ShowLarasShortDialog("Laras", "Kamu tidak memiliki Jamu Jahe di inventory.");
                }
            }
        });
    }

    private void AddLovePoints(int points, string feedbackText)
    {
        if (GameManager.Instance == null) return;
        
        GameManager.Instance.Data.larasLovePoints = Mathf.Min(GameManager.Instance.Data.larasLovePoints + points, 100);
        GameManager.Instance.SaveGame();

        int currentPoints = GameManager.Instance.Data.larasLovePoints;
        string heartEmoji = GetLarasHeartEmoji();

        if (currentPoints >= 100)
        {
            DialogLine[] loveFullLines = new DialogLine[] {
                new DialogLine { speakerName = "Laras " + heartEmoji, subtitle = "Peternak Desa", text = feedbackText, isPlayerLine = false },
                new DialogLine { speakerName = "Laras " + heartEmoji, subtitle = "Peternak Desa", text = "Robby... terima kasih atas segala perhatianmu. Aku merasa kamu adalah orang yang paling berharga untukku.", isPlayerLine = false },
                new DialogLine { speakerName = "Robby", subtitle = "", text = "Laras...", isPlayerLine = true }
            };
            DialogManager.Instance.SetPortraitSilhouettes(playerSilhouette, npcSilhouette);
            DialogManager.Instance.StartDialog(loveFullLines, () => {
                if (QuestManager.Instance != null) QuestManager.Instance.Advance();
            });
        }
        else
        {
            DialogLine[] lines = new DialogLine[] {
                new DialogLine { speakerName = "Laras " + heartEmoji, subtitle = "Peternak Desa", text = feedbackText, isPlayerLine = false }
            };
            DialogManager.Instance.SetPortraitSilhouettes(playerSilhouette, npcSilhouette);
            DialogManager.Instance.StartDialog(lines, null);
        }
    }

    private void ShowLarasShortDialog(string speaker, string text)
    {
        DialogLine[] lines = new DialogLine[] {
            new DialogLine { speakerName = speaker, subtitle = "", text = text, isPlayerLine = false }
        };
        DialogManager.Instance.SetPortraitSilhouettes(playerSilhouette, npcSilhouette);
        DialogManager.Instance.StartDialog(lines, null);
    }

    /// <summary>Paksa mulai dialog dari luar (misal: cutscene).</summary>
    public void ForceStartDialog()
    {
        if (DialogManager.Instance != null && !DialogManager.Instance.IsDialogActive)
        {
            DialogManager.Instance.SetPortraitSilhouettes(playerSilhouette, npcSilhouette);
            DialogManager.Instance.StartDialog(GetCurrentDialogLines(), HandleDialogComplete);
        }
    }

    private string GetLarasHeartEmoji()
    {
        int pts = (GameManager.Instance != null) ? GameManager.Instance.Data.larasLovePoints : 0;
        if (pts >= 90) return "❤️";      // Merah
        if (pts >= 75) return "🧡";      // Oranye
        if (pts >= 60) return "💛";      // Kuning
        if (pts >= 45) return "💚";      // Hijau
        if (pts >= 30) return "💙";      // Biru
        if (pts >= 15) return "💜";      // Ungu
        return "🖤";                     // Hitam
    }

    private DialogLine[] FormatDialogSpeakers(DialogLine[] lines)
    {
        if (lines == null) return null;
        string heartEmoji = GetLarasHeartEmoji();
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].speakerName == "Laras")
            {
                lines[i].speakerName = "Laras " + heartEmoji;
            }
        }
        return lines;
    }

    private DialogLine[] GetCurrentDialogLines()
    {
        if (QuestManager.Instance == null) return dialogLines;

        int step = QuestManager.Instance.CurrentStep;
        bool isFreeplay = step >= QuestManager.Instance.objectives.Length;

        if (isFreeplay)
        {
            switch (npcId)
            {
                case "Nenek":
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Nenek Rukmini", subtitle = "Tabib Desa",
                            text = "Robby... desa kita ini sekarang sangat makmur berkat ramuan jamumu. Nenek sangat bangga padamu.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Ini semua juga berkat bimbingan Nenek.", isPlayerLine = true }
                    };

                case "Laras":
                    return FormatDialogSpeakers(new DialogLine[] {
                        new DialogLine { speakerName = "Laras", subtitle = "Istri Robby",
                            text = "Halo suamiku! Sapi-sapi kita hari ini sehat sekali. Terima kasih sudah mendampingiku dan membantuku di peternakan.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Sama-sama, istriku tercinta. Aku selalu senang membantumu.", isPlayerLine = true }
                    });

                case "Nisa":
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Nisa", subtitle = "Penjaga Toko",
                            text = "Hai Robby! Selamat atas pernikahanmu dengan Laras ya! Senang melihat kalian bahagia.", isPlayerLine = false }
                    };

                case "Ratri":
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Ratri", subtitle = "Pemburu",
                            text = "Robby, badanku sudah sepenuhnya bugar sejak ramuanmu yang waktu itu. Selamat menempuh hidup baru dengan Laras ya.", isPlayerLine = false }
                    };

                case "Darma":
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Darma", subtitle = "Mertua Robby",
                            text = "Nak Robby! Terima kasih banyak sudah menjaga Laras dan membantunya mengurus peternakan. Bapak bangga punya menantu sepertimu.", isPlayerLine = false }
                    };

                case "Bahri":
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Bahri", subtitle = "Nelayan",
                            text = "Berkat jamumu, aku bisa melaut setiap hari tanpa rasa mual lagi. Terima kasih, Robby!", isPlayerLine = false }
                    };

                case "Darsono":
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Darsono", subtitle = "Kepala Desa",
                            text = "Terima kasih Robby, kamu benar-benar kebanggaan desa.", isPlayerLine = false }
                    };

                case "Seno":
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Seno", subtitle = "Pemilik Toko",
                            text = "Halo Robby! Toko kelontongku selalu terbuka untukmu. Butuh barang-barang kebutuhan harian?", isPlayerLine = false }
                    };
            }
        }

        switch (npcId)
        {
            case "Nenek":
                if (step < 3) return dialogLines;
                if (step == 3)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Nenek Rukmini", subtitle = "Tabib Desa",
                            text = "Bagus, Cu! Tanahnya sudah siap. Ini bibit Jahe dan Kunyit dari Nenek.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Terima kasih, Nek. Ini langsung kutanam?", isPlayerLine = true },
                        new DialogLine { speakerName = "Nenek Rukmini", subtitle = "Tabib Desa",
                            text = "Nanti dulu. Coba buka tasmu dulu [B], lalu lihat buku resep [Tab].", isPlayerLine = false },
                        new DialogLine { speakerName = "Nenek Rukmini", subtitle = "Tabib Desa",
                            text = "Di situ ada resep Jamu Jahe. Buatkan Nenek satu ya, Cu.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Siap, Nek!", isPlayerLine = true }
                    };
                }
                if (step >= 4 && step <= 6)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Nenek Rukmini", subtitle = "Tabib Desa",
                            text = "Bagaimana, Cu? Apakah kamu sudah bisa meracik Jamu Jahe di tungku?", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Sedang kusiapkan, Nek.", isPlayerLine = true }
                    };
                }
                if (step == 7)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Nenek Rukmini", subtitle = "Tabib Desa",
                            text = "Wah, sudah jadi! Coba Nenek cicipi... Hmm, enak! Kamu berbakat, Cu.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Hehe, belajar dari Nenek juga.", isPlayerLine = true },
                        new DialogLine { speakerName = "Nenek Rukmini", subtitle = "Tabib Desa",
                            text = "Nah, sekarang coba jalan-jalan keliling desa. Siapa tahu ada yang butuh bantuan.", isPlayerLine = false }
                    };
                }
                if (step == 30)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Nenek Rukmini", subtitle = "Tabib Desa",
                            text = "Robby... kamu sudah menyembuhkan seluruh desa. Nenek sangat bangga padamu.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Terima kasih, Nek. Ini berkat bimbingan Nenek.", isPlayerLine = true },
                        new DialogLine { speakerName = "Nenek Rukmini", subtitle = "Tabib Desa",
                            text = "Sekarang saatnya kamu memikirkan masa depanmu sendiri. Nenek perhatikan kamu sangat cocok dengan Laras.", isPlayerLine = false },
                        new DialogLine { speakerName = "Nenek Rukmini", subtitle = "Tabib Desa",
                            text = "Pergilah menemui Laras di peternakan, tunjukkan perhatianmu untuk meluluhkan hatinya sampai berwarna Merah.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Baiklah, Nek. Aku akan pergi menemui Laras sekarang.", isPlayerLine = true }
                    };
                }
                return new DialogLine[] {
                    new DialogLine { speakerName = "Nenek Rukmini", subtitle = "Tabib Desa",
                        text = "Jaga dirimu baik-baik ya, Cu. Teruskan meracik jamu yang baik untuk warga.", isPlayerLine = false }
                };

            case "Laras":
                if (step < 8)
                {
                    return FormatDialogSpeakers(new DialogLine[] {
                        new DialogLine { speakerName = "Laras", subtitle = "Peternak Desa",
                            text = "Hai Robby! Senang melihatmu kembali ke desa.", isPlayerLine = false }
                    });
                }
                if (step == 8)
                {
                    return FormatDialogSpeakers(new DialogLine[] {
                        new DialogLine { speakerName = "Laras", subtitle = "Peternak Desa",
                            text = "Robby! Tolonglah, bapakku (Pak Darma) pinggangnya sakit sekali sampai tidak bisa bangun dari tempat tidur.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Aduh, kasihan sekali. Aku akan segera menjenguk Pak Darma ke rumah untuk memeriksa keadaannya.", isPlayerLine = true }
                    });
                }
                if (step >= 9 && step <= 13)
                {
                    return FormatDialogSpeakers(new DialogLine[] {
                        new DialogLine { speakerName = "Laras", subtitle = "Peternak Desa",
                            text = "Bagaimana keadaan bapak, Robby? Semoga ada jamu yang bisa menyembuhkannya.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Tenang Laras, aku sedang meracik Jamu Pegal Linu khusus untuk Pak Darma.", isPlayerLine = true }
                    });
                }
                if (step == 14)
                {
                    return FormatDialogSpeakers(new DialogLine[] {
                        new DialogLine { speakerName = "Laras", subtitle = "Peternak Desa",
                            text = "Robby! Bapak benar-benar pulih dan bisa berjalan lagi sekarang! Terima kasih banyak!", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Sama-sama, Laras. Syukurlah ramuanku bekerja dengan baik.", isPlayerLine = true },
                        new DialogLine { speakerName = "Laras", subtitle = "Peternak Desa",
                            text = "Oh ya, tadi aku mendengar Ratri pergi berburu ke hutan perbatasan. Tapi sepertinya ia sedang demam kedinginan di sana. Bisakah kamu menolongnya?", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Tentu saja, Laras. Aku akan menyusul Ratri ke hutan sekarang.", isPlayerLine = true }
                    });
                }
                if (step == 32) // Proposal Step
                {
                    bool hasFeather = InventoryManager.Instance != null && InventoryManager.Instance.HasItem("Bulu Biru", 1);
                    if (hasFeather)
                    {
                        return FormatDialogSpeakers(new DialogLine[] {
                            new DialogLine { speakerName = "Laras", subtitle = "Peternak Desa",
                                text = "Robby? Ada apa? Kamu terlihat tegang sekali hari ini.", isPlayerLine = false },
                            new DialogLine { speakerName = "Robby", subtitle = "",
                                text = "Laras... terimalah Bulu Biru ini. Maukah kamu menikah denganku dan menjadi pendamping hidupku?", isPlayerLine = true },
                            new DialogLine { speakerName = "Laras", subtitle = "Peternak Desa",
                                text = "Bulu Biru? Robby... kamu melamarku?! Oh! Aku... aku bersedia, Robby! Aku sangat mencintaimu!", isPlayerLine = false },
                            new DialogLine { speakerName = "Laras", subtitle = "Peternak Desa",
                                text = "Mari kita bangun masa depan yang indah bersama-sama di desa ini...", isPlayerLine = false },
                            new DialogLine { speakerName = "Robby", subtitle = "",
                                text = "Terima kasih, Laras! Aku berjanji akan selalu membahagiakanmu.", isPlayerLine = true }
                        });
                    }
                    else
                    {
                        return FormatDialogSpeakers(new DialogLine[] {
                            new DialogLine { speakerName = "Laras", subtitle = "Peternak Desa",
                                text = "Robby? Ada apa? Kamu terlihat kebingungan. Apakah ada sesuatu yang ingin kamu sampaikan padaku?", isPlayerLine = false },
                            new DialogLine { speakerName = "Robby", subtitle = "",
                                text = "Laras... sebenarnya aku ingin menyampaikan sesuatu, tapi belum siap.", isPlayerLine = true },
                            new DialogLine { speakerName = "Laras", subtitle = "Peternak Desa",
                                text = "Hihi, tidak apa-apa Robby. Oh ya, aku dengar Nisa baru mendatangkan barang langka yang indah dari kota di tokonya... mungkin kamu tertarik membelinya?", isPlayerLine = false }
                        });
                    }
                }
                return FormatDialogSpeakers(new DialogLine[] {
                    new DialogLine { speakerName = "Laras", subtitle = "Peternak Desa",
                        text = "Halo Robby! Semoga harimu menyenangkan ya.", isPlayerLine = false }
                });

            case "Darma":
                if (step < 9)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Darma", subtitle = "Peternak Senior",
                            text = "Aduh... badanku lelah sekali...", isPlayerLine = false }
                    };
                }
                if (step == 9)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Darma", subtitle = "Peternak Senior",
                            text = "Aduh... pinggang dan sendi bapak linu sekali, Nak Robby. Nggak sanggup berdiri rasanya.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Bapak istirahat saja dulu. Laras sangat mengkhawatirkan bapak. Saya akan cari resep Jamu Pegal Linu untuk memulihkan sendi bapak.", isPlayerLine = true }
                    };
                }
                if (step >= 10 && step <= 12)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Darma", subtitle = "Peternak Senior",
                            text = "Aduh... pinggang bapak linu sekali...", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Sabar sebentar ya Pak, bahan jamunya sedang saya siapkan.", isPlayerLine = true }
                    };
                }
                if (step == 13)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Darma", subtitle = "Peternak Senior",
                            text = "Terima kasih, Nak Robby... Jamu Pegal Linu ini rasanya hangat sekali.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Minumlah sampai habis Pak, biar khasiatnya cepat terasa.", isPlayerLine = true }
                    };
                }
                return new DialogLine[] {
                    new DialogLine { speakerName = "Pak Darma", subtitle = "Peternak Senior",
                        text = "Nak Robby! Pinggang bapak rasanya bugar sekali! Seperti kembali muda! Hahaha.", isPlayerLine = false }
                };

            case "Ratri":
                if (step < 16)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Ratri", subtitle = "Pemburu",
                            text = "Aku sibuk menjaga hutan perbatasan desa. Hutan sedang kurang bersahabat akhir-akhir ini.", isPlayerLine = false }
                    };
                }
                if (step == 16)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Ratri", subtitle = "Pemburu",
                            text = "Uhuk... badanku... menggigil sekali... kepalaku terasa terbakar...", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Ratri! Laras bilang kamu sakit demam di hutan. Tenang, aku akan segera meracikkan Ramuan Penurun Panas untukmu.", isPlayerLine = true }
                    };
                }
                if (step == 17)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Ratri", subtitle = "Pemburu",
                            text = "Uhuk... rasanya sangat dingin...", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Tahan sebentar Ratri, aku sedang mengumpulkan bahan obatnya.", isPlayerLine = true }
                    };
                }
                if (step == 18)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Ratri", subtitle = "Pemburu",
                            text = "Ramuan Penurun Panas? (Meminum)... Ah! Hangat di tenggorokan, dan demamku perlahan mereda. Terima kasih, Robby.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Syukurlah, beristirahatlah dulu sampai staminamu pulih sepenuhnya.", isPlayerLine = true }
                    };
                }
                if (step == 19)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Ratri", subtitle = "Pemburu",
                            text = "Robby, terima kasih atas obatnya kemarin. Oh ya, tadi aku berpapasan dengan Sekar (istri Pak Bahri) di pantai.", isPlayerLine = false },
                        new DialogLine { speakerName = "Ratri", subtitle = "Pemburu",
                            text = "Sekar terlihat sangat panik karena suaminya (Pak Bahri) terus mual-mual hebat setelah melaut. Coba jenguk mereka.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Pak Bahri mual-mual? Baik, aku akan langsung meluncur ke pantai.", isPlayerLine = true }
                    };
                }
                return new DialogLine[] {
                    new DialogLine { speakerName = "Ratri", subtitle = "Pemburu",
                        text = "Terima kasih atas bantuanmu kemarin, Robby. Hutan perbatasan sekarang aman kujaga.", isPlayerLine = false }
                };

            case "Sekar":
            case "Istri Nelayan":
                if (step == 21)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Sekar", subtitle = "Istri Nelayan",
                            text = "Aduh Robby! Untung kamu ke sini. Pak Bahri (suamiku) mual-mual hebat dari semalam setelah meminum air sungai.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Tenang Bu Sekar, saya akan memeriksa keadaan Pak Bahri dulu di dalam gubuk.", isPlayerLine = true }
                    };
                }
                return new DialogLine[] {
                    new DialogLine { speakerName = "Sekar", subtitle = "Istri Nelayan",
                        text = "Selamat siang Robby. Terima kasih banyak ya sudah menyembuhkan suami ibu.", isPlayerLine = false }
                };

            case "Bahri":
                if (step < 22)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Bahri", subtitle = "Nelayan",
                            text = "Sungai desa hari ini tenang, namun mencari ikan sedang agak sulit.", isPlayerLine = false }
                    };
                }
                if (step == 22)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Bahri", subtitle = "Nelayan",
                            text = "Ugh... perutku melilit... rasanya mual sekali...", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Pak Bahri, istirahat dulu. Bu Sekar sangat panik di luar. Saya akan racikkan Ramuan Anti Mual khusus.", isPlayerLine = true }
                    };
                }
                if (step == 23)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Bahri", subtitle = "Nelayan",
                            text = "Ugh... mual...", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Bahan jamunya sedang saya proses di tungku, Pak.", isPlayerLine = true }
                    };
                }
                if (step == 24)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Bahri", subtitle = "Nelayan",
                            text = "(Meminum ramuan)... Wah! Luar biasa hangat perutku! Mualnya hilang seketika! Terima kasih, peracik muda!", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Sama-sama Pak Bahri. Senang melihat bapak kembali bugar.", isPlayerLine = true }
                    };
                }
                if (step == 25)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Bahri", subtitle = "Nelayan",
                            text = "Terima kasih Robby! Oh ya, tolong jenguk Kepala Desa (Pak Darsono) di balai desa.", isPlayerLine = false },
                        new DialogLine { speakerName = "Pak Bahri", subtitle = "Nelayan",
                            text = "Kudengar beliau juga pening dan bersalah atas air sungai yang kotor.", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Baik Pak Bahri, saya akan segera pergi ke Balai Desa.", isPlayerLine = true }
                    };
                }
                return new DialogLine[] {
                    new DialogLine { speakerName = "Pak Bahri", subtitle = "Nelayan",
                        text = "Berkat jamumu, aku bisa kembali melaut dan menjaring ikan dengan tenang. Terima kasih, Robby!", isPlayerLine = false }
                };

            case "Darsono":
                if (step < 27)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Darsono", subtitle = "Kepala Desa",
                            text = "Selamat datang kembali di desa kita, Robby. Nenek Rukmini sangat merindukanmu.", isPlayerLine = false }
                    };
                }
                if (step == 27)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Darsono", subtitle = "Kepala Desa",
                            text = "Aduh... kepalaku pening sekali... bapak bersalah kurang menjaga aliran air sungai...", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Pak Kades, jangan terlalu menyalahkan diri. Istirahatlah. Saya akan racikkan Jamu Sehat Desa khusus stamina bapak.", isPlayerLine = true }
                    };
                }
                if (step == 28)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Darsono", subtitle = "Kepala Desa",
                            text = "Kepala bapak pening...", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Sebentar lagi jamunya jadi, Pak.", isPlayerLine = true }
                    };
                }
                if (step == 29)
                {
                    return new DialogLine[] {
                        new DialogLine { speakerName = "Pak Darsono", subtitle = "Kepala Desa",
                            text = "(Meminum jamu)... Wah! Tubuh bapak langsung bugar dan segar kembali! Stamina bapak pulih sepenuhnya! Terima kasih, Robby!", isPlayerLine = false },
                        new DialogLine { speakerName = "Robby", subtitle = "",
                            text = "Sama-sama, Pak Kades. Senang bisa menolong bapak.", isPlayerLine = true }
                    };
                }
                return new DialogLine[] {
                    new DialogLine { speakerName = "Pak Darsono", subtitle = "Kepala Desa",
                        text = "Terima kasih Robby, kamu benar-benar kebanggaan desa kita ini.", isPlayerLine = false }
                };

            case "Nisa":
                return new DialogLine[] {
                    new DialogLine { speakerName = "Nisa", subtitle = "Penjaga Toko",
                        text = "Hai Robby! Butuh bibit tanaman herbal atau ingin menjual sesuatu? Tokoku selalu terbuka untukmu!", isPlayerLine = false }
                };

            case "Seno":
                return new DialogLine[] {
                    new DialogLine { speakerName = "Pak Seno", subtitle = "Pemilik Toko",
                        text = "Halo Robby! Toko kelontongku selalu terbuka untukmu. Butuh barang-barang kebutuhan harian?", isPlayerLine = false },
                    new DialogLine { speakerName = "Robby", subtitle = "",
                        text = "Terima kasih, Pak Seno!", isPlayerLine = true }
                };
        }

        return dialogLines;
    }

    private void UpdatePrompt()
    {
        if (DialogManager.Instance == null) return;
        bool isFreeplay = QuestManager.Instance != null && QuestManager.Instance.CurrentStep >= QuestManager.Instance.objectives.Length;
        bool show = playerInRange && (!HasTalked || canTalkAgain || isFreeplay);
        DialogManager.Instance.ShowInteractPrompt(show);
    }

    private void CachePlayer()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawSphere(transform.position, interactRadius);
        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
