using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Configuración de la bala")]
    public float velocidad = 20f;   // Disparo.cs la sobrescribe al instanciar la bala
    public float daño = 25f;
    public float tiempoDeVida = 3f; // seguridad para que no quede volando para siempre

    [Header("Efectos (opcional)")]
    public GameObject efectoImpacto; // prefab de partículas que se muestra al chocar

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // la bala no debe caer por gravedad
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // evita que atraviese el objetivo
    }

    void Start()
    {
        rb.linearVelocity = transform.forward * velocidad; // en versiones viejas de Unity usa: rb.velocity
        Destroy(gameObject, tiempoDeVida);
    }

    // IMPORTANTE: el Collider de la bala debe tener "Is Trigger" activado
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bala") || other.CompareTag("Jugador")) return;

        // Punto aproximado donde la bala tocó al otro objeto
        Vector3 puntoImpacto = other.ClosestPoint(transform.position);

        if (efectoImpacto != null)
        {
            Instantiate(efectoImpacto, puntoImpacto, Quaternion.identity);
        }

        Objetivo objetivo = other.GetComponent<Objetivo>();
        if (objetivo != null)
        {
            objetivo.RecibirDaño(daño);
        }

        Destroy(gameObject);
    }
}
