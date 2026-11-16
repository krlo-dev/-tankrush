
using UnityEngine;
using TMPro;

public class ControladorPuntuacion : MonoBehaviour
{
    public TMP_Text textoPuntuacion;
    public TMP_Text textoVictoria;

    public int puntuacionActual = 0;
    public int puntuacionObjetivo = 100;

    private bool juegoTerminado = false;

    void Start()
    {
        textoVictoria.gameObject.SetActive(false);
        ActualizarPuntuacion();
    }

    public void SumarPuntos(int puntos)
    {
        if (juegoTerminado)
            return;

        puntuacionActual += puntos;

        ActualizarPuntuacion();

        if (puntuacionActual >= puntuacionObjetivo)
        {
            GanarJuego();
        }
    }

    void ActualizarPuntuacion()
    {
        textoPuntuacion.text = "Puntuación: " +
            puntuacionActual + " / " + puntuacionObjetivo;
    }

    void GanarJuego()
    {
        juegoTerminado = true;

        textoVictoria.gameObject.SetActive(true);

        Time.timeScale = 0f;
    }
}
