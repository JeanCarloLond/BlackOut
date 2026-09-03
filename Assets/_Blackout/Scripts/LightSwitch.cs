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

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clipChasquido;
        [SerializeField] private AudioClip clipZumbido;
        [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.5f;
        [SerializeField] private float hapticDuration = 0.06f;

        public bool Encendido { get; private set; }
        public event System.Action<LightSwitch> Cambiado;

        private XRSimpleInteractable interactable;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            if (palanca == null) palanca = transform;
            palanca.localEulerAngles = anguloApagado;

            if (luces != null)
                foreach (var l in luces)
                    if (l != null) { l.intensity = 0f; l.enabled = false; }
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
            if (luces == null || luces.Length == 0) yield break;

            foreach (var l in luces) if (l != null) l.enabled = true;

            // Chisporroteo: dos parpadeos antes de estabilizar.
            for (int i = 0; i < 2; i++)
            {
                SetIntensidad(intensidadFinal * Random.Range(0.5f, 0.9f));
                yield return new WaitForSeconds(Random.Range(0.03f, 0.07f));
                SetIntensidad(0f);
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
                SetIntensidad(Mathf.Lerp(0f, intensidadFinal, Mathf.Clamp01(t / segundosCalentamiento)));
                yield return null;
            }
            SetIntensidad(intensidadFinal);
        }

        private void SetIntensidad(float v)
        {
            foreach (var l in luces) if (l != null) l.intensity = v;
        }
    }
}
