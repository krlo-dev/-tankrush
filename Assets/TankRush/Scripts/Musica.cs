using UnityEngine;

// Música de fondo generada por código (estilo chiptune / caja musical kawaii).
// Dos pistas en bucle: una tranquila para el menú y otra con batería para la partida.
// Tecla M: silenciar o activar la música (se recuerda entre partidas).
public class Musica : MonoBehaviour
{
    public enum Pista { Ninguna, Menu, Juego }

    [Range(0f, 1f)] public float volumen = 0.35f;

    AudioSource fuente;
    AudioClip clipMenu, clipJuego;
    Pista actual = Pista.Ninguna;
    Pista siguiente = Pista.Ninguna;
    bool cambiando;
    bool atenuada;
    bool silenciada;
    float fundido;

    const int FS = 44100;
    const string ClaveSilencio = "TankRush_MusicaSilenciada";

    public bool Silenciada => silenciada;

    public static Musica Crear(GameObject anfitrion)
    {
        Musica m = anfitrion.GetComponent<Musica>();
        if (m == null) m = anfitrion.AddComponent<Musica>();
        return m;
    }

    void Awake()
    {
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.loop = true;
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        fuente.volume = 0f;
        fuente.priority = 0;
        silenciada = PlayerPrefs.GetInt(ClaveSilencio, 0) == 1;

        clipMenu = ComponerMenu();
        clipJuego = ComponerJuego();
    }

    public void Tocar(Pista p)
    {
        if (p == actual && !cambiando && fuente.isPlaying) return;
        siguiente = p;
        cambiando = true;
    }

    public void Detener() => Tocar(Pista.Ninguna);

    public void Atenuar(bool a) => atenuada = a;

    public void AlternarSilencio()
    {
        silenciada = !silenciada;
        PlayerPrefs.SetInt(ClaveSilencio, silenciada ? 1 : 0);
        PlayerPrefs.Save();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (cambiando)
        {
            fundido = Mathf.MoveTowards(fundido, 0f, dt * 2.5f);
            if (fundido <= 0f || !fuente.isPlaying)
            {
                actual = siguiente;
                cambiando = false;
                fundido = 0f;
                if (actual == Pista.Ninguna) fuente.Stop();
                else
                {
                    fuente.clip = actual == Pista.Menu ? clipMenu : clipJuego;
                    fuente.Play();
                }
            }
        }
        else
        {
            fundido = Mathf.MoveTowards(fundido, 1f, dt * 1.2f);
        }

        float objetivo = silenciada ? 0f : volumen * (atenuada ? 0.35f : 1f);
        fuente.volume = objetivo * fundido;
    }

    // ============================================================ composición

    // Melodías: pares (nota MIDI, duración en corcheas). -1 = silencio.
    static readonly int[] MelodiaMenu =
    {
        76,2, 79,2, 84,2, 79,2,
        81,3, 79,1, 76,2, 72,2,
        77,2, 81,2, 84,3, 81,1,
        79,4, 74,2, 71,2,
        76,2, 79,2, 84,2, 86,2,
        88,3, 86,1, 84,2, 81,2,
        81,2, 84,2, 79,2, 77,2,
        79,3, 74,1, 72,4,
    };

    static readonly int[] MelodiaJuego =
    {
        81,1, 84,1, 88,2, 84,1, 81,1, 79,2,
        77,1, 81,1, 84,2, 81,2, 77,2,
        76,1, 79,1, 84,2, 79,1, 76,1, 72,2,
        74,2, 79,2, 83,2, 86,2,
        81,1, 84,1, 88,2, 91,2, 88,2,
        89,2, 88,1, 84,1, 81,2, 77,2,
        79,1, 84,1, 88,1, 84,1, 79,2, 76,2,
        74,2, 83,2, 86,4,
    };

    // Acordes por compás: (raíz MIDI, menor? 1 : 0)
    static readonly int[] AcordesMenu = { 60, 0, 57, 1, 53, 0, 55, 0, 60, 0, 57, 1, 53, 0, 55, 0 };
    static readonly int[] AcordesJuego = { 57, 1, 53, 0, 60, 0, 55, 0, 57, 1, 53, 0, 60, 0, 55, 0 };

