using UnityEngine;

// Hace que un elemento de la UI (el logo) aparezca con rebote y luego flote suavemente.
public class Flotar : MonoBehaviour
{
    public float altura = 10f;
    public float velocidad = 1.6f;
    public float respiracion = 0.025f;
    public float giro = 1.5f;

    RectTransform rt;
    Vector2 posBase;
    float entrada;

    void Awake()
    {
        rt = (RectTransform)transform;
        posBase = rt.anchoredPosition;
    }

    void OnEnable()
    {
        entrada = 0f;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;
        entrada = Mathf.Min(1f, entrada + dt * 1.8f);

        // Rebote de entrada (sobrepasa un poquito y se acomoda)
        float e = entrada;
        float rebote = 1f + Mathf.Sin(e * Mathf.PI * 1.5f) * (1f - e) * 0.35f;
        float escalaEntrada = Mathf.Lerp(0.4f, 1f, Mathf.SmoothStep(0f, 1f, e)) * rebote;

        float s = 1f + Mathf.Sin(t * velocidad * 1.3f) * respiracion;
        rt.localScale = Vector3.one * escalaEntrada * s;
        rt.anchoredPosition = posBase + Vector2.up * Mathf.Sin(t * velocidad) * altura;
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * velocidad * 0.7f) * giro);
    }
}
