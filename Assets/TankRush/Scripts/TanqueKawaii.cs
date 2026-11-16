using UnityEngine;

// Control del tanque: WASD para moverse, la torreta y el cañón siguen la mira
// (sin tener que mantener clic derecho) y clic izquierdo dispara.
[RequireComponent(typeof(Rigidbody))]
public class TanqueKawaii : MonoBehaviour
{
    [Header("Partes del tanque")]
    public Transform torreta;        // gira en horizontal
    public Transform canon;          // sube y baja
    public Transform puntoDisparo;   // punta del cañón (su eje Z apunta hacia donde sale la bala)
    public Collider casco;           // collider principal (los ratones te dañan al tocarlo)

    [Header("Tamaño de referencia (largo del tanque en metros)")]
    public float tamanio = 10f;

    [Header("Movimiento")]
    public float velocidadMax = 9f;
    public float aceleracion = 14f;
    public float velocidadGiro = 75f;

    [Header("Torreta y cañón")]
    public float velGiroTorreta = 140f;   // grados por segundo
    public float velElevacion = 70f;
    public float elevacionMin = -12f;
    public float elevacionMax = 30f;

    [Header("Disparo")]
    public Proyectil proyectilPrefab;
    public float cadencia = 0.28f;          // segundos entre disparos (mantén clic para ráfaga)
    public float velocidadProyectil = 60f;
    public float danio = 1f;
    public float radioExplosion = 1.5f;

    [Header("Vida")]
    public int vidaMax = 5;
    public float invulnerabilidad = 0.8f;

    [Header("Límite del escenario")]
    public Vector3 centroArena = Vector3.zero;
    public float radioArena = 100f;

    [Header("Efectos")]
    public ParticleSystem polvo;

    public bool ControlActivo { get; set; }
    public int Vida { get; private set; }
    public Vector3 PuntoApuntado { get; set; }
    public float VelocidadActual => vel;

    Rigidbody rb;
    float vel;
    float proximoDisparo;
    float invulnerableHasta;
    float retroceso;
    Vector3 canonPosBase;
    bool tieneCanonPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        Vida = vidaMax;

