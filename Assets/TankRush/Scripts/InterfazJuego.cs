using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Maneja todo lo que se ve en pantalla: menú, HUD, pausa, fin de juego, banners y popups.
// Las referencias de la UI se asignan desde el Inspector.
public class InterfazJuego : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelControles;
    public GameObject panelPausa;
    public GameObject panelFin;
    public GameObject hud;

    [Header("Botones")]
    public Button botonJugar, botonControles, botonSalir, botonVolverControles;
    public Button botonContinuar, botonReiniciarPausa, botonMenuPausa;
    public Button botonReintentar, botonMenuFin;
    public Button botonMusica, botonEfectos, botonMusicaPausa, botonEfectosPausa;

    [Header("Textos")]
    public TextMeshProUGUI textoRecordMenu;
    public GameObject fondoRecord;     // pastilla detrás del récord (se oculta si no hay récord)
    public TextMeshProUGUI textoPuntos;
    public TextMeshProUGUI textoOleada;
    public TextMeshProUGUI textoRatones;
    public TextMeshProUGUI textoModoCamara;
    public TextMeshProUGUI textoBannerTitulo;
    public TextMeshProUGUI textoBannerSub;
    public TextMeshProUGUI textoFinPuntos;
    public TextMeshProUGUI textoFinDetalle;
    public TextMeshProUGUI plantillaPopup;
    public TMP_Text[] textosMusica;
    public TMP_Text[] textosEfectos;

    [Header("Otros")]
    public Image[] corazones;
    public Color colorCorazonLleno = new Color(1f, 0.35f, 0.6f);
    public Color colorCorazonVacio = new Color(1f, 1f, 1f, 0.35f);
    public RectTransform mira;
    public CanvasGroup grupoBanner;
    public Image flashDanio;
    public Camera camara;

    Coroutine rutinaBanner;
    float flash;
    bool miraLibre;
    int vidaPrevia = -1;

    void Awake()
    {
        Escuchar(botonJugar, () => GestorJuego.I.Jugar());
        Escuchar(botonControles, () => { panelMenu.SetActive(false); panelControles.SetActive(true); });
        Escuchar(botonVolverControles, () => { panelControles.SetActive(false); panelMenu.SetActive(true); });
        Escuchar(botonSalir, () => GestorJuego.I.Salir());
        Escuchar(botonContinuar, () => GestorJuego.I.Pausar(false));
        Escuchar(botonReiniciarPausa, () => GestorJuego.I.Reiniciar());
        Escuchar(botonMenuPausa, () => GestorJuego.I.VolverAlMenu());
        Escuchar(botonReintentar, () => GestorJuego.I.Reiniciar());
        Escuchar(botonMenuFin, () => GestorJuego.I.VolverAlMenu());
        UnityEngine.Events.UnityAction musica = () =>
        {
            if (GestorJuego.I != null && GestorJuego.I.Musica != null) GestorJuego.I.Musica.AlternarSilencio();
            ActualizarBotonesSonido();
        };
        UnityEngine.Events.UnityAction efectos = () =>
        {
            Sonidos.AlternarSilencio();
            ActualizarBotonesSonido();
        };
        Escuchar(botonMusica, musica);
        Escuchar(botonMusicaPausa, musica);
        Escuchar(botonEfectos, efectos);
        Escuchar(botonEfectosPausa, efectos);

        if (plantillaPopup != null) plantillaPopup.gameObject.SetActive(false);
        if (grupoBanner != null) grupoBanner.alpha = 0f;
        if (flashDanio != null) flashDanio.color = new Color(1f, 0.3f, 0.55f, 0f);
        if (camara == null) camara = Camera.main;
    }

    static void Escuchar(Button b, UnityEngine.Events.UnityAction accion)
    {
        if (b != null) b.onClick.AddListener(accion);
    }

    void Update()
    {
        if (flashDanio != null && flash > 0f)
        {
            flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 2.5f);
            Color c = flashDanio.color;
            c.a = flash * 0.45f;
            flashDanio.color = c;
        }

        if (mira != null && !miraLibre)
            mira.anchoredPosition = Vector2.Lerp(mira.anchoredPosition, Vector2.zero, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
    }

    void Start()
    {
        ActualizarBotonesSonido();
    }

    public void ActualizarBotonesSonido()
    {
        bool musicaOn = GestorJuego.I == null || GestorJuego.I.Musica == null || !GestorJuego.I.Musica.Silenciada;
        bool efectosOn = !Sonidos.Silenciados;
        PonerTexto(textosMusica, "Música: " + (musicaOn ? "Sí" : "No"));
        PonerTexto(textosEfectos, "Efectos: " + (efectosOn ? "Sí" : "No"));
        Atenuar(botonMusica, musicaOn);
        Atenuar(botonMusicaPausa, musicaOn);
        Atenuar(botonEfectos, efectosOn);
        Atenuar(botonEfectosPausa, efectosOn);
    }

    static void PonerTexto(TMP_Text[] textos, string valor)
    {
        if (textos == null) return;
        foreach (TMP_Text t in textos) if (t != null) t.text = valor;
    }

    // Colores originales de cada botón de sonido y su base (para restaurarlos al encender)
    readonly System.Collections.Generic.Dictionary<Button, Color[]> coloresOriginales =
        new System.Collections.Generic.Dictionary<Button, Color[]>();

    void Atenuar(Button b, bool encendido)
    {
        if (b == null) return;
        Image img = b.targetGraphic as Image;
        Transform t = b.transform.parent != null ? b.transform.parent.Find(b.name + "_Base") : null;
        Image baseImg = t != null ? t.GetComponent<Image>() : null;

        if (!coloresOriginales.TryGetValue(b, out Color[] orig))
        {
            orig = new[] { img != null ? img.color : Color.white, baseImg != null ? baseImg.color : Color.white };
            coloresOriginales[b] = orig;
        }

        // Apagado = gris lavanda opaco (sin transparencias que dejen ver la base)
        Color gris = new Color(0.78f, 0.75f, 0.82f);
        if (img != null) img.color = encendido ? orig[0] : Color.Lerp(orig[0], gris, 0.75f);
        if (baseImg != null) baseImg.color = encendido ? orig[1] : Color.Lerp(orig[1], gris * 0.8f, 0.75f);
    }

    // ---------------- Paneles ----------------

    public void MostrarMenu(int record)
    {
        OcultarMenus();
        MostrarHUD(false);
        if (panelMenu != null) panelMenu.SetActive(true);
        if (textoRecordMenu != null) textoRecordMenu.text = record > 0 ? "Récord: " + record : "";
        if (fondoRecord != null) fondoRecord.SetActive(record > 0);
        ActualizarBotonesSonido();
    }

    public void OcultarMenus()
    {
        if (panelMenu != null) panelMenu.SetActive(false);
        if (panelControles != null) panelControles.SetActive(false);
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelFin != null) panelFin.SetActive(false);
    }

    public void MostrarHUD(bool mostrar)
    {
        if (hud != null) hud.SetActive(mostrar);
    }

    public void MostrarPausa(bool mostrar)
    {
        if (panelPausa != null) panelPausa.SetActive(mostrar);
        if (mostrar) ActualizarBotonesSonido();
    }

    public void MostrarFin(int puntos, int oleada, int record, bool nuevoRecord)
    {
        OcultarMenus();
        if (panelFin != null) panelFin.SetActive(true);
        if (textoFinPuntos != null) textoFinPuntos.text = puntos + " puntos";
        if (textoFinDetalle != null)
        {
            textoFinDetalle.text = "Llegaste a la oleada " + oleada + "\n" +
                (nuevoRecord ? "<color=#FF4FA3>¡Nuevo récord!</color>" : "Récord: " + record);
        }
    }

    // ---------------- HUD ----------------

    public void ActualizarPuntos(int puntos)
    {
        if (textoPuntos != null) textoPuntos.text = puntos.ToString();
    }

    public void ActualizarVida(int vida, int max)
    {
        if (corazones == null) return;
        for (int i = 0; i < corazones.Length; i++)
        {
            if (corazones[i] == null) continue;
            bool visible = i < max;
            corazones[i].gameObject.SetActive(visible);
            corazones[i].color = i < vida ? colorCorazonLleno : colorCorazonVacio;
        }
        if (vidaPrevia >= 0 && vida > vidaPrevia && vida - 1 < corazones.Length && vida > 0)
            StartCoroutine(Latido(corazones[vida - 1].rectTransform));
        vidaPrevia = vida;
    }

    public void ActualizarOleada(int oleada, int restantes)
    {
        if (textoOleada != null) textoOleada.text = oleada > 0 ? "Oleada " + oleada : "";
        if (textoRatones != null) textoRatones.text = oleada > 0 ? restantes + " ratoncitos" : "";
    }

    public void ModoCamara(string nombre, bool miraSigueCursor)
    {
        if (textoModoCamara != null) textoModoCamara.text = "[C] Cámara: " + nombre;
        miraLibre = miraSigueCursor;
    }

    public void MoverMira(Vector2 posPantalla)
    {
        if (mira == null) return;
        RectTransform padre = mira.parent as RectTransform;
        if (padre != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, posPantalla, null, out Vector2 local))
            mira.anchoredPosition = local;
    }

    public void FlashDanio()
    {
        flash = 1f;
    }

    public void Banner(string titulo, string subtitulo)
    {
        if (grupoBanner == null) return;
        if (textoBannerTitulo != null) textoBannerTitulo.text = titulo;
        if (textoBannerSub != null) textoBannerSub.text = subtitulo;
        if (rutinaBanner != null) StopCoroutine(rutinaBanner);
        rutinaBanner = StartCoroutine(AnimarBanner());
    }

    IEnumerator AnimarBanner()
    {
        RectTransform rt = grupoBanner.transform as RectTransform;
        float t = 0f;
        while (t < 2.6f)
        {
            t += Time.unscaledDeltaTime;
            float a = t < 0.25f ? t / 0.25f : (t > 2.1f ? 1f - (t - 2.1f) / 0.5f : 1f);
            grupoBanner.alpha = Mathf.Clamp01(a);
            float s = t < 0.25f ? Mathf.Lerp(0.6f, 1.08f, t / 0.25f) : Mathf.Lerp(1.08f, 1f, Mathf.Clamp01((t - 0.25f) / 0.2f));
            if (rt != null) rt.localScale = Vector3.one * s;
            yield return null;
        }
        grupoBanner.alpha = 0f;
    }

    IEnumerator Latido(RectTransform rt)
    {
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            rt.localScale = Vector3.one * (1f + Mathf.Sin(t / 0.5f * Mathf.PI) * 0.5f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    public void Popup(Vector3 posMundo, string texto, Color color)
    {
        if (plantillaPopup == null || camara == null) return;
        Vector3 p = camara.WorldToScreenPoint(posMundo);
        if (p.z < 0f) return;
        TextMeshProUGUI t = Instantiate(plantillaPopup, plantillaPopup.transform.parent);
        t.gameObject.SetActive(true);
        t.text = texto;
        t.color = color;
        t.transform.position = new Vector3(p.x, p.y, 0f);
        StartCoroutine(AnimarPopup(t));
    }

    IEnumerator AnimarPopup(TextMeshProUGUI t)
    {
        float dur = 0.9f, k = 0f;
        Vector3 inicio = t.transform.position;
        Color c = t.color;
        while (k < dur && t != null)
        {
            k += Time.unscaledDeltaTime;
            float u = k / dur;
            t.transform.position = inicio + Vector3.up * (60f * u);
            t.transform.localScale = Vector3.one * (u < 0.15f ? Mathf.Lerp(0.5f, 1.2f, u / 0.15f) : Mathf.Lerp(1.2f, 1f, (u - 0.15f) / 0.85f));
            c.a = 1f - Mathf.Clamp01((u - 0.5f) / 0.5f);
            t.color = c;
            yield return null;
        }
        if (t != null) Destroy(t.gameObject);
    }
}
