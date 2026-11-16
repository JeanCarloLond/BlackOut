using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Blackout
{
    /// <summary>
    /// 03 La barra. Grifo de cerveza: se agarra la maneta y se tira hacia uno.
    /// Mientras esta abierta, el chorro cae del caño y llena el vaso que haya
    /// debajo, con espuma. El documento lo pide asi: «servirte un trago y
    /// beberlo; la cerveza suelta espuma».
    ///
    /// La maneta no sigue a la mano: basta con agarrarla. Un grifo real tiene
    /// solo dos posiciones y seguir la mano en un eje tan corto se siente peor
    /// que un tiron que encaja.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class Grifo : MonoBehaviour
    {
        [Header("Cerveza")]
        [SerializeField] private string cerveza = "Rubia";
        [SerializeField] private Color color = new Color(0.95f, 0.65f, 0.12f);
        [SerializeField] private float graduacion = 0.35f;
        [Tooltip("Espuma que suma cada unidad servida. La negra hace mas.")]
        [SerializeField] private float espuma = 1.6f;

        [Header("Mecanica")]
        [Tooltip("Pivote que gira al tirar. Si se deja vacio gira el propio objeto.")]
        [SerializeField] private Transform pivote;
        [SerializeField] private float anguloAbierto = 35f;
        [Tooltip("Punta del caño, de donde cae el chorro.")]
        [SerializeField] private Transform salida;
        [SerializeField] private float caudal = 0.3f;
        [SerializeField] private float alcance = 0.3f;
        [SerializeField] private GameObject chorro;

        [Header("Respuesta")]
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip clipChorro;
        [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.4f;

        private XRSimpleInteractable interactable;
        private Quaternion rotCerrado;
        private float apertura;
        private bool abierto;

        public string Cerveza { get { return cerveza; } }

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            if (pivote == null) pivote = transform;
            rotCerrado = pivote.localRotation;
            if (chorro != null)
            {
                var r = chorro.GetComponent<Renderer>();
                if (r != null)
                {
                    var b = new MaterialPropertyBlock();
                    r.GetPropertyBlock(b);
                    b.SetColor("_BaseColor", color);
                    r.SetPropertyBlock(b);
                }
                chorro.SetActive(false);
            }
        }

        private void OnEnable()
        {
            interactable.selectEntered.AddListener(OnAbrir);
            interactable.selectExited.AddListener(OnCerrar);
        }

        private void OnDisable()
        {
            interactable.selectEntered.RemoveListener(OnAbrir);
            interactable.selectExited.RemoveListener(OnCerrar);
        }

        private void OnAbrir(SelectEnterEventArgs args)
        {
            abierto = true;
            HapticFeedback.Send(args.interactorObject, hapticAmplitude, 0.06f);
        }

        private void OnCerrar(SelectExitEventArgs args)
        {
            abierto = false;
        }

        /// <summary>Para la demo y para probar sin visor.</summary>
        public void Abrir(bool valor) { abierto = valor; }

        private void Update()
        {
            apertura = Mathf.MoveTowards(apertura, abierto ? 1f : 0f, Time.deltaTime * 6f);
            pivote.localRotation = rotCerrado * Quaternion.Euler(0f, 0f, -anguloAbierto * apertura);

            bool sirviendo = false;
            if (apertura > 0.8f && salida != null)
            {
                sirviendo = true;
                Vector3 fin = salida.position + Vector3.down * alcance;
                RaycastHit hit;
                if (Physics.Raycast(salida.position, Vector3.down, out hit, alcance))
                {
                    fin = hit.point;
                    var vaso = hit.collider.GetComponentInParent<Glass>();
                    if (vaso != null) vaso.Llenar(caudal * Time.deltaTime, color, graduacion, espuma);
                }
                ColocarChorro(salida.position, fin);
            }

            if (chorro != null && chorro.activeSelf != sirviendo) chorro.SetActive(sirviendo);

            if (sfx != null && clipChorro != null)
            {
                if (sirviendo && !sfx.isPlaying) { sfx.clip = clipChorro; sfx.loop = true; sfx.Play(); }
                else if (!sirviendo && sfx.isPlaying) sfx.Stop();
            }
        }

        private void ColocarChorro(Vector3 desde, Vector3 hasta)
        {
            if (chorro == null) return;
            var t = chorro.transform;
            float largo = Mathf.Max(0.01f, desde.y - hasta.y);
            t.SetPositionAndRotation(new Vector3(desde.x, desde.y - largo * 0.5f, desde.z), Quaternion.identity);
            var padre = t.parent != null ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3(0.012f / padre.x, largo * 0.5f / padre.y, 0.012f / padre.z);
        }
    }
}
