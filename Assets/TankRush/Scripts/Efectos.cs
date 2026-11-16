using UnityEngine;

// Punto central para crear efectos (confeti arcoíris, nubecitas, chispas, corazones).
// Los prefabs de los efectos se asignan desde el Inspector.
public class Efectos : MonoBehaviour
{
    public static Efectos I { get; private set; }

    public GameObject confeti;          // muerte de ratón
    public GameObject confetiGrande;    // gordito / tanque
    public GameObject puff;             // nubecita al aparecer o morder
    public GameObject impacto;          // bala que pega en el piso
    public GameObject destello;         // fogonazo del cañón
    public GameObject corazones;        // cuando dañan al tanque

    void Awake()
    {
        I = this;
    }

    GameObject Crear(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;
        GameObject go = Instantiate(prefab, pos, rot);
        Destroy(go, 5f); // por si acaso
        return go;
    }

    public void Confeti(Vector3 pos) => Crear(confeti, pos, Quaternion.identity);
    public void ConfetiGrande(Vector3 pos) => Crear(confetiGrande != null ? confetiGrande : confeti, pos, Quaternion.identity);
    public void Puff(Vector3 pos) => Crear(puff, pos, Quaternion.identity);
    public void Impacto(Vector3 pos) => Crear(impacto, pos, Quaternion.identity);
    public void Corazones(Vector3 pos) => Crear(corazones, pos, Quaternion.identity);

    public void Destello(Vector3 pos, Vector3 dir)
    {
        Crear(destello, pos, Quaternion.LookRotation(dir));
    }
}
