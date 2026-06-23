using UnityEngine;

/// <summary>
/// Penanda objektif melayang ("!") yang mengikuti target aktif.
/// Dikendalikan oleh QuestManager: cukup set 'target' ke objek yang harus dituju,
/// atau null untuk menyembunyikan.
///
/// - Otomatis melayang di atas kepala/puncak target (dari bounds renderer)
/// - Bob naik-turun + selalu menghadap kamera (billboard)
/// </summary>
public class QuestMarker : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Child visual (teks '!') yang di-show/hide. Kalau null, pakai GameObject ini.")]
    public Transform icon;

    [Header("Target")]
    [Tooltip("Objek yang sedang dituju. Null = marker disembunyikan.")]
    public Transform target;

    [Tooltip("Jarak marker di atas puncak target (meter).")]
    public float margin = 0.7f;

    [Header("Animasi")]
    public float bobAmount = 0.18f;
    public float bobSpeed = 2.2f;
    public bool billboard = true;

    private Transform cachedTarget;
    private float topY;
    private Camera cam;

    private void LateUpdate()
    {
        if (target == null)
        {
            SetIconVisible(false);
            return;
        }

        SetIconVisible(true);

        if (target != cachedTarget)
        {
            cachedTarget = target;
            topY = ComputeTopY(target);
        }

        Vector3 p = new Vector3(target.position.x, topY + margin, target.position.z);
        p.y += Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = p;

        if (billboard)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }

    private void SetIconVisible(bool visible)
    {
        var go = (icon != null) ? icon.gameObject : gameObject;
        if (icon == null) return; // jangan matikan diri sendiri (LateUpdate harus jalan)
        if (go.activeSelf != visible) go.SetActive(visible);
    }

    private float ComputeTopY(Transform t)
    {
        var renderers = t.GetComponentsInChildren<Renderer>();
        bool has = false;
        Bounds b = new Bounds();
        foreach (var r in renderers)
        {
            if (r is ParticleSystemRenderer) continue;
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        return has ? b.max.y : t.position.y + 1.6f;
    }
}
