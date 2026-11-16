using UnityEngine;

public class AjusteCanon : MonoBehaviour
{
    [Header("Ángulo de tiro")]
    public float sensibilidad = 2f;
    public float anguloMin = -5f;    // cuánto puede bajar el cañón
    public float anguloMax = 35f;    // cuánto puede subir el cañón

    private float elevacion;         // positivo = cañón apuntando hacia arriba

    void Update()
    {
        // Solo mientras mantienes presionado el click derecho
        if (Input.GetMouseButton(1))
        {
            elevacion += Input.GetAxis("Mouse Y") * sensibilidad;
            elevacion = Mathf.Clamp(elevacion, anguloMin, anguloMax);

            // En Unity, rotar en X con valor negativo sube la punta del cañón
            transform.localRotation = Quaternion.Euler(-elevacion, 0f, 0f);
        }
    }
}
