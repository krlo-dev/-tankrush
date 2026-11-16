using UnityEngine;

// Bala del tanque. Se mueve sola y detecta choques con un SphereCast por frame,
// así no atraviesa a los ratoncitos aunque vaya muy rápido.
public class Proyectil : MonoBehaviour
{
    public float radio = 0.3f;
    public float tiempoVida = 3f;

    Vector3 direccion;
    float velocidad;
    float danio;
    float radioExplosion;
    TanqueKawaii duenio;
    float vida;
    bool lanzado;

    static readonly RaycastHit[] hits = new RaycastHit[16];
    static readonly Collider[] cercanos = new Collider[48];

    public void Lanzar(Vector3 dir, float vel, float dmg, float radioExp, TanqueKawaii tanque)
    {
        direccion = dir.normalized;
        velocidad = vel;
        danio = dmg;
        radioExplosion = radioExp;
        duenio = tanque;
        lanzado = true;
    }

    void Update()
    {
        if (!lanzado) return;

        vida += Time.deltaTime;
        if (vida > tiempoVida)
        {
            Destroy(gameObject);
            return;
        }

        float paso = velocidad * Time.deltaTime;
        int n = Physics.SphereCastNonAlloc(transform.position, radio, direccion, hits, paso, ~0, QueryTriggerInteraction.Ignore);

        int mejor = -1;
        float dist = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            Collider c = hits[i].collider;
            if (duenio != null && duenio.EsParteDelTanque(c)) continue;
            if (c.GetComponentInParent<Proyectil>() != null) continue;
            if (hits[i].distance < dist)
            {
                dist = hits[i].distance;
                mejor = i;
            }
        }

        if (mejor >= 0)
        {
            Vector3 punto = hits[mejor].point;
            if (punto == Vector3.zero && hits[mejor].distance == 0f) punto = transform.position;
            Impactar(punto, hits[mejor].collider);
            return;
        }

        transform.position += direccion * paso;
    }

    void Impactar(Vector3 punto, Collider golpeado)
    {
        // Daño directo
        Raton directo = golpeado != null ? golpeado.GetComponentInParent<Raton>() : null;
        if (directo != null) directo.RecibirDanio(danio, direccion);

        // Daño en área (más chico)
        if (radioExplosion > 0f)
        {
            int n = Physics.OverlapSphereNonAlloc(punto, radioExplosion, cercanos, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Raton r = cercanos[i].GetComponentInParent<Raton>();
                if (r != null && r != directo) r.RecibirDanio(danio * 0.5f, (r.transform.position - punto).normalized);
            }
        }

        if (Efectos.I != null) Efectos.I.Impacto(punto);
        Destroy(gameObject);
    }
}
