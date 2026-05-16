using UnityEngine;

public class HouseButtonTrigger : MonoBehaviour
{
    public GameObject enterButton;

    [Header("Scene Configuration")]
    [Tooltip("Drag scene asset ke sini dari Project window")]
    public string targetScene;

    private bool playerInside = false;

    void Update()
    {
        if (playerInside && Input.GetKeyDown(KeyCode.G))
        {
            if (!string.IsNullOrEmpty(targetScene))
            {
                Debug.Log("SCENE: " + targetScene);
                SceneTransitionManager.Instance.LoadSceneSmooth(targetScene);
            }
            else
            {
                Debug.LogWarning("Target Scene belum dipilih di Inspector!");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            if (enterButton != null) enterButton.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            if (enterButton != null) enterButton.SetActive(false);
        }
    }
}