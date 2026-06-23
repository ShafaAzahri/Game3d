using UnityEngine;
using System.Collections.Generic;

// ================================================
// DIALOGUE DATA - ScriptableObject untuk data dialog NPC
// ================================================
[CreateAssetMenu(fileName = "NewDialogue", menuName = "RPG/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("NPC Info")]
    public string npcName = "NPC";

    [Header("Dialog Lines")]
    [TextArea(2, 5)]
    public List<string> lines = new List<string>();

    [Header("Optional: Portrait")]
    public Sprite npcPortrait;
}
