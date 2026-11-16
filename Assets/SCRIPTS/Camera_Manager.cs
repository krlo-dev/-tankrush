using UnityEngine;

public class Camera_Manager : MonoBehaviour
{
    [Header("Referencias")]
    public Transform tanque;           // Tank 2
    public Transform puntoDisparo;     // PuntoDisparo
    public Camera camPrimeraPersona;
    public Camera camTerceraPersona;
    public Camera camFija;

    [Header("Primera persona")]
    public Vector3 offsetPrimera = Vector3.zero;   // local a PuntoDisparo

    [Header("Tercera persona (detrás del cañón)")]
    public Vector3 puntoFoco = new Vector3(0f, 2f, 0f);
    public float distancia = 9f;
    public float sensibilidad = 3f;
    public float pitchInicial = 15f;
    public float pitchMin = -10f;
    public float pitchMax = 60f;

    [Header("Cámara fija (vista general desde atrás)")]
    public Vector3 offsetFija = new Vector3(0f, 14f, -16f);
    public bool fijaRotaConTanque = false;   // false = no gira cuando el tanque gira
    public float suavizado = 5f;

    Camera[] camaras;
    int actual = 1;                  // empieza en tercera persona
    float yaw, pitch;
    float yawInicialTanque;

    void Start()
    {
        camaras = new[] { camPrimeraPersona, camTerceraPersona, camFija };
        pitch = pitchInicial;
        yawInicialTanque = tanque.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Activar(actual);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
            Activar((actual + 1) % 3);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void LateUpdate()
    {
        switch (actual)
        {
            case 0: PrimeraPersona(); break;
            case 1: TerceraPersona(); break;
            case 2: Fija(); break;
        }
    }

    // 1) En el punto de disparo: sube, baja y gira con el cañón
    void PrimeraPersona()
    {
        camPrimeraPersona.transform.position = puntoDisparo.TransformPoint(offsetPrimera);
        camPrimeraPersona.transform.rotation = puntoDisparo.rotation;
    }

    // 2) Detrás del cañón, sigue al tanque, el mouse orbita
    void TerceraPersona()
    {
        yaw += Input.GetAxis("Mouse X") * sensibilidad;

        // Con click derecho el mouse vertical lo usa el cañón
        if (!Input.GetMouseButton(1))
        {
            pitch -= Input.GetAxis("Mouse Y") * sensibilidad;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        }

        Quaternion rot = tanque.rotation * Quaternion.Euler(pitch, yaw, 0f);
        Vector3 foco = tanque.position + puntoFoco;

        camTerceraPersona.transform.position = foco - rot * Vector3.forward * distancia;
        camTerceraPersona.transform.LookAt(foco);
    }

    // 3) Vista general desde atrás, sigue al tanque con suavizado
    void Fija()
    {
        camFija.transform.position = Vector3.Lerp(
            camFija.transform.position, DestinoFija(), suavizado * Time.deltaTime);
        camFija.transform.LookAt(tanque.position + puntoFoco);
    }

    Vector3 DestinoFija()
    {
        Quaternion baseRot = fijaRotaConTanque
            ? tanque.rotation
            : Quaternion.Euler(0f, yawInicialTanque, 0f);
        return tanque.position + baseRot * offsetFija;
    }

    void Activar(int i)
    {
        actual = i;
        for (int c = 0; c < camaras.Length; c++)
            camaras[c].gameObject.SetActive(c == i);

        // Al entrar a la fija, colocarla de una vez (sin "volar" desde lejos)
        if (i == 2)
            camFija.transform.position = DestinoFija();
    }
}