    AudioClip ComponerMenu()
    {
        const float bpm = 96f;
        float negra = 60f / bpm, corchea = negra * 0.5f, semi = negra * 0.25f;
        int compases = AcordesMenu.Length / 2;
        float[] buf = new float[Mathf.RoundToInt(compases * 4 * negra * FS)];

        for (int c = 0; c < compases; c++)
        {
            int raiz = AcordesMenu[c * 2];
            bool menor = AcordesMenu[c * 2 + 1] == 1;
            int[] tonos = { raiz, raiz + (menor ? 3 : 4), raiz + 7, raiz + 12 };
            float t0 = c * 4 * negra;

            // Caja musical: arpegio en semicorcheas
            int[] patron = { 0, 1, 2, 3, 2, 1, 2, 3, 0, 1, 2, 3, 2, 3, 2, 1 };
            for (int i = 0; i < 16; i++)
                Tono(buf, t0 + i * semi, 0.6f, Hz(tonos[patron[i]] + 12), 0, 0.07f);

            // Bajo suave en blancas
            Tono(buf, t0, negra * 1.9f, Hz(raiz - 24), 2, 0.2f);
            Tono(buf, t0 + 2 * negra, negra * 1.9f, Hz(raiz - 17), 2, 0.16f);

            // Shaker muy suave a contratiempo
            for (int b = 0; b < 4; b++) Ruido(buf, t0 + b * negra + corchea, 0.025f, 0.03f, true);
        }

        Melodia(buf, MelodiaMenu, corchea, 1, 0.15f);
        return Finalizar("MusicaMenu", buf);
    }

    AudioClip ComponerJuego()
    {
        const float bpm = 138f;
        float negra = 60f / bpm, corchea = negra * 0.5f;
        int compases = AcordesJuego.Length / 2;
        float[] buf = new float[Mathf.RoundToInt(compases * 4 * negra * FS)];

        for (int c = 0; c < compases; c++)
        {
            int raiz = AcordesJuego[c * 2];
            bool menor = AcordesJuego[c * 2 + 1] == 1;
            int[] tonos = { raiz, raiz + (menor ? 3 : 4), raiz + 7, raiz + 12 };
            float t0 = c * 4 * negra;

            // Arpegio en corcheas
            int[] patron = { 0, 2, 1, 3, 0, 2, 1, 3 };
            for (int i = 0; i < 8; i++)
                Tono(buf, t0 + i * corchea, 0.35f, Hz(tonos[patron[i]] + 12), 0, 0.05f);

            // Bajo saltarín en octavas
            for (int i = 0; i < 8; i++)
                Tono(buf, t0 + i * corchea, corchea * 0.85f, Hz(raiz - 24 + (i % 2 == 1 ? 12 : 0)), 2, 0.2f);

            // Batería
            for (int b = 0; b < 4; b++)
            {
                float tb = t0 + b * negra;
                if (b == 0 || b == 2) Bombo(buf, tb, 0.5f);
                if (b == 1) Bombo(buf, tb + corchea, 0.35f);
                if (b == 1 || b == 3) Ruido(buf, tb, 0.12f, 0.16f, false);
                Ruido(buf, tb, 0.03f, 0.05f, true);
                Ruido(buf, tb + corchea, 0.025f, 0.035f, true);
            }
        }

        Melodia(buf, MelodiaJuego, corchea, 1, 0.14f);
        return Finalizar("MusicaJuego", buf);
    }

    // ============================================================ síntesis

    static float Hz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

    static void Melodia(float[] buf, int[] notas, float corchea, int tipo, float vol)
    {
        float t = 0f;
        for (int i = 0; i + 1 < notas.Length; i += 2)
        {
            float dur = notas[i + 1] * corchea;
            if (notas[i] >= 0) Tono(buf, t, dur * 0.92f, Hz(notas[i]), tipo, vol);
            t += dur;
        }
    }