        if (canon != null)
        {
            canonPosBase = canon.localPosition;
            tieneCanonPos = true;
        }
        PuntoApuntado = transform.position + transform.forward * tamanio * 5f;
    }

    public void Reiniciar()
    {
        Vida = vidaMax;
        invulnerableHasta = 0f;
        vel = 0f;
    }

    void Update()
    {
        if (ControlActivo && Time.timeScale > 0f)
        {
            ApuntarTorreta(PuntoApuntado, Time.deltaTime);

            if (Input.GetMouseButton(0) && Time.time >= proximoDisparo)
            {
                Disparar();
                proximoDisparo = Time.time + cadencia;
            }
        }

        if (polvo != null)
        {
            var em = polvo.emission;
            em.rateOverTimeMultiplier = Mathf.Abs(vel) / Mathf.Max(0.01f, velocidadMax) * 22f;
        }
    }

    void LateUpdate()
    {
        // Retroceso del cañón al disparar
        retroceso = Mathf.MoveTowards(retroceso, 0f, Time.deltaTime * 4f);
        if (tieneCanonPos && canon != null && canon.parent != null && puntoDisparo != null)
        {
            Vector3 atrasMundo = -puntoDisparo.forward * retroceso * tamanio * 0.035f;
            canon.localPosition = canonPosBase + canon.parent.InverseTransformVector(atrasMundo);
        }
    }

    void FixedUpdate()
    {
        float avance = ControlActivo ? Input.GetAxisRaw("Vertical") : 0f;
        float giro = ControlActivo ? Input.GetAxisRaw("Horizontal") : 0f;
        float dt = Time.fixedDeltaTime;

        float objetivo = avance * velocidadMax * (avance < 0f ? 0.6f : 1f);
        float acel = Mathf.Approximately(avance, 0f) ? aceleracion * 1.5f : aceleracion;
        vel = Mathf.MoveTowards(vel, objetivo, acel * dt);

        Vector3 nueva = rb.position + transform.forward * vel * dt;

        // No dejar que el tanque salga del jardín
        Vector3 desdeCentro = nueva - centroArena;
        desdeCentro.y = 0f;
        if (desdeCentro.magnitude > radioArena)
        {
            Vector3 limite = centroArena + desdeCentro.normalized * radioArena;
            nueva.x = limite.x;
            nueva.z = limite.z;
            vel *= 0.5f;
        }
        rb.MovePosition(nueva);

        if (Mathf.Abs(giro) > 0.01f)
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, giro * velocidadGiro * dt, 0f));
    }

    // Gira la torreta en horizontal y el cañón en vertical hacia el punto apuntado.
    // Funciona en espacio mundo, así no importa cómo vengan orientados los ejes del modelo.
    void ApuntarTorreta(Vector3 objetivo, float dt)
    {
        if (torreta == null || puntoDisparo == null) return;
        Vector3 arriba = transform.up;

        Vector3 boca = puntoDisparo.forward;
        Vector3 bocaPlana = Vector3.ProjectOnPlane(boca, arriba);
        Vector3 haciaObjetivo = objetivo - torreta.position;
        Vector3 objetivoPlano = Vector3.ProjectOnPlane(haciaObjetivo, arriba);

        if (bocaPlana.sqrMagnitude > 1e-6f && objetivoPlano.sqrMagnitude > 1e-6f)
        {
            float angulo = Vector3.SignedAngle(bocaPlana, objetivoPlano, arriba);
            float paso = Mathf.Clamp(angulo, -velGiroTorreta * dt, velGiroTorreta * dt);
            torreta.RotateAround(torreta.position, arriba, paso);
            if (canon != null && !canon.IsChildOf(torreta))
                canon.RotateAround(torreta.position, arriba, paso);
        }

        if (canon == null) return;

        boca = puntoDisparo.forward;
        bocaPlana = Vector3.ProjectOnPlane(boca, arriba);
        if (bocaPlana.sqrMagnitude < 1e-6f) return;

        Vector3 desdeCanon = objetivo - canon.position;
        if (desdeCanon.sqrMagnitude < 1e-4f) return;

        float elevActual = Mathf.Asin(Mathf.Clamp(Vector3.Dot(boca.normalized, arriba), -1f, 1f)) * Mathf.Rad2Deg;
        float elevDeseada = Mathf.Asin(Mathf.Clamp(Vector3.Dot(desdeCanon.normalized, arriba), -1f, 1f)) * Mathf.Rad2Deg;
        elevDeseada = Mathf.Clamp(elevDeseada, elevacionMin, elevacionMax);

        float delta = Mathf.Clamp(elevDeseada - elevActual, -velElevacion * dt, velElevacion * dt);
        Vector3 derecha = Vector3.Cross(arriba, bocaPlana.normalized);
        // En Unity rotar positivo sobre "derecha" baja la punta, por eso va negativo
        canon.RotateAround(canon.position, derecha, -delta);
    }

    void Disparar()
    {
        if (proyectilPrefab == null || puntoDisparo == null) return;

        Vector3 origen = puntoDisparo.position;
        Vector3 dir = puntoDisparo.forward;
        Vector3 haciaMira = PuntoApuntado - origen;

        // Pequeña ayuda: si el cañón ya casi apunta a la mira, la bala va exacta a la mira
        if (haciaMira.sqrMagnitude > 0.01f && Vector3.Angle(dir, haciaMira) < 6f)
            dir = haciaMira.normalized;

        Proyectil p = Instantiate(proyectilPrefab, origen, Quaternion.LookRotation(dir));
        p.Lanzar(dir, velocidadProyectil, danio, radioExplosion, this);

        retroceso = 1f;
        if (Efectos.I != null) Efectos.I.Destello(origen, dir);
        if (GestorJuego.I != null && GestorJuego.I.camara != null) GestorJuego.I.camara.Sacudir(0.12f, 0.1f);
        Sonidos.Reproducir(Sonidos.Disparo, 0.35f, 0.08f);
    }

    public bool EsParteDelTanque(Collider c)
    {
        if (c == null) return false;
        return c.transform == transform || c.transform.IsChildOf(transform) || c.attachedRigidbody == rb;
    }

    public void RecibirDanio(int cantidad)
    {
        if (Vida <= 0 || Time.time < invulnerableHasta) return;
        invulnerableHasta = Time.time + invulnerabilidad;
        Vida = Mathf.Max(0, Vida - cantidad);

        if (Efectos.I != null) Efectos.I.Corazones(transform.position + Vector3.up * tamanio * 0.4f);

        if (Vida <= 0 && Efectos.I != null)
        {
            Efectos.I.ConfetiGrande(transform.position + Vector3.up * tamanio * 0.3f);
        }

        if (GestorJuego.I != null) GestorJuego.I.TanqueDaniado(Vida);
    }

    public void Curar(int cantidad)
    {
        Vida = Mathf.Min(vidaMax, Vida + cantidad);
        if (GestorJuego.I != null) GestorJuego.I.TanqueCurado(Vida);
    }
}
