using UnityEngine;

public class Tank_Cannon : MonoBehaviour
{
    public Transform torreta;          // Body 2
    public Transform canon;            // Body 3
    public Transform puntoDisparo;     // PuntoDisparo
    public GameObject balaPrefab;      // tu bala que ya funciona
    public float fuerzaDisparo = 30f;
    public float sensibilidad = 2f;
    public float anguloMin = -5f;      // hacia abajo
    public float anguloMax = 35f;      // hacia arriba

    Quaternion canonInicial, torretaInicial;
    float angulo, giroTorreta;

    void Start()
    {
        canonInicial = canon.localRotation;
        torretaInicial = torreta.localRotation;
    }

    void Update()
    {
        // Click derecho mantenido: mouse arriba/abajo = ángulo, izquierda/derecha = girar torreta
        if (Input.GetMouseButton(1))
        {
            angulo += Input.GetAxis("Mouse Y") * sensibilidad;
            angulo = Mathf.Clamp(angulo, anguloMin, anguloMax);
            giroTorreta += Input.GetAxis("Mouse X") * sensibilidad;

            canon.localRotation = canonInicial * Quaternion.Euler(-angulo, 0f, 0f);
            torreta.localRotation = torretaInicial * Quaternion.Euler(0f, giroTorreta, 0f);
        }

        if (Input.GetMouseButtonDown(0))
            Disparar();
    }

    void Disparar()
    {
        GameObject bala = Instantiate(balaPrefab, puntoDisparo.position, puntoDisparo.rotation);
        Rigidbody brb = bala.GetComponent<Rigidbody>();
        if (brb != null) brb.linearVelocity = puntoDisparo.forward * fuerzaDisparo;
    }
}