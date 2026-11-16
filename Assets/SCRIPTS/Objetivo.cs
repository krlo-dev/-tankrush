using UnityEngine;

public class Objetivo : MonoBehaviour
{
    [Header("Vida del objetivo")]
    public float vidaMaxima = 50f;
    private float vidaActual;

    [Header("Puntos")]
    public int puntosAlDestruir = 10;
    private bool yaDestruido = false;

    [Header("Efecto al primer impacto (opcional)")]
    public GameObject efectoPrimerImpacto; // se instancia solo la primera vez que recibe daño
    private bool yaRecibioPrimerImpacto = false;

    [Header("Efecto al destruirse (opcional)")]
    public GameObject efectoDestruccion;

    void Start()
    {
        vidaActual = vidaMaxima;
    }

    // Este método lo llama Bullet.cs cuando el objetivo recibe un impacto
    public void RecibirDaño(float cantidad)
    {
        Debug.Log("Objetivo recibió daño: " + cantidad + " | vida antes: " + vidaActual);

        if (!yaRecibioPrimerImpacto)
        {
            yaRecibioPrimerImpacto = true;

            if (efectoPrimerImpacto != null)
            {
                GameObject fxInicial = Instantiate(efectoPrimerImpacto, transform.position, transform.rotation);
                Destroy(fxInicial, 3f); // limpia el efecto cuando termina
            }
        }

        vidaActual -= cantidad;

        if (vidaActual <= 0f)
        {
            Destruir();
        }
    }

    protected virtual void Destruir()
    {
        if (yaDestruido) return; // evita sumar dos veces si llegan dos balas a la vez
        yaDestruido = true;

        if (ContadorPuntos.instancia != null)
        {
            ContadorPuntos.instancia.SumarPuntos(puntosAlDestruir);
        }

        if (efectoDestruccion != null)
        {
            GameObject fx = Instantiate(efectoDestruccion, transform.position, Quaternion.identity);
            Destroy(fx, 3f); // limpia el efecto cuando termina
        }

        Destroy(gameObject);
    }
}