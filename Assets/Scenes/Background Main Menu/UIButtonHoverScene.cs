using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

public class UIButtonHoverScene : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Scene Settings")]
    [SerializeField] private string sceneName;

    [Header("Hover Animation")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animationSpeed = 8f;

    [Header("Click Animation")]
    [SerializeField] private float clickScale = 0.9f;
    [SerializeField] private float clickDuration = 0.1f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * animationSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        StartCoroutine(ClickAndLoadScene());
    }

    private IEnumerator ClickAndLoadScene()
    {
        transform.localScale = originalScale * clickScale;

        yield return new WaitForSeconds(clickDuration);

        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Scene Name belum diisi pada " + gameObject.name);
        }
    }
}