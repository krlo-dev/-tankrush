using UnityEngine;

// Movimiento lento de cámara sobre la ilustración del menú (zoom y paneo suaves).
public class FondoAnimado : MonoBehaviour
{
    public float zoom = 0.035f;
    public float paneo = 18f;
    public float velocidad = 0.12f;

    RectTransform rt;
    Vector2 posBase;

    void Awake()
    {
        rt = (RectTransform)transform;
        posBase = rt.anchoredPosition;
    }

    void Update()
    {
        float t = Time.unscaledTime * velocidad;
        rt.localScale = Vector3.one * (1f + zoom + zoom * Mathf.Sin(t));
        rt.anchoredPosition = posBase + new Vector2(Mathf.Sin(t * 0.7f) * paneo, Mathf.Cos(t * 0.9f) * paneo * 0.5f);
    }
}
