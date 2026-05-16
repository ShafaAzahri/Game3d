using UnityEngine;

public class SpawnPlayer : MonoBehaviour
{
    public GameObject player;

    void Start()
    {
        GameObject spawn =
            GameObject.Find("SpawnPoint");

        if (spawn != null)
        {
            player.transform.position =
                spawn.transform.position;

            player.transform.rotation =
                spawn.transform.rotation;
        }
    }
}