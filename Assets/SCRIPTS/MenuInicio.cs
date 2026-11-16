using UnityEngine;

public class MenuInicio : MonoBehaviour
{
    void Start()
    {
        Time.timeScale = 0f; // pausa el juego mientras el menú está visible
    }

    public void Jugar()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false); // oculta este mismo panel
    }
}   