    // tipo 0 = caja musical (seno con caída), 1 = melodía (cuadrada suave con vibrato), 2 = bajo (triangular)
    static void Tono(float[] buf, float inicio, float dur, float f, int tipo, float vol)
    {
        int n = buf.Length;
        int i0 = Mathf.RoundToInt(inicio * FS);
        int largo = Mathf.RoundToInt((dur + (tipo == 0 ? 0.5f : 0.05f)) * FS);
        float fase = 0f;
        float ataque = tipo == 0 ? 0.003f : 0.01f;
        for (int k = 0; k < largo; k++)
        {
            float t = (float)k / FS;
            float vib = tipo == 1 ? 1f + 0.004f * Mathf.Sin(2f * Mathf.PI * 5.5f * t) * Mathf.Clamp01((t - 0.12f) * 4f) : 1f;
            fase += f * vib / FS;
            fase -= Mathf.Floor(fase);

            float s;
            switch (tipo)
            {
                case 0:
                    s = Mathf.Sin(2f * Mathf.PI * fase) + 0.25f * Mathf.Sin(4f * Mathf.PI * fase);
                    break;
                case 1:
                    float cuadrada = fase < 0.5f ? 1f : -1f;
                    float tri = 4f * Mathf.Abs(fase - 0.5f) - 1f;
                    s = cuadrada * 0.35f + tri * 0.65f;
                    break;
                default:
                    s = 4f * Mathf.Abs(fase - 0.5f) - 1f;
                    break;
            }

            float env;
            if (t < ataque) env = t / ataque;
            else if (tipo == 0) env = Mathf.Exp(-(t - ataque) / 0.28f);
            else
            {
                env = Mathf.Lerp(1f, 0.7f, Mathf.Clamp01((t - ataque) / 0.15f));
                if (t > dur) env *= Mathf.Clamp01(1f - (t - dur) / 0.05f);
            }

            buf[(i0 + k) % n] += s * env * vol;
        }
    }

    static void Bombo(float[] buf, float inicio, float vol)
    {
        int n = buf.Length;
        int i0 = Mathf.RoundToInt(inicio * FS);
        int largo = Mathf.RoundToInt(0.18f * FS);
        float fase = 0f;
        for (int k = 0; k < largo; k++)
        {
            float t = (float)k / FS;
            float f = Mathf.Lerp(150f, 45f, Mathf.Clamp01(t / 0.12f));
            fase += f / FS;
            buf[(i0 + k) % n] += Mathf.Sin(2f * Mathf.PI * fase) * Mathf.Exp(-t / 0.06f) * vol;
        }
    }

    static uint semilla = 12345u;

    static float Aleatorio()
    {
        semilla = semilla * 1664525u + 1013904223u;
        return (semilla >> 8) / 8388608f - 1f;
    }

    // Ruido: agudo = platillo/shaker, grave = caja
    static void Ruido(float[] buf, float inicio, float decay, float vol, bool agudo)
    {
        int n = buf.Length;
        int i0 = Mathf.RoundToInt(inicio * FS);
        int largo = Mathf.RoundToInt(decay * 5f * FS);
        float previo = 0f, fase = 0f;
        for (int k = 0; k < largo; k++)
        {
            float t = (float)k / FS;
            float r = Aleatorio();
            float s;
            if (agudo)
            {
                s = r - previo;   // filtro pasa altos sencillo
                previo = r;
            }
            else
            {
                fase += 190f / FS;
                s = r * 0.7f + Mathf.Sin(2f * Mathf.PI * fase) * 0.5f;
            }
            buf[(i0 + k) % n] += s * Mathf.Exp(-t / decay) * vol;
        }
    }

    static AudioClip Finalizar(string nombre, float[] buf)
    {
        float pico = 0.0001f;
        for (int i = 0; i < buf.Length; i++) pico = Mathf.Max(pico, Mathf.Abs(buf[i]));
        float g = 0.85f / pico;
        for (int i = 0; i < buf.Length; i++)
        {
            float x = buf[i] * g;
            buf[i] = x / (1f + Mathf.Abs(x) * 0.15f);   // saturación suave
        }
        AudioClip clip = AudioClip.Create(nombre, buf.Length, 1, FS, false);
        clip.SetData(buf, 0);
        return clip;
    }
}
