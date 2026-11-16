using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Genera oleadas de ratoncitos alrededor del tanque. Cada oleada trae más y más rápidos.
public class GeneradorOleadas : MonoBehaviour
{
    [Header("Prefabs")]
    public Raton ratonNormal;
    public Raton ratonRapido;
    public Raton ratonGordito;

    [Header("Referencias")]
    public TanqueKawaii tanque;

    [Header("Dónde aparecen")]
    public Vector3 centroArena = Vector3.zero;
    public float radioArena = 90f;
    public float distanciaMin = 30f;     // desde el tanque
    public float distanciaMax = 45f;

    [Header("Dificultad")]
    public int ratonesPrimeraOleada = 6;
    public int ratonesExtraPorOleada = 4;
    public int maximoSimultaneos = 35;
    public float intervaloInicial = 1.1f;
    public float intervaloMinimo = 0.3f;
    public float pausaEntreOleadas = 3f;

    public int OleadaActual { get; private set; }

    readonly List<Raton> vivos = new List<Raton>();
    int porGenerar;
    int restantes;
    Coroutine ciclo;

    public void Iniciar()
    {
        Detener();
        OleadaActual = 0;
        ciclo = StartCoroutine(Ciclo());
    }

    public void Detener()
    {
        if (ciclo != null) StopCoroutine(ciclo);
        ciclo = null;
    }

    IEnumerator Ciclo()
    {
        yield return new WaitForSeconds(1f);
        while (true)
        {
            OleadaActual++;
            int n = OleadaActual;
            int total = ratonesPrimeraOleada + ratonesExtraPorOleada * (n - 1);
            porGenerar = total;
            restantes = total;
            if (GestorJuego.I != null) GestorJuego.I.OleadaIniciada(n, total);

            yield return new WaitForSeconds(2f);

            float intervalo = Mathf.Max(intervaloMinimo, intervaloInicial * Mathf.Pow(0.9f, n - 1));
            float multVel = Mathf.Min(1.8f, 1f + 0.06f * (n - 1));

            while (porGenerar > 0)
            {
                LimpiarMuertos();
                if (vivos.Count < maximoSimultaneos)
                {
                    // A veces salen en grupitos
                    int grupo = Random.value < 0.25f + 0.03f * n ? Random.Range(2, 4) : 1;
                    Vector3 basePos = PosicionAparicion();
                    for (int g = 0; g < grupo && porGenerar > 0; g++)
                    {
                        Vector2 off = Random.insideUnitCircle * (tanque != null ? tanque.tamanio * 0.4f : 3f);
                        Generar(ElegirTipo(n), basePos + new Vector3(off.x, 0f, off.y), multVel);
                        porGenerar--;
                    }
                }
                yield return new WaitForSeconds(intervalo);
            }

            while (restantes > 0)
            {
                LimpiarMuertos();
                if (vivos.Count == 0) restantes = 0;
                yield return null;
            }

            if (GestorJuego.I != null) GestorJuego.I.OleadaCompletada(n);
            yield return new WaitForSeconds(pausaEntreOleadas);
        }
    }

    Raton ElegirTipo(int oleada)
    {
        float r = Random.value;
        float probGordito = oleada >= 3 ? Mathf.Min(0.25f, 0.08f + 0.03f * (oleada - 3)) : 0f;
        float probRapido = oleada >= 2 ? Mathf.Min(0.35f, 0.15f + 0.04f * (oleada - 2)) : 0f;
        if (r < probGordito && ratonGordito != null) return ratonGordito;
        if (r < probGordito + probRapido && ratonRapido != null) return ratonRapido;
        return ratonNormal;
    }

    Vector3 PosicionAparicion()
    {
        Vector3 t = tanque != null ? tanque.transform.position : centroArena;
        Vector3 mejor = centroArena;
        float margen = radioArena * 0.95f;

        for (int intento = 0; intento < 12; intento++)
        {
            float ang = Random.value * Mathf.PI * 2f;
            float d = Random.Range(distanciaMin, distanciaMax);
            Vector3 p = t + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * d;
            Vector3 off = p - centroArena;
            off.y = 0f;
            if (off.magnitude > margen) p = centroArena + off.normalized * margen;
            p.y = centroArena.y;
            mejor = p;
            Vector3 aTanque = p - t;
            aTanque.y = 0f;
            if (aTanque.magnitude > distanciaMin * 0.6f) break;
        }
        return mejor;
    }

    void Generar(Raton prefab, Vector3 pos, float multVel)
    {
        if (prefab == null) return;
        Vector3 haciaTanque = tanque != null ? tanque.transform.position - pos : Vector3.forward;
        haciaTanque.y = 0f;
        Raton r = Instantiate(prefab, pos, Quaternion.identity);
        r.Configurar(tanque, this, multVel);
        Transform vis = r.transform.Find("Visual");
        if (vis != null && haciaTanque.sqrMagnitude > 0.01f) vis.rotation = Quaternion.LookRotation(haciaTanque);
        vivos.Add(r);
        if (Efectos.I != null) Efectos.I.Puff(pos + Vector3.up * r.radio);
    }

    public void RatonEliminado(Raton r)
    {
        vivos.Remove(r);
        restantes = Mathf.Max(0, restantes - 1);
        if (GestorJuego.I != null) GestorJuego.I.RatonesRestantes(OleadaActual, restantes);
    }

    void LimpiarMuertos()
    {
        vivos.RemoveAll(x => x == null);
    }
}
