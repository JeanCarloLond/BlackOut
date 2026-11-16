using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// 03 La barra. Botella agarrable: pesa distinto segun este llena o vacia,
    /// al inclinarla sobre un vaso lo sirve y llevada a la boca se bebe a morro.
    ///
    /// Cada botella es un licor distinto, con su color y su graduacion. El
    /// vaso mezcla lo que le echen, asi que se puede servir medio de ron y
    /// medio de licor verde y sale un color intermedio.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Bottle : MonoBehaviour
    {
        [Header("Peso")]
        [SerializeField] private float masaLlena = 1.2f;
        [SerializeField] private float masaVacia = 0.35f;
        [SerializeField, Range(0f, 1f)] private float contenido = 1f;

        [Header("Licor")]
        [SerializeField] private string bebida = "Ron";
        [SerializeField] private Color colorLiquido = new Color(0.75f, 0.35f, 0.06f);
        [Tooltip("1 = un licor normal. La cerveza va en torno a 0.35.")]
        [SerializeField] private float graduacion = 1f;
        [Tooltip("Hijo que muestra lo que queda dentro. Se escala con el contenido.")]
        [SerializeField] private Transform liquidoVisual;

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
        private float altoLiquido;
        private float baseLiquido;

        public float Contenido { get { return contenido; } }
        public string Bebida { get { return bebida; } }

        private void Awake()
        {
            cuerpo = GetComponent<Rigidbody>();
            if (boca == null) boca = transform;
            if (liquidoVisual != null)
            {
                altoLiquido = liquidoVisual.localScale.y;
                baseLiquido = liquidoVisual.localPosition.y - altoLiquido;
                Pintar(liquidoVisual, colorLiquido);
            }
            if (chorro != null)
            {
                Pintar(chorro.transform, colorLiquido);
                chorro.SetActive(false);
            }
            ActualizarMasa();
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
                        float cantidad = Mathf.Min(contenido, caudal * Time.deltaTime);
                        vaso.Llenar(cantidad, colorLiquido, graduacion, 0f);
                        contenido = Mathf.Max(0f, contenido - cantidad);
                        ActualizarMasa();
                        sirviendo = true;
                        ColocarChorro(boca.position, hit.point);
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
            if (liquidoVisual == null) return;

            bool hay = contenido > 0.01f;
            if (liquidoVisual.gameObject.activeSelf != hay) liquidoVisual.gameObject.SetActive(hay);
            if (!hay) return;
            float alto = altoLiquido * contenido;
            var e = liquidoVisual.localScale;
            liquidoVisual.localScale = new Vector3(e.x, alto, e.z);
            var p = liquidoVisual.localPosition;
            liquidoVisual.localPosition = new Vector3(p.x, baseLiquido + alto, p.z);
        }

        /// <summary>
        /// Un cilindro vertical de la boca al vaso. Va en coordenadas de mundo
        /// porque la botella esta inclinada y el chorro cae recto.
        /// </summary>
        private void ColocarChorro(Vector3 desde, Vector3 hasta)
        {
            if (chorro == null) return;
            var t = chorro.transform;
            float largo = Mathf.Max(0.01f, desde.y - hasta.y);
            t.SetPositionAndRotation(new Vector3(desde.x, desde.y - largo * 0.5f, desde.z), Quaternion.identity);
            var padre = t.parent != null ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3(0.008f / padre.x, largo * 0.5f / padre.y, 0.008f / padre.z);
        }

        private static void Pintar(Transform t, Color c)
        {
            var r = t.GetComponent<Renderer>();
            if (r == null) return;
            var b = new MaterialPropertyBlock();
            r.GetPropertyBlock(b);
            b.SetColor("_BaseColor", c);
            r.SetPropertyBlock(b);
        }
    }
}
