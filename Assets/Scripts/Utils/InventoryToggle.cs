using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryUI;
    public GameObject hotbarUI;

    private bool isOpen = false;

    void Start()
    {
        inventoryUI.SetActive(false);
        hotbarUI.SetActive(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            isOpen = !isOpen;

            inventoryUI.SetActive(isOpen);

            // kalau inventory buka -> hotbar hilang
            hotbarUI.SetActive(!isOpen);

            if (isOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                Time.timeScale = 0f;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                Time.timeScale = 1f;
            }
        }
    }
}