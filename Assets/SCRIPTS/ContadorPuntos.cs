using UnityEngine;
using TMPro;

public class ContadorPuntos : MonoBehaviour
{
    public static ContadorPuntos instancia;

    public TextMeshProUGUI textoPuntos;
    private int puntos = 0;

    void Awake()
    {
        instancia = this;
    }

   public void SumarPuntos(int cantidad)
{
    puntos += cantidad;
    Debug.Log("Puntos actuales: " + puntos);

    if (textoPuntos == null)
    {
        Debug.LogWarning("textoPuntos está vacío en el GameManager");
        return;
    }

    textoPuntos.text = "Puntos: " + puntos;
}
}