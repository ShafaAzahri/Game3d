using UnityEngine;
using UnityEngine.UI;

public class HotbarSelector : MonoBehaviour
{
    public Button[] slots;

    private int currentIndex = 0;

    void Start()
    {
        SelectSlot(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SelectSlot(0);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SelectSlot(1);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            SelectSlot(2);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            SelectSlot(3);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            SelectSlot(4);

        if (Input.GetKeyDown(KeyCode.Alpha6))
            SelectSlot(5);

        if (Input.GetKeyDown(KeyCode.Alpha7))
            SelectSlot(6);
    }

    void SelectSlot(int index)
    {
        currentIndex = index;

        for (int i = 0; i < slots.Length; i++)
        {
            ColorBlock cb = slots[i].colors;

            if (i == index)
            {
                cb.normalColor = Color.yellow;
            }
            else
            {
                cb.normalColor = Color.white;
            }

            slots[i].colors = cb;
        }

        // Trigger click event button
        slots[index].onClick.Invoke();
    }
}