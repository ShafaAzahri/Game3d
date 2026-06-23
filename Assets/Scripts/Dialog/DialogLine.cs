using System;
using UnityEngine;

/// <summary>
/// Satu baris dialog. Bisa diisi dari Inspector di NPCDialog component.
/// </summary>
[Serializable]
public class DialogLine
{
    [Tooltip("Nama yang ditampilkan di kotak nama. Gunakan nama karakter, contoh: 'Nenek' atau 'MC'")]
    public string speakerName = "Nenek";

    [Tooltip("Subtitle / jabatan / keterangan di bawah nama (opsional, bisa dikosongkan)")]
    public string subtitle = "";

    [Tooltip("Isi teks dialog")]
    [TextArea(2, 5)]
    public string text = "";

    [Tooltip("Centang ini jika yang bicara adalah Player/MC — nama akan berwarna biru muda")]
    public bool isPlayerLine = false;

    [Tooltip("Gambar ekspresi/portrait yang ditampilkan saat baris ini muncul. Boleh dikosongkan — kalau kosong, portrait akan disembunyikan.")]
    public Sprite expression = null;
}
