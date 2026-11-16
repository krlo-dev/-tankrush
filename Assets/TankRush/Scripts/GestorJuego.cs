using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Cerebro del juego: estados (menú, jugando, pausa, fin), puntos, récord y flujo general.
public class GestorJuego : MonoBehaviour
{
    public enum Estado { Menu, Jugando, Pausa, Fin }

    public static GestorJuego I { get; private set; }

    // Al reiniciar se recarga la escena; esto hace que arranque directo sin pasar por el menú.
    static bool iniciarDirecto;

    [Header("Referencias")]
    public TanqueKawaii tanque;
    public CamaraTanque camara;
    public GeneradorOleadas generador;
    public InterfazJuego ui;
    public Efectos efectos;

    [Header("Puntos")]
    public int bonoPorOleada = 50;

    public Estado EstadoActual { get; private set; } = Estado.Menu;
    public Musica Musica { get; private set; }
    public int Puntos { get; private set; }
    public int Record { get; private set; }

    const string ClaveRecord = "TankRush_Record";

    void Awake()
    {
        I = this;
        Time.timeScale = 1f;
        Record = PlayerPrefs.GetInt(ClaveRecord, 0);
    }

    void Start()
    {
        // Sin un AudioListener encendido no se escucha nada
        if (camara != null)
        {
            AudioListener oido = camara.GetComponent<AudioListener>();
            if (oido == null) oido = camara.gameObject.AddComponent<AudioListener>();
            oido.enabled = true;
        }
        AudioListener.volume = 1f;
        AudioListener.pause = false;

        Sonidos.Inicializar(camara != null ? camara.gameObject : gameObject);
        Musica = Musica.Crear(gameObject);

        if (ui != null)
        {
            ui.ActualizarPuntos(0);
            if (tanque != null) ui.ActualizarVida(tanque.vidaMax, tanque.vidaMax);
            ui.ActualizarOleada(0, 0);
        }

        if (iniciarDirecto)
        {
            iniciarDirecto = false;
            Jugar();
        }
        else
        {
            IrAlMenu();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (EstadoActual == Estado.Jugando) Pausar(true);
            else if (EstadoActual == Estado.Pausa) Pausar(false);
        }

        if (Input.GetKeyDown(KeyCode.M) && Musica != null)
        {
            Musica.AlternarSilencio();
            if (ui != null) ui.ActualizarBotonesSonido();
        }
    }

    // ---------------- Flujo ----------------

    void IrAlMenu()
    {
        EstadoActual = Estado.Menu;
        Time.timeScale = 1f;
        if (tanque != null) tanque.ControlActivo = false;
        if (ui != null) ui.MostrarMenu(Record);
        if (Musica != null) Musica.Tocar(Musica.Pista.Menu);
        ActualizarCursor();
    }

    public void Jugar()
    {
        Puntos = 0;
        EstadoActual = Estado.Jugando;
        Time.timeScale = 1f;

        if (tanque != null)
        {
            tanque.Reiniciar();
            tanque.ControlActivo = true;
        }
        if (ui != null)
        {
            ui.OcultarMenus();
            ui.MostrarHUD(true);
            ui.ActualizarPuntos(0);
            if (tanque != null) ui.ActualizarVida(tanque.Vida, tanque.vidaMax);
        }
        if (camara != null) camara.AlEmpezarPartida();
        if (generador != null) generador.Iniciar();

        Sonidos.Reproducir(Sonidos.Oleada, 0.5f);
        if (Musica != null)
        {
            Musica.Atenuar(false);
            Musica.Tocar(Musica.Pista.Juego);
        }
        ActualizarCursor();
    }

    public void Pausar(bool pausar)
    {
        if (pausar && EstadoActual != Estado.Jugando) return;
        if (!pausar && EstadoActual != Estado.Pausa) return;

        EstadoActual = pausar ? Estado.Pausa : Estado.Jugando;
        Time.timeScale = pausar ? 0f : 1f;
        if (ui != null) ui.MostrarPausa(pausar);
        if (Musica != null) Musica.Atenuar(pausar);
        ActualizarCursor();
    }

