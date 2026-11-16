using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Blackout
{
    /// <summary>
    /// Vaso de la barra. Se llena al servirle una botella o un grifo encima
    /// y se bebe acercandolo a la boca e inclinandolo.
    ///
    /// Lleva la cuenta de lo que tiene dentro: el color sale de mezclar lo
    /// servido segun cuanto se echo de cada cosa, y la graduacion igual, asi
    /// que una cerveza emborracha menos que un vaso de ron. La cerveza ademas
    /// deja espuma, que baja sola con el tiempo como en un vaso de verdad.
    /// </summary>
    public class Glass : MonoBehaviour
    {
        [Header("Liquido")]
        [Tooltip("Hijo que representa el contenido. Se escala segun el nivel.")]
        [SerializeField] private Transform liquido;
        [SerializeField] private float alturaMaxima = 0.09f;
        [SerializeField, Range(0f, 1f)] private float nivel = 0f;
        [Tooltip("A partir de este nivel el trago se da por servido y suma.")]
        [SerializeField, Range(0f, 1f)] private float nivelServido = 0.8f;

        [Header("Espuma")]
        [Tooltip("Capa clara encima del liquido. Opcional.")]
        [SerializeField] private Transform espumaVisual;
        [Tooltip("Grosor de la espuma en metros cuando esta al maximo.")]
        [SerializeField] private float altoEspuma = 0.025f;
        [Tooltip("Cuanto baja por segundo. Nunca desaparece del todo mientras haya cerveza.")]
        [SerializeField] private float bajadaEspuma = 0.04f;
        [SerializeField, Range(0f, 1f)] private float espumaMinima = 0.15f;

        [Header("Beber")]
        [SerializeField] private float distanciaBoca = 0.3f;
        [SerializeField] private float anguloTrago = 40f;
        [SerializeField] private float ritmoTrago = 0.6f;

        [Header("Brindar")]
        [Tooltip("Velocidad relativa minima al chocar con otro vaso para que suene.")]
        [SerializeField] private float velocidadBrindis = 0.35f;
        [SerializeField] private AudioClip clipBrindis;

        [Header("Sonido")]
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip clipTrago;

        private Transform cabeza;
        private float baseY;
        private Color color = new Color(0.75f, 0.35f, 0.06f);
        private float graduacion = 1f;
        private float espuma;
        private float ultimoBrindis = -999f;
        private Renderer rendLiquido;
        private MaterialPropertyBlock bloque;

        private static readonly int IdColor = Shader.PropertyToID("_BaseColor");

        public float Nivel { get { return nivel; } }
        public Color ColorBebida { get { return color; } }

        private void Awake()
        {
            if (liquido != null)
            {
                baseY = liquido.localPosition.y;
                rendLiquido = liquido.GetComponent<Renderer>();
                if (rendLiquido != null && rendLiquido.sharedMaterial != null
                    && rendLiquido.sharedMaterial.HasProperty(IdColor))
                    color = rendLiquido.sharedMaterial.GetColor(IdColor);
            }
            bloque = new MaterialPropertyBlock();
            if (Camera.main != null) cabeza = Camera.main.transform;
            ActualizarVisual();
        }

        /// <summary>Compatibilidad: sirve el mismo liquido que ya tenga.</summary>
        public void Llenar(float cantidad)
        {
            Llenar(cantidad, color, graduacion, 0f);
        }

        /// <summary>
        /// Echa liquido. El color y la graduacion se mezclan con lo que ya
        /// hubiera, ponderados por cantidad. <paramref name="espumaPorUnidad"/>
        /// es la espuma que suma cada unidad servida (0 para licores).
        /// </summary>
        public void Llenar(float cantidad, Color colorNuevo, float graduacionNueva, float espumaPorUnidad)
        {
            if (cantidad <= 0f) return;
            float cabe = Mathf.Min(cantidad, 1f - nivel);

            if (cabe > 0f)
            {
                float peso = cabe / Mathf.Max(0.0001f, nivel + cabe);
                color = Color.Lerp(color, colorNuevo, peso);
                graduacion = Mathf.Lerp(graduacion, graduacionNueva, peso);
            }

            bool yaServido = nivel >= nivelServido;
            nivel = Mathf.Clamp01(nivel + cabe);
            // La espuma sigue subiendo aunque el vaso este lleno: al servir de
            // mas, rebosa espuma y no liquido.
            espuma = Mathf.Clamp01(espuma + cantidad * espumaPorUnidad);
            ActualizarVisual();

            if (!yaServido && nivel >= nivelServido)
                BarScore.Anotar(Hito.Servir, this, transform.position);
        }

        private void Update()
        {
            if (espuma > espumaMinima && nivel > 0f)
            {
                espuma = Mathf.Max(espumaMinima, espuma - bajadaEspuma * Time.deltaTime);
                ActualizarVisual();
            }

            if (cabeza == null)
            {
                if (Camera.main != null) cabeza = Camera.main.transform;
                return;
            }
            if (nivel <= 0f) return;
            if (Vector3.Distance(transform.position, cabeza.position) > distanciaBoca) return;
            if (Vector3.Angle(transform.up, Vector3.up) < anguloTrago) return;

            float antes = nivel;
            nivel = Mathf.Max(0f, nivel - ritmoTrago * Time.deltaTime);
            if (nivel <= 0f) espuma = 0f;
            ActualizarVisual();
            Borrachera.Beber((antes - nivel) * graduacion);

            // Vaciarlo bebiendo cuenta; derramarlo no, porque aqui no se resta.
            // Frente a la vista y no en el vaso: el vaso esta en la boca y el +N
            // saldria pegado a los ojos.
            if (nivel <= 0f) BarScore.Anotar(Hito.Trago, this);

            if (sfx != null && clipTrago != null && !sfx.isPlaying)
                sfx.PlayOneShot(clipTrago);
        }

        private void OnCollisionEnter(Collision choque)
        {
            // Brindar entre lanzamientos, como pide el documento en los dardos.
            // Solo suena uno de los dos vasos para no oir el choque doble.
            var otro = choque.collider.GetComponentInParent<Glass>();
            if (otro == null || otro == this) return;
            if (GetInstanceID() < otro.GetInstanceID()) return;
            if (choque.relativeVelocity.magnitude < velocidadBrindis) return;
            if (Time.time - ultimoBrindis < 0.4f) return;
            ultimoBrindis = Time.time;
            otro.ultimoBrindis = Time.time;

            if (sfx != null && clipBrindis != null)
            {
                sfx.pitch = Random.Range(0.95f, 1.1f);
                sfx.PlayOneShot(clipBrindis);
            }
            otro.VibrarMano();
            VibrarMano();
        }

        private void VibrarMano()
        {
            var agarre = GetComponent<XRGrabInteractable>();
            if (agarre == null) return;
            foreach (var mano in agarre.interactorsSelecting)
                HapticFeedback.Send(mano, 0.35f, 0.05f);
        }

        private void ActualizarVisual()
        {
            if (liquido == null) return;
            bool hay = nivel > 0.01f;
            if (liquido.gameObject.activeSelf != hay) liquido.gameObject.SetActive(hay);
            if (espumaVisual != null)
            {
                bool hayEspuma = hay && espuma > 0.01f;
                if (espumaVisual.gameObject.activeSelf != hayEspuma) espumaVisual.gameObject.SetActive(hayEspuma);
            }
            if (!hay) return;

            float alto = Mathf.Max(0.001f, nivel * alturaMaxima);
            var e = liquido.localScale;
            liquido.localScale = new Vector3(e.x, alto * 0.5f / MedioAlto(liquido), e.z);
            var p = liquido.localPosition;
            liquido.localPosition = new Vector3(p.x, baseY + alto * 0.5f, p.z);

            if (rendLiquido != null)
            {
                rendLiquido.GetPropertyBlock(bloque);
                bloque.SetColor(IdColor, color);
                rendLiquido.SetPropertyBlock(bloque);
            }

            if (espumaVisual != null && espuma > 0.01f)
            {
                float grosor = Mathf.Max(0.002f, espuma * altoEspuma);
                var es = espumaVisual.localScale;
                espumaVisual.localScale = new Vector3(es.x, grosor * 0.5f / MedioAlto(espumaVisual), es.z);
                var ep = espumaVisual.localPosition;
                espumaVisual.localPosition = new Vector3(ep.x, baseY + alto + grosor * 0.5f, ep.z);
            }
        }

        /// <summary>
        /// Medio alto de la malla en unidades locales. El cilindro primitivo
        /// mide 2 de alto, asi que una escala Y de 0.5 da un alto de 1.
        /// </summary>
        private static float MedioAlto(Transform t)
        {
            var mf = t.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return 1f;
            float h = mf.sharedMesh.bounds.extents.y;
            return h > 0f ? h : 1f;
        }
    }
}
