using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Blackout
{
    /// <summary>
    /// 06 Muro de posters. Sitio del muro donde se clava un cartel.
    /// Al fijarlo suena la chincheta y arranca la anecdota de esa banda.
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public class PosterSlot : MonoBehaviour
    {
        [Header("Sonido")]
        [SerializeField] private AudioSource sfx;
        [Tooltip("Chincheta al clavar.")]
        [SerializeField] private AudioClip clipChincheta;
        [Tooltip("Fuente por la que se narra la anecdota. Si se deja vacia usa sfx.")]
        [SerializeField] private AudioSource narrador;

        private XRSocketInteractor socket;

        public Poster CartelPuesto { get; private set; }

        private void Awake()
        {
            socket = GetComponent<XRSocketInteractor>();
            if (sfx == null) sfx = GetComponent<AudioSource>();
            if (narrador == null) narrador = sfx;
        }

        private void OnEnable()
        {
            socket.selectEntered.AddListener(OnFijar);
            socket.selectExited.AddListener(OnQuitar);
        }

        private void OnDisable()
        {
            socket.selectEntered.RemoveListener(OnFijar);
            socket.selectExited.RemoveListener(OnQuitar);
        }

        private void OnFijar(SelectEnterEventArgs args)
        {
            var t = args.interactableObject != null ? args.interactableObject.transform : null;
            CartelPuesto = t != null ? t.GetComponent<Poster>() : null;

            if (sfx != null && clipChincheta != null) sfx.PlayOneShot(clipChincheta);

            if (CartelPuesto != null && CartelPuesto.Anecdota != null && narrador != null)
            {
                // La memoria del bar en la pared: un cartel, una historia.
                narrador.Stop();
                narrador.clip = CartelPuesto.Anecdota;
                narrador.loop = false;
                narrador.Play();
            }
        }

        private void OnQuitar(SelectExitEventArgs args)
        {
            CartelPuesto = null;
        }
    }
}