    public void Reiniciar()
    {
        iniciarDirecto = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void VolverAlMenu()
    {
        iniciarDirecto = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------- Eventos del juego ----------------

    public void SumarPuntos(int cantidad, Vector3 posicionMundo)
    {
        if (EstadoActual != Estado.Jugando) return;
        Puntos += cantidad;
        if (ui != null)
        {
            ui.ActualizarPuntos(Puntos);
            ui.Popup(posicionMundo, "+" + cantidad, new Color(1f, 0.45f, 0.75f));
        }
    }

    public void TanqueDaniado(int vidaRestante)
    {
        if (ui != null)
        {
            ui.ActualizarVida(vidaRestante, tanque != null ? tanque.vidaMax : vidaRestante);
            ui.FlashDanio();
        }
        if (camara != null) camara.Sacudir(0.5f, 0.35f);
        Sonidos.Reproducir(Sonidos.Danio, 0.7f);

        if (vidaRestante <= 0) StartCoroutine(FinDelJuego());
    }

    public void TanqueCurado(int vida)
    {
        if (ui != null) ui.ActualizarVida(vida, tanque != null ? tanque.vidaMax : vida);
    }

    public void OleadaIniciada(int numero, int total)
    {
        if (ui != null)
        {
            ui.ActualizarOleada(numero, total);
            ui.Banner("¡Oleada " + numero + "!", numero == 1 ? "Los ratoncitos se acercan..." : total + " ratoncitos en camino");
        }
        Sonidos.Reproducir(Sonidos.Oleada, 0.6f);
    }

    public void RatonesRestantes(int oleada, int restantes)
    {
        if (ui != null) ui.ActualizarOleada(oleada, restantes);
    }

    public void OleadaCompletada(int numero)
    {
        if (EstadoActual != Estado.Jugando) return;
        int bono = bonoPorOleada * numero;
        Puntos += bono;
        bool curo = false;
        if (tanque != null && tanque.Vida < tanque.vidaMax)
        {
            tanque.Curar(1);
            curo = true;
        }
        if (ui != null)
        {
            ui.ActualizarPuntos(Puntos);
            ui.Banner("¡Oleada superada!", "+" + bono + " puntos" + (curo ? "   +1 corazón" : ""));
        }
        Sonidos.Reproducir(Sonidos.Oleada, 0.6f);
    }

    IEnumerator FinDelJuego()
    {
        if (EstadoActual != Estado.Jugando) yield break;
        EstadoActual = Estado.Fin;
        if (tanque != null) tanque.ControlActivo = false;
        if (generador != null) generador.Detener();
        if (Musica != null) Musica.Detener();
        Sonidos.Reproducir(Sonidos.Derrota, 0.7f);

        // Cámara lenta dramática antes del panel
        Time.timeScale = 0.35f;
        yield return new WaitForSecondsRealtime(1.6f);
        Time.timeScale = 0f;

        bool nuevoRecord = Puntos > Record;
        if (nuevoRecord)
        {
            Record = Puntos;
            PlayerPrefs.SetInt(ClaveRecord, Record);
            PlayerPrefs.Save();
        }

        if (ui != null)
        {
            ui.MostrarHUD(false);
            ui.MostrarFin(Puntos, generador != null ? generador.OleadaActual : 0, Record, nuevoRecord);
        }
        ActualizarCursor();
    }

    public void ActualizarCursor()
    {
        bool libre = EstadoActual != Estado.Jugando;
        if (libre)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (camara != null && camara.Modo == CamaraTanque.ModoCamara.Aerea)
        {
            // En vista aérea se apunta con el cursor; se dibuja una mira propia
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public bool Jugando => EstadoActual == Estado.Jugando;
}
