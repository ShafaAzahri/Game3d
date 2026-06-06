using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryUI;

    private InventoryUI inventoryUIScript;
    private bool isOpen = false;

    void Start()
    {
        if (inventoryUI != null)
        {
            inventoryUIScript = inventoryUI.GetComponent<InventoryUI>();
            inventoryUI.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            isOpen = !isOpen;

            if (isOpen)
            {
                if (inventoryUIScript != null)
                    inventoryUIScript.Open();
                else if (inventoryUI != null)
                    inventoryUI.SetActive(true);

                Debug.Log("[InventoryToggle] Inventory dibuka.");
            }
            else
            {
                if (inventoryUIScript != null)
                    inventoryUIScript.Close();
                else if (inventoryUI != null)
                    inventoryUI.SetActive(false);

                Debug.Log("[InventoryToggle] Inventory ditutup.");
            }
        }
    }
}