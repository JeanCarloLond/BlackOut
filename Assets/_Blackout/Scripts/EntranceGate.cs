using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Blackout
{
    /// <summary>
    /// 01 La entrada. Apuntar a la reja y jalar el gatillo la levanta.
    /// El ruido de la calle baja al cruzar y arranca la voz del dueno.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class EntranceGate : MonoBehaviour
    {
        [Header("Reja")]
        [SerializeField] private Transform reja;
        [SerializeField] private float alturaApertura = 2.1f;
        [SerializeField] private float segundosApertura = 1.8f;

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Golpe seco del pestillo al ceder.")]
        [SerializeField] private AudioClip clipPestillo;
        [Tooltip("La reja corriendo por el riel.")]
        [SerializeField] private AudioClip clipRiel;
        [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.8f;
        [SerializeField] private float hapticDuration = 0.15f;

        private XRSimpleInteractable interactable;
        private bool abierta;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            if (reja == null) reja = transform;
        }

        private void OnEnable()  { interactable.selectEntered.AddListener(OnJalar); }
        private void OnDisable() { interactable.selectEntered.RemoveListener(OnJalar); }

        private void OnJalar(SelectEnterEventArgs args)
        {
            if (abierta) return;
            abierta = true;

            HapticFeedback.Send(args.interactorObject, hapticAmplitude, hapticDuration);
            if (audioSource != null && clipPestillo != null) audioSource.PlayOneShot(clipPestillo);
            StartCoroutine(Abrir());
        }

        private IEnumerator Abrir()
        {
            if (audioSource != null && clipRiel != null) audioSource.PlayOneShot(clipRiel);

            Vector3 origen = reja.localPosition;
            Vector3 destino = origen + Vector3.up * alturaApertura;

            float t = 0f;
            while (t < segundosApertura)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / segundosApertura));
                reja.localPosition = Vector3.Lerp(origen, destino, k);
                yield return null;
            }
            reja.localPosition = destino;

            if (BarStateManager.Instance != null)
                BarStateManager.Instance.AdvanceTo(BarPhase.Inside);
        }
    }
}
