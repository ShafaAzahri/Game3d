using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    public Image fadeImage;
    public float fadeDuration = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(FadeIn());
    }

    public void LoadSceneSmooth(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    IEnumerator LoadSceneRoutine(string sceneName)
    {
        yield return StartCoroutine(FadeOut());

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            yield return null;
        }

        yield return StartCoroutine(FadeIn());
    }

    IEnumerator FadeOut()
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            float alpha =
                Mathf.Lerp(0f, 1f, time / fadeDuration);

            fadeImage.color =
                new Color(0f, 0f, 0f, alpha);

            yield return null;
        }
    }

    IEnumerator FadeIn()
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            float alpha =
                Mathf.Lerp(1f, 0f, time / fadeDuration);

            fadeImage.color =
                new Color(0f, 0f, 0f, alpha);

            yield return null;
        }
    }
}