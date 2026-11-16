using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// 03 La barra. Botella agarrable: pesa distinto segun este llena o vacia,
    /// al inclinarla sobre un vaso lo sirve y llevada a la boca se bebe a morro.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Bottle : MonoBehaviour
    {
        [Header("Peso")]
        [SerializeField] private float masaLlena = 1.2f;
        [SerializeField] private float masaVacia = 0.35f;
        [SerializeField, Range(0f, 1f)] private float contenido = 1f;

        [Header("Servir")]
        [Tooltip("Punto por donde sale el liquido. Si se deja vacio usa el propio transform.")]
        [SerializeField] private Transform boca;
        [SerializeField] private float anguloVertido = 60f;
        [SerializeField] private float caudal = 0.4f;
        [SerializeField] private float alcanceChorro = 0.55f;
        [Tooltip("Visual opcional del chorro.")]
        [SerializeField] private GameObject chorro;

        [Header("Beber a morro")]
        [Tooltip("Distancia de la boca de la botella a la cabeza para que cuente como trago.")]
        [SerializeField] private float distanciaBoca = 0.3f;
        [SerializeField] private float anguloTrago = 50f;
        [Tooltip("Contenido que se bebe por segundo.")]
        [SerializeField] private float ritmoTrago = 0.15f;
        [Tooltip("Cuantos vasos caben en la botella: a morro se emborracha mas rapido.")]
        [SerializeField] private float vasosPorBotella = 3f;

        [Header("Sonido")]
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip clipServir;
        [SerializeField] private AudioClip clipTrago;

        private Rigidbody cuerpo;
        private Transform cabeza;

        public float Contenido { get { return contenido; } }

        private void Awake()
        {
            cuerpo = GetComponent<Rigidbody>();
            if (boca == null) boca = transform;
            ActualizarMasa();
            if (chorro != null) chorro.SetActive(false);
        }

        private void Update()
        {
            if (Beber()) return;

            bool sirviendo = false;

            if (contenido > 0f && Vector3.Angle(transform.up, Vector3.up) > anguloVertido)
            {
                RaycastHit hit;
                if (Physics.Raycast(boca.position, Vector3.down, out hit, alcanceChorro))
                {
                    var vaso = hit.collider.GetComponentInParent<Glass>();
                    if (vaso != null)
                    {
                        float cantidad = caudal * Time.deltaTime;
                        vaso.Llenar(cantidad);
                        contenido = Mathf.Max(0f, contenido - cantidad);
                        ActualizarMasa();
                        sirviendo = true;
                    }
                }
            }

            if (chorro != null && chorro.activeSelf != sirviendo) chorro.SetActive(sirviendo);

            if (sfx != null && clipServir != null)
            {
                if (sirviendo && !sfx.isPlaying)
                {
                    sfx.clip = clipServir;
                    sfx.loop = true;
                    sfx.Play();
                }
                else if (!sirviendo && sfx.isPlaying && sfx.clip == clipServir)
                {
                    sfx.Stop();
                    sfx.loop = false;
                }
            }
        }

        /// <summary>A morro: boca de la botella junto a la cabeza e inclinada.</summary>
        private bool Beber()
        {
            if (cabeza == null)
            {
                if (Camera.main == null) return false;
                cabeza = Camera.main.transform;
            }
            if (contenido <= 0f) return false;
            if (Vector3.Distance(boca.position, cabeza.position) > distanciaBoca) return false;
            if (Vector3.Angle(transform.up, Vector3.up) < anguloTrago) return false;

            float trago = Mathf.Min(contenido, ritmoTrago * Time.deltaTime);
            contenido -= trago;
            ActualizarMasa();
            Borrachera.Beber(trago * vasosPorBotella);

            if (chorro != null && chorro.activeSelf) chorro.SetActive(false);
            if (sfx != null && clipTrago != null && !sfx.isPlaying)
            {
                sfx.loop = false;
                sfx.PlayOneShot(clipTrago);
            }
            return true;
        }

        private void ActualizarMasa()
        {
            if (cuerpo != null) cuerpo.mass = Mathf.Lerp(masaVacia, masaLlena, contenido);
        }
    }
}
