using UnityEngine;

// Ratoncito enemigo: camina dando saltitos hacia el tanque. Si lo toca, le quita un corazón.
[RequireComponent(typeof(Rigidbody))]
public class Raton : MonoBehaviour
{
    public enum Tipo { Normal, Rapido, Gordito }

    public Tipo tipo = Tipo.Normal;
    public float vidaMax = 1f;
    public float velocidad = 4f;
    public int puntos = 10;
    public int danioAlTanque = 1;
    public float radio = 0.5f;            // radio del collider
    public float frecuenciaSalto = 7f;
    public float alturaSalto = 0.15f;      // relativa al radio

    [HideInInspector] public GeneradorOleadas generador;
    [HideInInspector] public TanqueKawaii objetivo;

    Rigidbody rb;
    Transform visual, orejaI, orejaD, cola;
    Quaternion orejaIBase, orejaDBase, colaBase;
    Vector3 escalaVisual;
    float vida;
    float fase;
    float aparicion;      // animación de entrada (0 a 1)
    float golpe;          // animación al recibir daño
    float zigzag;
    bool muerto;

    static PhysicsMaterial sinFriccion;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (sinFriccion == null)
        {
            sinFriccion = new PhysicsMaterial("RatonSinFriccion")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f
            };
        }
        Collider col = GetComponent<Collider>();
        if (col != null) col.sharedMaterial = sinFriccion;

        visual = transform.Find("Visual");
        if (visual != null)
        {
            escalaVisual = visual.localScale;
            orejaI = visual.Find("OrejaI");
            orejaD = visual.Find("OrejaD");
            cola = visual.Find("Cola");
            if (orejaI) orejaIBase = orejaI.localRotation;
            if (orejaD) orejaDBase = orejaD.localRotation;
            if (cola) colaBase = cola.localRotation;
        }

        vida = vidaMax;
        fase = Random.value * 10f;
        zigzag = Random.Range(0f, 100f);
    }

    public void Configurar(TanqueKawaii tanque, GeneradorOleadas gen, float multVelocidad)
    {
        objetivo = tanque;
        generador = gen;
        velocidad *= multVelocidad;
    }

    void FixedUpdate()
    {
        if (muerto || objetivo == null) return;

        Vector3 dir = objetivo.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;
        dir.Normalize();

        // Los rápidos van en zigzag para que sea más difícil atinarles
        if (tipo == Tipo.Rapido)
        {
            Vector3 lado = Vector3.Cross(Vector3.up, dir);
            dir = (dir + lado * Mathf.Sin(Time.time * 3f + zigzag) * 0.6f).normalized;
        }

        float factorEntrada = Mathf.Clamp01(aparicion * 1.5f);
        Vector3 v = dir * velocidad * factorEntrada;
        rb.linearVelocity = new Vector3(v.x, rb.linearVelocity.y, v.z);
    }

    void Update()
    {
        if (muerto) return;
        float dt = Time.deltaTime;
        aparicion = Mathf.MoveTowards(aparicion, 1f, dt * 2.5f);
        golpe = Mathf.MoveTowards(golpe, 0f, dt * 6f);

        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        float rapidez = v.magnitude;

        if (visual != null)
        {
            // Mirar hacia donde camina
            if (rapidez > 0.05f)
            {
                Quaternion mirar = Quaternion.LookRotation(v / rapidez, Vector3.up);
                visual.rotation = Quaternion.Slerp(visual.rotation, mirar, 1f - Mathf.Exp(-12f * dt));
            }

            // Saltitos con aplastamiento
            fase += dt * frecuenciaSalto * Mathf.Lerp(0.6f, 1.4f, rapidez / Mathf.Max(0.01f, velocidad));
            float s = Mathf.Abs(Mathf.Sin(fase));
            visual.localPosition = Vector3.up * s * alturaSalto * radio * 2f;
            float aplasta = 1f - (1f - s) * 0.12f;
            float entrada = Mathf.SmoothStep(0f, 1f, aparicion);
            float pulso = 1f + golpe * 0.35f;
            visual.localScale = new Vector3(
                escalaVisual.x * (2f - aplasta) * entrada * pulso,
                escalaVisual.y * aplasta * entrada * (1f + golpe * 0.15f),
                escalaVisual.z * (2f - aplasta) * entrada * pulso);

            if (orejaI) orejaI.localRotation = orejaIBase * Quaternion.Euler(0f, 0f, Mathf.Sin(fase * 2f) * 12f);
            if (orejaD) orejaD.localRotation = orejaDBase * Quaternion.Euler(0f, 0f, -Mathf.Sin(fase * 2f) * 12f);
            if (cola) cola.localRotation = colaBase * Quaternion.Euler(0f, Mathf.Sin(fase * 1.3f) * 25f, 0f);
        }

        // ¿Tocó al tanque?
        if (objetivo != null && objetivo.isActiveAndEnabled)
        {
            Vector3 centro = transform.position + Vector3.up * radio;
            bool toca;
            if (objetivo.casco != null && objetivo.casco.enabled && objetivo.casco.gameObject.activeInHierarchy)
            {
                Vector3 cercano = objetivo.casco.ClosestPoint(centro);
                toca = (cercano - centro).sqrMagnitude < (radio * 1.25f) * (radio * 1.25f);
            }
            else
            {
                Vector3 d = objetivo.transform.position - transform.position;
                d.y = 0f;
                toca = d.magnitude < objetivo.tamanio * 0.5f + radio;
            }
            if (toca) Morder();
        }
    }

    void Morder()
    {
        if (muerto) return;
        muerto = true;
        objetivo.RecibirDanio(danioAlTanque);
        if (Efectos.I != null) Efectos.I.Puff(transform.position + Vector3.up * radio);
        Sonidos.Reproducir(Sonidos.Pop, 0.4f, 0.1f);
        if (generador != null) generador.RatonEliminado(this);
        Destroy(gameObject);
    }

    public void RecibirDanio(float cantidad, Vector3 direccion)
    {
        if (muerto) return;
        vida -= cantidad;
        golpe = 1f;

        // Pequeño empujón
        Vector3 empuje = direccion;
        empuje.y = 0f;
        rb.AddForce(empuje.normalized * (tipo == Tipo.Gordito ? 2f : 4f) + Vector3.up * 2f, ForceMode.VelocityChange);

        if (vida <= 0f) Morir();
        else Sonidos.Reproducir(Sonidos.Golpe, 0.3f, 0.15f);
    }

    void Morir()
    {
        muerto = true;
        Vector3 centro = transform.position + Vector3.up * radio;
        if (Efectos.I != null)
        {
            if (tipo == Tipo.Gordito) Efectos.I.ConfetiGrande(centro);
            else Efectos.I.Confeti(centro);
        }
        Sonidos.Reproducir(Sonidos.Pop, 0.55f, 0.15f);
        if (GestorJuego.I != null) GestorJuego.I.SumarPuntos(puntos, centro + Vector3.up * radio);
        if (generador != null) generador.RatonEliminado(this);
        Destroy(gameObject);
    }
}
