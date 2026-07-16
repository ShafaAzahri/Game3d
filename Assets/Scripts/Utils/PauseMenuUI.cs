using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistem Pause Menu Dinamis & Premium (Persistent).
/// - Dipicu otomatis dengan tombol [ESC] atau mengklik tombol Pause (||) di pojok kanan atas.
/// - Pengaturan suara global (AudioListener.volume) lewat Slider UI.
/// - Tombol "LANJUTKAN" untuk menutup pause menu.
/// - Tombol "SIMPAN & KELUAR" untuk auto-save lalu kembali ke Main Menu.
/// - Dibuat secara runtime (dynamic UI) agar tidak mengotori/merubah scene editor.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    private static PauseMenuUI instance;

    private GameObject canvasObj;
    private GameObject menuPanel;
    private Slider volumeSlider;
    private Text volumeValueText;
    private bool isPaused = false;

    private AudioSource clickSource;
    private AudioClip clickSFX;

    // Inisialisasi otomatis saat game berjalan (selalu aktif sebagai manager persisten)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("PauseMenuUI_Manager");
            instance = go.AddComponent<PauseMenuUI>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset state jika pindah scene
        isPaused = false;
        Time.timeScale = 1f;

        if (canvasObj != null)
        {
            Destroy(canvasObj);
        }

        // Jalankan setup HUD hanya di scene gameplay (bukan MainMenu/Cutscene/Credit)
        string sName = scene.name;
        if (sName != "MainMenu" && sName != "Cutscene" && sName != "CreditScene")
        {
            StartCoroutine(SetupHUDNextFrame());
        }
    }

    private System.Collections.IEnumerator SetupHUDNextFrame()
    {
        yield return null;
        SetupHUDPauseButton();
    }

    private void Start()
    {
        // Terapkan volume global dari PlayerPrefs
        float savedVol = PlayerPrefs.GetFloat("GlobalVolume", 1f);
        AudioListener.volume = savedVol;

        // Setup HUD jika scene saat ini adalah gameplay
        string sName = SceneManager.GetActiveScene().name;
        if (sName != "MainMenu" && sName != "Cutscene" && sName != "CreditScene")
        {
            SetupHUDPauseButton();
        }
    }

    private void Update()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenu" || sceneName == "Cutscene" || sceneName == "CreditScene")
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                // Prioritaskan UI lain yang sedang aktif untuk menutup dirinya dulu
                if (!IsAnyOtherUIOpen())
                {
                    Pause();
                }
            }
        }
    }

    private void SetupHUDPauseButton()
    {
        // Cari GoldPanel
        GameObject goldPanel = GameObject.Find("GoldPanel");
        if (goldPanel == null) return;

        // 1. Hilangkan background hitam kotak panjang dari GoldPanel (agar teks melayang)
        Image goldBg = goldPanel.GetComponent<Image>();
        if (goldBg != null)
        {
            goldBg.enabled = false;
        }

        // 2. Geser GoldPanel ke kiri untuk memberi ruang bagi tombol pause
        RectTransform goldRt = goldPanel.GetComponent<RectTransform>();
        if (goldRt != null)
        {
            goldRt.anchorMin = new Vector2(0.72f, 0.92f);
            goldRt.anchorMax = new Vector2(0.89f, 0.99f);
            goldRt.offsetMin = Vector2.zero;
            goldRt.offsetMax = Vector2.zero;
        }

        // 3. Buat tombol Pause putih di pojok kanan atas
        if (GameObject.Find("HUD_PauseButton") == null && goldPanel.transform.parent != null)
        {
            GameObject hudBtnObj = new GameObject("HUD_PauseButton");
            hudBtnObj.transform.SetParent(goldPanel.transform.parent, false);
            RectTransform btnRt = hudBtnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.91f, 0.92f);
            btnRt.anchorMax = new Vector2(0.98f, 0.99f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;

            // Image transparan untuk area klik
            Image btnImg = hudBtnObj.AddComponent<Image>();
            btnImg.color = new Color(0f, 0f, 0f, 0.01f);

            Button btn = hudBtnObj.AddComponent<Button>();
            btn.onClick.AddListener(Pause);
            btn.onClick.AddListener(PlayClickSound);

            hudBtnObj.AddComponent<ButtonHoverEffect>();

            // Buat ikon Pause || putih di tengah
            GameObject iconContainer = new GameObject("IconContainer");
            iconContainer.transform.SetParent(hudBtnObj.transform, false);
            RectTransform iconContainerRt = iconContainer.AddComponent<RectTransform>();
            iconContainerRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconContainerRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconContainerRt.pivot = new Vector2(0.5f, 0.5f);
            iconContainerRt.sizeDelta = new Vector2(30f, 30f);

            // Bar kiri
            GameObject leftBar = new GameObject("LeftBar");
            leftBar.transform.SetParent(iconContainer.transform, false);
            RectTransform leftBarRt = leftBar.AddComponent<RectTransform>();
            leftBarRt.anchorMin = new Vector2(0.5f, 0.5f);
            leftBarRt.anchorMax = new Vector2(0.5f, 0.5f);
            leftBarRt.pivot = new Vector2(0.5f, 0.5f);
            leftBarRt.anchoredPosition = new Vector2(-5f, 0f);
            leftBarRt.sizeDelta = new Vector2(5f, 20f);
            Image leftBarImg = leftBar.AddComponent<Image>();
            leftBarImg.color = Color.white;

            // Bar kanan
            GameObject rightBar = new GameObject("RightBar");
            rightBar.transform.SetParent(iconContainer.transform, false);
            RectTransform rightBarRt = rightBar.AddComponent<RectTransform>();
            rightBarRt.anchorMin = new Vector2(0.5f, 0.5f);
            rightBarRt.anchorMax = new Vector2(0.5f, 0.5f);
            rightBarRt.pivot = new Vector2(0.5f, 0.5f);
            rightBarRt.anchoredPosition = new Vector2(5f, 0f);
            rightBarRt.sizeDelta = new Vector2(5f, 20f);
            Image rightBarImg = rightBar.AddComponent<Image>();
            rightBarImg.color = Color.white;
        }
    }

    private bool IsAnyOtherUIOpen()
    {
        // 1. Dialogue panel active
        if (DialogManager.Instance != null && DialogManager.Instance.dialogPanel != null && DialogManager.Instance.dialogPanel.activeInHierarchy)
            return true;

        // 2. Inventory UI active
        var inv = Object.FindFirstObjectByType<InventoryUI>();
        if (inv != null && inv.gameObject.activeInHierarchy)
            return true;

        // 3. Cooking UI active
        var cook = Object.FindFirstObjectByType<CookingUI>();
        if (cook != null && cook.gameObject.activeInHierarchy)
            return true;

        // 4. Shop UI active
        var shop = Object.FindFirstObjectByType<ShopUI>();
        if (shop != null && shop.shopPanel != null && shop.shopPanel.activeInHierarchy)
            return true;

        // 5. Cutscene Player active
        var cutscene = Object.FindFirstObjectByType<CutscenePlayer>();
        if (cutscene != null && cutscene.gameObject.activeInHierarchy)
            return true;

        return false;
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (canvasObj == null)
        {
            CreatePauseUI();
        }
        else
        {
            canvasObj.SetActive(true);
            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
            }
        }

        // Tampilkan kursor mouse agar bisa interaksi dengan menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (canvasObj != null)
        {
            canvasObj.SetActive(false);
        }
    }

    private void CreatePauseUI()
    {
        // 1. Create Canvas
        canvasObj = new GameObject("PauseCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Background Blur/Overlay
        GameObject bgObj = new GameObject("BackgroundOverlay");
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.75f);

        // 3. Panel Menu Tengah
        menuPanel = new GameObject("MenuPanel");
        menuPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(400f, 320f);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        
        Image panelImg = menuPanel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        
        Outline outline = menuPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.82f, 0.38f, 0.6f); // Garis tepi emas
        outline.effectDistance = new Vector2(2f, -2f);

        // 4. Judul Menu
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(menuPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(300f, 50f);
        titleRect.anchoredPosition = new Vector2(0f, 110f);
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        
        Text titleTxt = titleObj.AddComponent<Text>();
        titleTxt.text = "PAUSE MENU";
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 26;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(0.95f, 0.82f, 0.38f, 1f);

        // 5. Wadah Pengaturan Volume
        GameObject volContainer = new GameObject("VolumeContainer");
        volContainer.transform.SetParent(menuPanel.transform, false);
        RectTransform volContainerRect = volContainer.AddComponent<RectTransform>();
        volContainerRect.sizeDelta = new Vector2(320f, 60f);
        volContainerRect.anchoredPosition = new Vector2(0f, 30f);
        volContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
        volContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
        volContainerRect.pivot = new Vector2(0.5f, 0.5f);

        // Label teks "Volume"
        GameObject volLabelObj = new GameObject("VolumeLabel");
        volLabelObj.transform.SetParent(volContainer.transform, false);
        RectTransform volLabelRect = volLabelObj.AddComponent<RectTransform>();
        volLabelRect.sizeDelta = new Vector2(100f, 25f);
        volLabelRect.anchoredPosition = new Vector2(-110f, 10f);
        Text volLabelTxt = volLabelObj.AddComponent<Text>();
        volLabelTxt.text = "VOLUME SUARA";
        volLabelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        volLabelTxt.fontSize = 12;
        volLabelTxt.fontStyle = FontStyle.Bold;
        volLabelTxt.alignment = TextAnchor.MiddleLeft;
        volLabelTxt.color = Color.white;

        // Persentase teks "100%"
        GameObject volValObj = new GameObject("VolumeValue");
        volValObj.transform.SetParent(volContainer.transform, false);
        RectTransform volValRect = volValObj.AddComponent<RectTransform>();
        volValRect.sizeDelta = new Vector2(50f, 25f);
        volValRect.anchoredPosition = new Vector2(125f, -10f);
        volumeValueText = volValObj.AddComponent<Text>();
        volumeValueText.text = Mathf.RoundToInt(AudioListener.volume * 100) + "%";
        volumeValueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        volumeValueText.fontSize = 12;
        volumeValueText.fontStyle = FontStyle.Bold;
        volumeValueText.alignment = TextAnchor.MiddleRight;
        volumeValueText.color = Color.white;

        // Slider Bar
        GameObject sliderObj = new GameObject("VolumeSlider");
        sliderObj.transform.SetParent(volContainer.transform, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(230f, 20f);
        sliderRect.anchoredPosition = new Vector2(-15f, -10f);
        
        volumeSlider = sliderObj.AddComponent<Slider>();

        GameObject sliderBg = new GameObject("Background");
        sliderBg.transform.SetParent(sliderObj.transform, false);
        RectTransform bgR = sliderBg.AddComponent<RectTransform>();
        bgR.anchorMin = new Vector2(0f, 0.25f);
        bgR.anchorMax = new Vector2(1f, 0.75f);
        bgR.offsetMin = Vector2.zero;
        bgR.offsetMax = Vector2.zero;
        Image sliderBgImg = sliderBg.AddComponent<Image>();
        sliderBgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5f, 0f);
        fillAreaRect.offsetMax = new Vector2(-5f, 0f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.95f, 0.82f, 0.38f, 1f);

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(16f, 16f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        
        Outline handleOutline = handle.AddComponent<Outline>();
        handleOutline.effectColor = Color.black;
        handleOutline.effectDistance = new Vector2(1f, -1f);

        volumeSlider.fillRect = fillRect;
        volumeSlider.handleRect = handleRect;
        volumeSlider.targetGraphic = handleImg;
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value = AudioListener.volume;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        // 6. Tombol Resume
        CreateButton("ResumeButton", "LANJUTKAN", new Vector2(0f, -50f), Resume);

        // 7. Tombol Keluar
        CreateButton("SaveExitButton", "SIMPAN & KELUAR", new Vector2(0f, -105f), SaveAndExit);
    }

    private void CreateButton(string objName, string label, Vector2 pos, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject btnObj = new GameObject(objName);
        btnObj.transform.SetParent(menuPanel.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(250f, 42f);
        btnRect.anchoredPosition = pos;
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClickAction);
        btn.onClick.AddListener(PlayClickSound);

        btnObj.AddComponent<ButtonHoverEffect>();

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text labelTxt = labelObj.AddComponent<Text>();
        labelTxt.text = label;
        labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelTxt.fontSize = 13;
        labelTxt.fontStyle = FontStyle.Bold;
        labelTxt.alignment = TextAnchor.MiddleCenter;
        labelTxt.color = Color.white;

        Outline textOutline = labelObj.AddComponent<Outline>();
        textOutline.effectColor = Color.black;
        textOutline.effectDistance = new Vector2(1f, -1f);
    }

    private void OnVolumeChanged(float val)
    {
        AudioListener.volume = val;
        PlayerPrefs.SetFloat("GlobalVolume", val);
        PlayerPrefs.Save();

        if (volumeValueText != null)
        {
            volumeValueText.text = Mathf.RoundToInt(val * 100) + "%";
        }
    }

    private void PlayClickSound()
    {
        if (clickSource == null)
        {
            clickSource = gameObject.AddComponent<AudioSource>();
            clickSource.loop = false;
            clickSource.playOnAwake = false;
            clickSource.volume = 0.5f;
            clickSFX = Resources.Load<AudioClip>("Music/click button");
        }
        if (clickSource != null && clickSFX != null)
        {
            clickSource.PlayOneShot(clickSFX);
        }
    }

    private void SaveAndExit()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
            Debug.Log("[PauseMenuUI] Game saved successfully before exiting to Main Menu.");
        }

        SceneManager.LoadScene("MainMenu");
    }
}
