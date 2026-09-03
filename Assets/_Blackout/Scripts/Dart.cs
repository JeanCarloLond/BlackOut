using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Blackout
{
    /// <summary>
    /// 08 Los dardos. Se lanza con el brazo y se clava con un golpe seco.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Dart : MonoBehaviour
    {
        [Header("Clavado")]
        [Tooltip("Velocidad minima de impacto para que se clave en vez de rebotar.")]
        [SerializeField] private float velocidadMinima = 1.2f;

        [Header("Sonido")]
        [SerializeField] private AudioSource sfx;
        [Tooltip("El dardo entrando en el corcho.")]
        [SerializeField] private AudioClip clipClavado;

        [Header("Haptico")]
        [Tooltip("Impulso al soltar el dardo.")]
        [SerializeField, Range(0f, 1f)] private float hapticSoltar = 0.45f;
        [SerializeField] private float hapticDuration = 0.08f;

        private Rigidbody cuerpo;
        private XRGrabInteractable agarre;
        private bool clavado;

        private void Awake()
        {
            cuerpo = GetComponent<Rigidbody>();
            agarre = GetComponent<XRGrabInteractable>();
        }

        private void OnEnable()
        {
            if (agarre != null)
            {
                agarre.selectExited.AddListener(OnSoltar);
                agarre.selectEntered.AddListener(OnAgarrar);
            }
        }

        private void OnDisable()
        {
            if (agarre != null)
            {
                agarre.selectExited.RemoveListener(OnSoltar);
                agarre.selectEntered.RemoveListener(OnAgarrar);
            }
        }

        private void OnAgarrar(SelectEnterEventArgs args)
        {
            // Volver a cogerlo lo despega de la diana.
            if (!clavado) return;
            clavado = false;
            transform.SetParent(null, true);
            cuerpo.isKinematic = false;
        }

        private void OnSoltar(SelectExitEventArgs args)
        {
            HapticFeedback.Send(args.interactorObject, hapticSoltar, hapticDuration);
        }

        private void OnCollisionEnter(Collision col)
        {
            if (clavado) return;

            var diana = col.collider.GetComponentInParent<Dartboard>();
            if (diana == null) return;
            if (col.relativeVelocity.magnitude < velocidadMinima) return;

            clavado = true;

            Vector3 punto = col.contactCount > 0 ? col.GetContact(0).point : transform.position;

            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
            cuerpo.isKinematic = true;
            transform.SetParent(diana.transform, true);

            if (sfx != null && clipClavado != null) sfx.PlayOneShot(clipClavado);

            diana.Puntuar(punto);
        }
    }
}
