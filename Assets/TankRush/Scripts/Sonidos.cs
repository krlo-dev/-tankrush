using UnityEngine;

// Sonidos kawaii generados por código (no hace falta importar audios).
public static class Sonidos
{
    public static AudioClip Disparo, Pop, Golpe, Danio, Oleada, Click, Derrota;
    static AudioSource fuente;
    const string ClaveSilencio = "TankRush_EfectosSilenciados";
    public static bool Silenciados { get; private set; }

    public static void AlternarSilencio()
    {
        Silenciados = !Silenciados;
        PlayerPrefs.SetInt(ClaveSilencio, Silenciados ? 1 : 0);
        PlayerPrefs.Save();
    }
    const int Frecuencia = 44100;

    public static void Inicializar(GameObject anfitrion)
    {
        if (anfitrion == null) return;
        Silenciados = PlayerPrefs.GetInt(ClaveSilencio, 0) == 1;
        fuente = anfitrion.GetComponent<AudioSource>();
        if (fuente == null) fuente = anfitrion.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;

        if (Disparo != null) return;
        Disparo = Barrido("pew", 0.16f, 900f, 260f, 0.5f, true);
        Pop = Barrido("pop", 0.12f, 500f, 1400f, 0.6f, false);
        Golpe = Barrido("golpe", 0.07f, 700f, 900f, 0.4f, false);
        Danio = Barrido("danio", 0.3f, 300f, 90f, 0.6f, true);
        Click = Barrido("click", 0.05f, 1200f, 1600f, 0.35f, false);
        Oleada = Arpegio("oleada", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.09f);
        Derrota = Arpegio("derrota", new[] { 783.99f, 659.25f, 523.25f, 392f, 329.63f }, 0.16f);
    }

    public static void Reproducir(AudioClip clip, float volumen = 1f, float variacionTono = 0f)
    {
        if (clip == null || fuente == null || Silenciados) return;
        fuente.pitch = 1f + Random.Range(-variacionTono, variacionTono);
        fuente.PlayOneShot(clip, volumen);
    }

    static AudioClip Barrido(string nombre, float dur, float f0, float f1, float vol, bool cuadrada)
    {
        int n = Mathf.CeilToInt(dur * Frecuencia);
        float[] datos = new float[n];
        float fase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(f0, f1, t * t * (3f - 2f * t));
            fase += 2f * Mathf.PI * f / Frecuencia;
            float s = Mathf.Sin(fase);
            if (cuadrada) s = Mathf.Clamp(s * 2.5f, -1f, 1f) * 0.6f + Mathf.Sin(fase * 0.5f) * 0.4f;
            float env = Mathf.Min(1f, t * 40f) * Mathf.Pow(1f - t, 2f);
            datos[i] = s * env * vol;
        }
        AudioClip c = AudioClip.Create(nombre, n, 1, Frecuencia, false);
        c.SetData(datos, 0);
        return c;
    }

    static AudioClip Arpegio(string nombre, float[] notas, float durNota)
    {
        int porNota = Mathf.CeilToInt(durNota * Frecuencia);
        int n = porNota * notas.Length + porNota * 2;
        float[] datos = new float[n];
        for (int k = 0; k < notas.Length; k++)
        {
            float fase = 0f;
            int inicio = k * porNota;
            int largo = porNota * 3;
            for (int i = 0; i < largo && inicio + i < n; i++)
            {
                float t = (float)i / largo;
                fase += 2f * Mathf.PI * notas[k] / Frecuencia;
                float env = Mathf.Min(1f, t * 60f) * Mathf.Pow(1f - t, 3f);
                datos[inicio + i] += (Mathf.Sin(fase) + 0.3f * Mathf.Sin(fase * 2f)) * env * 0.35f;
            }
        }
        AudioClip c = AudioClip.Create(nombre, n, 1, Frecuencia, false);
        c.SetData(datos, 0);
        return c;
    }
}
