using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Blackout
{
    /// <summary>
    /// 02 Luces &amp; ambiente. Una palanca del tablero viejo.
    /// Chisporroteo al prender, zumbido calido del tubo y vibracion corta al bajarla.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class LightSwitch : MonoBehaviour
    {
        [Header("Palanca")]
        [SerializeField] private Transform palanca;
        [SerializeField] private Vector3 anguloApagado = new Vector3(20f, 0f, 0f);
        [SerializeField] private Vector3 anguloEncendido = new Vector3(-20f, 0f, 0f);
        [SerializeField] private float segundosRecorrido = 0.1f;

        [Header("Luces que controla")]
        [SerializeField] private Light[] luces;
        [SerializeField] private float intensidadFinal = 4f;
        [SerializeField] private float segundosCalentamiento = 0.7f;

        [Header("Superficies emisivas que enciende")]
        [Tooltip("El estante retroiluminado, un neon, un rotulo. Se encienden " +
                 "con el mismo chisporroteo que las luces.")]
        [SerializeField] private Renderer[] emisivos;
        [SerializeField] private Color colorEmision = new Color(1f, 0.14f, 0.08f);
        [SerializeField] private float emisionFinal = 4f;

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clipChasquido;
        [SerializeField] private AudioClip clipZumbido;
        [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.5f;
        [SerializeField] private float hapticDuration = 0.06f;

        public bool Encendido { get; private set; }
        public event System.Action<LightSwitch> Cambiado;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private XRSimpleInteractable interactable;
        private MaterialPropertyBlock mpb;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            if (palanca == null) palanca = transform;
            palanca.localEulerAngles = anguloApagado;
            mpb = new MaterialPropertyBlock();

            if (luces != null)
                foreach (var l in luces)
                    if (l != null) { l.intensity = 0f; l.enabled = false; }

            SetEmision(0f);
        }

        private void OnEnable()  { interactable.selectEntered.AddListener(OnAccionar); }
        private void OnDisable() { interactable.selectEntered.RemoveListener(OnAccionar); }

        private void OnAccionar(SelectEnterEventArgs args)
        {
            if (Encendido) return;
            Encendido = true;

            HapticFeedback.Send(args.interactorObject, hapticAmplitude, hapticDuration);
            if (audioSource != null && clipChasquido != null) audioSource.PlayOneShot(clipChasquido);

            StartCoroutine(BajarPalanca());
            StartCoroutine(Encender());
            Cambiado?.Invoke(this);
        }

        private IEnumerator BajarPalanca()
        {
            Quaternion desde = Quaternion.Euler(anguloApagado);
            Quaternion hasta = Quaternion.Euler(anguloEncendido);
            float t = 0f;
            while (t < segundosRecorrido)
            {
                t += Time.deltaTime;
                palanca.localRotation = Quaternion.Slerp(desde, hasta, Mathf.Clamp01(t / segundosRecorrido));
                yield return null;
            }
            palanca.localRotation = hasta;
        }

        private IEnumerator Encender()
        {
            bool hayLuces = luces != null && luces.Length > 0;
            bool hayEmisivos = emisivos != null && emisivos.Length > 0;
            if (!hayLuces && !hayEmisivos) yield break;

            if (hayLuces)
                foreach (var l in luces)
                    if (l != null) l.enabled = true;

            // Chisporroteo: dos parpadeos antes de estabilizar.
            for (int i = 0; i < 2; i++)
            {
                float k = Random.Range(0.5f, 0.9f);
                SetIntensidad(intensidadFinal * k);
                SetEmision(emisionFinal * k);
                yield return new WaitForSeconds(Random.Range(0.03f, 0.07f));
                SetIntensidad(0f);
                SetEmision(0f);
                yield return new WaitForSeconds(Random.Range(0.03f, 0.06f));
            }

            if (audioSource != null && clipZumbido != null)
            {
                audioSource.clip = clipZumbido;
                audioSource.loop = true;
                audioSource.Play();
            }

            float t = 0f;
            while (t < segundosCalentamiento)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / segundosCalentamiento);
                SetIntensidad(Mathf.Lerp(0f, intensidadFinal, k));
                SetEmision(Mathf.Lerp(0f, emisionFinal, k));
                yield return null;
            }
            SetIntensidad(intensidadFinal);
            SetEmision(emisionFinal);
        }

        private void SetIntensidad(float v)
        {
            if (luces == null) return;
            foreach (var l in luces) if (l != null) l.intensity = v;
        }

        private void SetEmision(float intensidad)
        {
            if (emisivos == null || mpb == null) return;
            Color c = colorEmision * Mathf.Max(0f, intensidad);
            foreach (var r in emisivos)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(EmissionColor, c);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
