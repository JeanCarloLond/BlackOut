using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// 03 La barra. Botella agarrable: pesa distinto segun este llena o vacia,
    /// y al inclinarla sobre un vaso lo sirve.
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

        [Header("Sonido")]
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip clipServir;

        private Rigidbody cuerpo;

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

        private void ActualizarMasa()
        {
            if (cuerpo != null) cuerpo.mass = Mathf.Lerp(masaVacia, masaLlena, contenido);
        }
    }
}
