using UnityEngine;

public class Disparo : MonoBehaviour
{
    public GameObject balaPrefab;
    public Transform puntoDisparo;   // debe ser hijo del cañón para que siga su ángulo
    public float velocidadBala = 30f;
    public float cadencia = 0.5f;    // tiempo mínimo entre disparos

    private float proximoDisparo = 0f;

    void Update()
    {
        if (Input.GetButtonDown("Fire1") && Time.time >= proximoDisparo)
        {
            Disparar();
            proximoDisparo = Time.time + cadencia;
        }
    }

    void Disparar()
    {
        GameObject bala = Instantiate(balaPrefab, puntoDisparo.position, puntoDisparo.rotation);

        // Bullet.cs es quien mueve la bala (en su Start usa su propio campo "velocidad"),
        // así que le pasamos la velocidad aquí, antes de que ese Start se ejecute.
        // La autodestrucción por tiempo también la maneja Bullet.cs (tiempoDeVida).
        Bullet scriptBala = bala.GetComponent<Bullet>();
        if (scriptBala != null)
        {
            scriptBala.velocidad = velocidadBala;
        }
    }
}
