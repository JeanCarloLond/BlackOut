using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Blackout
{
    /// <summary>
    /// 09 Abrir. El cartelito cuelga de un cordel sobre la puerta y se gira
    /// con la mano, como los de cualquier bar. No hay animacion: el neon sube
    /// segun el angulo real al que lo has girado, y el bar abre cuando la cara
    /// OPEN queda de frente. Solo responde con el bar ya encendido.
    /// </summary>
    public class OpenSign : MonoBehaviour
    {
        [Header("Caras del cartel")]
        [SerializeField] private Renderer caraClosed;
        [SerializeField] private Renderer caraOpen;
        [SerializeField] private Color colorClosed = new Color(0.4f, 0.07f, 0.08f);
        [SerializeField] private Color colorOpen = new Color(1f, 0.22f, 0.12f);
        [SerializeField] private float emisionClosed = 0.55f;
        [SerializeField] private float emisionOpen = 3.2f;

        [Header("Giro")]
        [Tooltip("Grados de giro a partir de los cuales el bar se da por abierto.")]
        [SerializeField] private float anguloApertura = 140f;
        [Tooltip("Por debajo de estos grados el cartel sigue leyendose CLOSED.")]
        [SerializeField] private float anguloMuerto = 25f;

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Campanilla de la puerta al abrir.")]
        [SerializeField] private AudioClip clipCampanilla;
        [Tooltip("El cartel golpeando al balancearse.")]
        [SerializeField] private AudioClip clipGolpe;
        [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.85f;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private XRGrabInteractable agarre;
        private MaterialPropertyBlock mpb;
        private Vector3 frenteCerrado;
        private bool abierto;
        private float ultimoGolpe = -999f;

        /// <summary>0 = CLOSED de frente, 1 = OPEN de frente.</summary>
        public float Progreso { get; private set; }

        private void Awake()
        {
            agarre = GetComponent<XRGrabInteractable>();
            mpb = new MaterialPropertyBlock();

            frenteCerrado = transform.forward;
            frenteCerrado.y = 0f;
            if (frenteCerrado.sqrMagnitude < 0.0001f) frenteCerrado = Vector3.forward;
            frenteCerrado.Normalize();

            Pintar(0f);
        }

        private void OnEnable()
        {
            if (agarre != null) agarre.selectEntered.AddListener(OnAgarrar);
        }

        private void OnDisable()
        {
            if (agarre != null) agarre.selectEntered.RemoveListener(OnAgarrar);
        }

        private void OnAgarrar(SelectEnterEventArgs args)
        {
            HapticFeedback.Send(args.interactorObject, hapticAmplitude * 0.5f, 0.05f);
        }

        private void Update()
        {
            var estado = BarStateManager.Instance;
            if (estado != null && !estado.HasLight) { Pintar(0f); return; }

            Vector3 f = transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.0001f) return;
            f.Normalize();

            float angulo = Vector3.Angle(f, frenteCerrado);
            Progreso = Mathf.Clamp01(Mathf.InverseLerp(anguloMuerto, anguloApertura, angulo));
            Pintar(Progreso);

            if (!abierto && angulo >= anguloApertura) Abrir();
        }

        private void Abrir()
        {
            abierto = true;
            if (audioSource != null && clipCampanilla != null) audioSource.PlayOneShot(clipCampanilla);
            if (BarStateManager.Instance != null)
                BarStateManager.Instance.AdvanceTo(BarPhase.Open);
        }

        private void OnCollisionEnter(Collision col)
        {
            if (audioSource == null || clipGolpe == null) return;
            if (col.relativeVelocity.magnitude < 0.35f) return;
            if (Time.time - ultimoGolpe < 0.2f) return;
            ultimoGolpe = Time.time;
            audioSource.PlayOneShot(clipGolpe, Mathf.Clamp01(col.relativeVelocity.magnitude / 2f));
        }

        private void Pintar(float k)
        {
            Emitir(caraClosed, colorClosed * (emisionClosed * (1f - k)));
            Emitir(caraOpen,   colorOpen   * (emisionOpen   * k));
        }

        private void Emitir(Renderer r, Color c)
        {
            if (r == null) return;
            r.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionColor, c);
            r.SetPropertyBlock(mpb);
        }
    }
}
