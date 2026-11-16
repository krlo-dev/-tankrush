using UnityEngine;
using UnityEngine.EventSystems;

// Hace que los botones crezcan un poquito al pasar el mouse y suenen al hacer clic.
public class BotonKawaii : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public float escalaHover = 1.08f;
    Vector3 escalaBase;
    float objetivo = 1f;
    float actual = 1f;

    void Awake()
    {
        escalaBase = transform.localScale;
    }

    void OnDisable()
    {
        objetivo = actual = 1f;
        if (escalaBase != Vector3.zero) transform.localScale = escalaBase;
    }

    void Update()
    {
        actual = Mathf.Lerp(actual, objetivo, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        transform.localScale = escalaBase * actual;
    }

    public void OnPointerEnter(PointerEventData e) => objetivo = escalaHover;
    public void OnPointerExit(PointerEventData e) => objetivo = 1f;

    public void OnPointerClick(PointerEventData e)
    {
        actual = 0.92f;
        Sonidos.Reproducir(Sonidos.Click, 0.5f);
    }
}
