using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class CreditMember
{
    public string memberName;
    public string memberInfo; // NIM or other info
    public Sprite memberPhoto;
}

public class CreditSlideController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image photoImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button backButton;

    [Header("Slide Animation")]
    [SerializeField] private RectTransform slideContainer;
    [SerializeField] private float slideSpeed = 10f;
    [SerializeField] private CanvasGroup slideCanvasGroup;
    [SerializeField] private float fadeSpeed = 5f;

    [Header("Data")]
    [SerializeField] private List<CreditMember> members = new List<CreditMember>();
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private int currentIndex = 0;
    private bool isTransitioning = false;

    private void Start()
    {
        // Add default dummy data if empty
        if (members.Count == 0)
        {
            members.Add(new CreditMember { memberName = "Adinda Rahimah A", memberInfo = "4.33.23.2.01" });
            members.Add(new CreditMember { memberName = "Anggota 2", memberInfo = "NIM 2" });
            members.Add(new CreditMember { memberName = "Anggota 3", memberInfo = "NIM 3" });
            members.Add(new CreditMember { memberName = "Anggota 4", memberInfo = "NIM 4" });
            members.Add(new CreditMember { memberName = "Anggota 5", memberInfo = "NIM 5" });
        }

        // Setup button listeners
        if (nextButton != null) nextButton.onClick.AddListener(NextSlide);
        if (prevButton != null) prevButton.onClick.AddListener(PrevSlide);
        if (backButton != null) backButton.onClick.AddListener(BackToMenu);

        UpdateUI(true);
    }

    private void NextSlide()
    {
        if (isTransitioning || members.Count <= 1) return;
        
        currentIndex++;
        if (currentIndex >= members.Count) currentIndex = 0;
        
        StartCoroutine(TransitionSlide(1)); // 1 for right to left
    }

    private void PrevSlide()
    {
        if (isTransitioning || members.Count <= 1) return;
        
        currentIndex--;
        if (currentIndex < 0) currentIndex = members.Count - 1;
        
        StartCoroutine(TransitionSlide(-1)); // -1 for left to right
    }

    private void BackToMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator TransitionSlide(int direction)
    {
        isTransitioning = true;

        // Fade out
        while (slideCanvasGroup != null && slideCanvasGroup.alpha > 0)
        {
            slideCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        // Update Content
        UpdateUI(false);

        // Optional: Reset position for slide-in effect
        if (slideContainer != null)
        {
            Vector2 pos = slideContainer.anchoredPosition;
            pos.x = direction * 50f; // Start a bit offset
            slideContainer.anchoredPosition = pos;

            // Slide and fade in
            while (slideCanvasGroup != null && slideCanvasGroup.alpha < 1)
            {
                slideCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
                slideContainer.anchoredPosition = Vector2.Lerp(slideContainer.anchoredPosition, Vector2.zero, Time.deltaTime * slideSpeed);
                yield return null;
            }

            slideContainer.anchoredPosition = Vector2.zero;
            if (slideCanvasGroup != null) slideCanvasGroup.alpha = 1;
        }
        else if (slideCanvasGroup != null)
        {
            // Just fade in if no container assigned
            while (slideCanvasGroup.alpha < 1)
            {
                slideCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
                yield return null;
            }
        }

        isTransitioning = false;
    }

    private void UpdateUI(bool immediate)
    {
        if (members == null || members.Count == 0) return;

        CreditMember currentMember = members[currentIndex];

        if (nameText != null) nameText.text = currentMember.memberName;
        if (infoText != null) infoText.text = currentMember.memberInfo;
        
        if (photoImage != null)
        {
            if (currentMember.memberPhoto != null)
            {
                photoImage.sprite = currentMember.memberPhoto;
                photoImage.color = Color.white;
            }
            else
            {
                // Clear or show placeholder
                photoImage.sprite = null;
                photoImage.color = new Color(0,0,0, 0.5f); // Semi-transparent black placeholder
            }
        }

        if (immediate && slideCanvasGroup != null)
        {
            slideCanvasGroup.alpha = 1f;
        }
    }
}
