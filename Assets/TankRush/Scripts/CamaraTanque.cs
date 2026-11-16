using UnityEngine;

// Una sola cámara con tres modos (tecla C):
//  - Tercera persona: el mouse mueve la cámara alrededor del tanque y la torreta apunta a la mira del centro.
//  - Artillero: vista desde encima de la torreta, el mouse apunta.
//  - Aérea: cámara alta, el cursor apunta sobre el piso.
// En el menú la cámara gira lentamente alrededor del tanque.
[RequireComponent(typeof(Camera))]
public class CamaraTanque : MonoBehaviour
{
    public enum ModoCamara { TerceraPersona, Artillero, Aerea }

    public TanqueKawaii tanque;

    [Header("Mouse")]
    public float sensibilidad = 2.2f;
    public float pitchMin = -10f;
    public float pitchMax = 55f;

    [Header("Distancias (en largos de tanque)")]
    public float distanciaTercera = 1.55f;
    public float alturaFoco = 0.45f;
    public float zoomMin = 0.7f;
    public float zoomMax = 1.8f;

    [Header("Suavizado")]
    public float suavizadoAerea = 6f;

    public ModoCamara Modo { get; private set; } = ModoCamara.TerceraPersona;

    Camera cam;
    float yaw, pitch = 14f;
    float zoom = 1f;
    float yawMenu;
    float yawAerea;
    float sacudida, sacudidaDur, sacudidaT;
    readonly RaycastHit[] hits = new RaycastHit[24];

    float L => tanque != null ? Mathf.Max(0.1f, tanque.tamanio) : 10f;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.nearClipPlane = Mathf.Max(0.05f, L * 0.01f);
        cam.farClipPlane = Mathf.Max(1000f, L * 60f);
    }

    void Start()
    {
        if (tanque != null)
        {
            yaw = tanque.transform.eulerAngles.y;
            yawMenu = yaw + 140f;
        }
    }

    public void AlEmpezarPartida()
    {
        if (tanque != null) yaw = tanque.transform.eulerAngles.y;
        pitch = 14f;
        Modo = ModoCamara.TerceraPersona;
        AvisarModo();
    }

    public void Sacudir(float intensidad, float duracion)
    {
        if (intensidad >= sacudida * (1f - sacudidaT / Mathf.Max(0.001f, sacudidaDur)))
        {
            sacudida = intensidad;
            sacudidaDur = duracion;
            sacudidaT = 0f;
        }
    }

    void Update()
    {
        if (GestorJuego.I == null || !GestorJuego.I.Jugando) return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            Modo = (ModoCamara)(((int)Modo + 1) % 3);
            if (Modo == ModoCamara.Aerea) yawAerea = yaw;
            AvisarModo();
            GestorJuego.I.ActualizarCursor();
        }

        float rueda = Input.mouseScrollDelta.y;
        if (Mathf.Abs(rueda) > 0.01f) zoom = Mathf.Clamp(zoom - rueda * 0.1f, zoomMin, zoomMax);
    }

    void AvisarModo()
    {
        if (GestorJuego.I == null || GestorJuego.I.ui == null) return;
        string nombre = Modo == ModoCamara.TerceraPersona ? "Tercera persona"
                      : Modo == ModoCamara.Artillero ? "Artillero" : "Vista aérea";
        GestorJuego.I.ui.ModoCamara(nombre, Modo == ModoCamara.Aerea);
    }

    void LateUpdate()
    {
        if (tanque == null) return;
        float dt = Time.unscaledDeltaTime;
        Vector3 pos = tanque.transform.position;
        Vector3 foco = pos + Vector3.up * L * alturaFoco;

        bool jugando = GestorJuego.I != null && GestorJuego.I.EstadoActual != GestorJuego.Estado.Menu;

        if (!jugando)
        {
            // Vista de presentación en el menú
            yawMenu += dt * 10f;
            Quaternion r = Quaternion.Euler(16f, yawMenu, 0f);
            transform.position = foco - r * Vector3.forward * L * 1.7f;
            transform.LookAt(foco);
            return;
        }

        bool controla = GestorJuego.I.Jugando;
        if (controla && Modo != ModoCamara.Aerea)
        {
            yaw += Input.GetAxis("Mouse X") * sensibilidad;
            pitch -= Input.GetAxis("Mouse Y") * sensibilidad;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        }

        switch (Modo)
        {
            case ModoCamara.TerceraPersona:
            {
                Quaternion r = Quaternion.Euler(pitch, yaw, 0f);
                Vector3 p = foco - r * Vector3.forward * L * distanciaTercera * zoom;
                float pisoMin = pos.y + L * 0.08f;
                if (p.y < pisoMin) p.y = pisoMin;
                transform.position = p;
                transform.rotation = r;
                break;
            }
            case ModoCamara.Artillero:
            {
                Quaternion r = Quaternion.Euler(pitch * 0.6f, yaw, 0f);
                Vector3 centroTorreta = tanque.torreta != null ? tanque.torreta.position : pos;
                Vector3 p = new Vector3(centroTorreta.x, pos.y + L * 0.62f, centroTorreta.z)
                            - (Quaternion.Euler(0f, yaw, 0f) * Vector3.forward) * L * 0.35f;
                transform.position = p;
                transform.rotation = r;
                break;
            }
            case ModoCamara.Aerea:
            {
                Quaternion baseRot = Quaternion.Euler(0f, yawAerea, 0f);
                Vector3 destino = pos + baseRot * new Vector3(0f, L * 2.3f * zoom, -L * 1.5f * zoom);
                transform.position = Vector3.Lerp(transform.position, destino, 1f - Mathf.Exp(-suavizadoAerea * dt));
                transform.LookAt(pos + Vector3.up * L * 0.2f);
                break;
            }
        }

        // Sacudida de cámara
        if (sacudidaT < sacudidaDur)
        {
            sacudidaT += dt;
            float k = 1f - sacudidaT / sacudidaDur;
            transform.position += Random.insideUnitSphere * sacudida * k * L * 0.03f;
        }

        if (controla) CalcularMira();
    }

    void CalcularMira()
    {
        Ray rayo = Modo == ModoCamara.Aerea
            ? cam.ScreenPointToRay(Input.mousePosition)
            : new Ray(transform.position, transform.forward);

        float alcance = L * 40f;
        int n = Physics.RaycastNonAlloc(rayo, hits, alcance, ~0, QueryTriggerInteraction.Ignore);
        float mejor = float.MaxValue;
        Vector3 punto = rayo.GetPoint(L * 12f);
        for (int i = 0; i < n; i++)
        {
            if (tanque.EsParteDelTanque(hits[i].collider)) continue;
            if (hits[i].distance < mejor)
            {
                mejor = hits[i].distance;
                punto = hits[i].point;
            }
        }

        // En aérea apuntamos un poco por encima del piso para no clavar las balas en el suelo
        if (Modo == ModoCamara.Aerea) punto += Vector3.up * L * 0.12f;

        tanque.PuntoApuntado = punto;

        if (GestorJuego.I.ui != null && Modo == ModoCamara.Aerea)
            GestorJuego.I.ui.MoverMira(Input.mousePosition);
    }
}
