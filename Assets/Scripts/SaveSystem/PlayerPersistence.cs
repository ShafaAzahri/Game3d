using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simpan & pulihkan posisi/rotasi player + scene aktif ke SaveData.
/// Pasang di GameObject player (MC).
///
/// - OnCaptureState : tulis posisi sekarang ke SaveData (saat menyimpan)
/// - OnApplyState   : pindahkan player ke posisi tersimpan (saat Continue)
/// </summary>
public class PlayerPersistence : MonoBehaviour
{
    private CharacterController cc;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCaptureState += Capture;
            GameManager.Instance.OnApplyState   += Apply;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCaptureState -= Capture;
            GameManager.Instance.OnApplyState   -= Apply;
        }
    }

    private void Capture()
    {
        var d = GameManager.Instance.Data;
        d.sceneName = SceneManager.GetActiveScene().name;
        Vector3 p = transform.position;
        d.playerX = p.x; d.playerY = p.y; d.playerZ = p.z;
        d.playerRotY = transform.eulerAngles.y;
        d.hasPlayerPosition = true;
    }

    private void Apply()
    {
        var d = GameManager.Instance.Data;
        if (!d.hasPlayerPosition) return;

        Vector3 pos = new Vector3(d.playerX, d.playerY, d.playerZ);

        // CharacterController menolak perubahan transform langsung — matikan sementara
        bool hadCC = cc != null && cc.enabled;
        if (hadCC) cc.enabled = false;

        transform.position = pos;
        transform.rotation = Quaternion.Euler(0f, d.playerRotY, 0f);

        if (hadCC) cc.enabled = true;
    }
}
