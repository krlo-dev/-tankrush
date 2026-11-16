using UnityEngine;

public class Move_tank : MonoBehaviour
{
    public float velocidad = 10f;
    public float velocidadGiro = 100f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float avance = Input.GetAxis("Vertical");
        float giro = Input.GetAxis("Horizontal");

        // Mover hacia adelante/atrás en la dirección que mira el tanque
        Vector3 movimiento = transform.forward * avance * velocidad * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movimiento);

        // Girar sobre su propio eje Y
        float rotacion = giro * velocidadGiro * Time.fixedDeltaTime;
        Quaternion giroDelta = Quaternion.Euler(0f, rotacion, 0f);
        rb.MoveRotation(rb.rotation * giroDelta);
    }
}
