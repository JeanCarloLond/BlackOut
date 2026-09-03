using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Blackout
{
    /// <summary>
    /// 09 Abrir. Girar el letrero de CLOSED a OPEN cierra la experiencia.
    /// Solo responde cuando el bar ya esta encendido: falta un gesto, no varios.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class OpenSign : MonoBehaviour
    {
        [Header("Letrero")]
        [SerializeField] private Transform letrero;
        [SerializeField] private float segundosGiro = 0.6f;

        [Header("Neon")]
        [SerializeField] private Renderer neonRenderer;
        [SerializeField] private Color colorApagado = new Color(0.12f, 0.02f, 0.02f);
        [SerializeField] private Color colorEncendido = new Color(1f, 0.15f, 0.1f);
        [SerializeField] private float emisionFinal = 6f;

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Campanilla de la puerta.")]
        [SerializeField] private AudioClip clipCampanilla;
        [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.9f;
        [SerializeField] private float hapticDuration = 0.2f;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private XRSimpleInteractable interactable;
        private MaterialPropertyBlock mpb;
        private bool abierto;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            if (letrero == null) letrero = transform;
            mpb = new MaterialPropertyBlock();
            AplicarNeon(colorApagado, 0f);
        }

        private void OnEnable()  { interactable.selectEntered.AddListener(OnGirar); }
        private void OnDisable() { interactable.selectEntered.RemoveListener(OnGirar); }

        private void OnGirar(SelectEnterEventArgs args)
        {
            if (abierto) return;

            // El cierre solo esta disponible con el bar ya encendido y sonando.
            var estado = BarStateManager.Instance;
            if (estado != null && !estado.HasLight) return;

            abierto = true;
            HapticFeedback.Send(args.interactorObject, hapticAmplitude, hapticDuration);
            if (audioSource != null && clipCampanilla != null) audioSource.PlayOneShot(clipCampanilla);
            StartCoroutine(Girar());
        }

        private IEnumerator Girar()
        {
            Quaternion desde = letrero.localRotation;
            Quaternion hasta = desde * Quaternion.Euler(0f, 180f, 0f);

            float t = 0f;
            while (t < segundosGiro)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / segundosGiro));
                letrero.localRotation = Quaternion.Slerp(desde, hasta, k);
                AplicarNeon(Color.Lerp(colorApagado, colorEncendido, k), emisionFinal * k);
                yield return null;
            }
            letrero.localRotation = hasta;
            AplicarNeon(colorEncendido, emisionFinal);

            if (BarStateManager.Instance != null)
                BarStateManager.Instance.AdvanceTo(BarPhase.Open);
        }

        private void AplicarNeon(Color color, float intensidad)
        {
            if (neonRenderer == null) return;
            neonRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionColor, color * Mathf.Max(0f, intensidad));
            neonRenderer.SetPropertyBlock(mpb);
        }
    }
}
