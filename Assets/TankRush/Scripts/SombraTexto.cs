using TMPro;
using UnityEngine;

// Mantiene una copia del texto desplazada detrás para dar efecto de sombra.
[ExecuteAlways]
public class SombraTexto : MonoBehaviour
{
    public TMP_Text texto;
    public TMP_Text sombra;

    void LateUpdate()
    {
        if (texto == null || sombra == null) return;
        if (sombra.text != texto.text) sombra.text = texto.text;
        if (sombra.fontSize != texto.fontSize) sombra.fontSize = texto.fontSize;
    }
}
