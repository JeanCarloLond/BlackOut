using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Blackout
{
    /// <summary>
    /// 04 Tocadiscos. Sacas un vinilo, lo pones y bajas la aguja.
    /// La aguja cae, cruje el vinilo y arranca el riff.
    /// </summary>
    public class Turntable : MonoBehaviour
    {
        [Header("Plato")]
        [Tooltip("Socket donde encaja el vinilo.")]
        [SerializeField] private XRSocketInteractor plato;
        [Tooltip("Se pone a girar mientras suena.")]
        [SerializeField] private Transform platoGiratorio;
        [SerializeField] private float rpm = 33.3f;

        [Header("Aguja")]
        [SerializeField] private XRSimpleInteractable palancaAguja;
        [SerializeField] private Transform brazo;
        [SerializeField] private Vector3 brazoArriba = new Vector3(0f, 0f, 0f);
        [SerializeField] private Vector3 brazoAbajo = new Vector3(0f, -28f, 0f);
        [SerializeField] private float segundosBrazo = 0.8f;

        [Header("Sonido")]
        [SerializeField] private AudioSource sfx;
        [Tooltip("La aguja al caer sobre el surco.")]
        [SerializeField] private AudioClip clipAguja;
        [Tooltip("Crujido del vinilo antes del riff.")]
        [SerializeField] private AudioClip clipCrujido;
        [SerializeField] private BarMusicController musica;

        [Header("Haptico")]
        [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.35f;
        [SerializeField] private float hapticDuration = 0.12f;

        private Vinyl discoPuesto;
        private bool sonando;

        private void Awake()
        {
            if (brazo != null) brazo.localEulerAngles = brazoArriba;
        }

        private void OnEnable()
        {
            if (plato != null)
            {
                plato.selectEntered.AddListener(OnDiscoPuesto);
                plato.selectExited.AddListener(OnDiscoRetirado);
            }
            if (palancaAguja != null) palancaAguja.selectEntered.AddListener(OnBajarAguja);
        }

        private void OnDisable()
        {
            if (plato != null)
            {
                plato.selectEntered.RemoveListener(OnDiscoPuesto);
                plato.selectExited.RemoveListener(OnDiscoRetirado);
            }
            if (palancaAguja != null) palancaAguja.selectEntered.RemoveListener(OnBajarAguja);
        }

        private void Update()
        {
            if (sonando && platoGiratorio != null)
                platoGiratorio.Rotate(Vector3.up, rpm * 6f * Time.deltaTime, Space.Self);
        }

        private void OnDiscoPuesto(SelectEnterEventArgs args)
        {
            var t = args.interactableObject != null ? args.interactableObject.transform : null;
            discoPuesto = t != null ? t.GetComponent<Vinyl>() : null;
        }

        private void OnDiscoRetirado(SelectExitEventArgs args)
        {
            discoPuesto = null;
            Parar();
        }

        private void OnBajarAguja(SelectEnterEventArgs args)
        {
            HapticFeedback.Send(args.interactorObject, hapticAmplitude, hapticDuration);

            if (sonando) { Parar(); return; }
            if (discoPuesto == null || discoPuesto.Tema == null) return;

            StartCoroutine(BajarYSonar());
        }

        private IEnumerator BajarYSonar()
        {
            sonando = true;

            if (brazo != null)
            {
                Quaternion desde = Quaternion.Euler(brazoArriba);
                Quaternion hasta = Quaternion.Euler(brazoAbajo);
                float t = 0f;
                while (t < segundosBrazo)
                {
                    t += Time.deltaTime;
                    brazo.localRotation = Quaternion.Slerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / segundosBrazo));
                    yield return null;
                }
                brazo.localRotation = hasta;
            }

            if (sfx != null && clipAguja != null) sfx.PlayOneShot(clipAguja);
            yield return new WaitForSeconds(0.25f);

            if (sfx != null && clipCrujido != null) sfx.PlayOneShot(clipCrujido);
            yield return new WaitForSeconds(0.6f);

            if (musica != null) musica.Reproducir(discoPuesto.Tema);
        }

        private void Parar()
        {
            sonando = false;
            if (brazo != null) brazo.localEulerAngles = brazoArriba;
            if (musica != null) musica.Detener();
        }
    }
}
