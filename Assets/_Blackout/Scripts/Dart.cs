using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Blackout
{
    /// <summary>
    /// 08 Los dardos. Se lanza con el brazo y se clava con un golpe seco.
    ///
    /// En vuelo la punta sigue la direccion del movimiento, como un dardo de
    /// verdad con sus aletas: sin esto salia girando segun como se soltara y
    /// podia quedar clavado de lado o al reves. Al clavarse se endereza hacia
    /// dentro de la diana y entra un poco en el corcho.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Dart : MonoBehaviour
    {
        [Header("Clavado")]
        [Tooltip("Velocidad minima de impacto para que se clave en vez de rebotar.")]
        [SerializeField] private float velocidadMinima = 1.2f;
        [Tooltip("Distancia del centro del dardo a la punta, en su eje Y local.")]
        [SerializeField] private float largoPunta = 0.077f;
        [Tooltip("Cuanto entra la punta en el corcho al clavarse.")]
        [SerializeField] private float profundidad = 0.012f;

        [Header("Vuelo")]
        [Tooltip("Rapidez con la que la punta se alinea con la direccion del vuelo.")]
        [SerializeField] private float alineacion = 18f;
        [Tooltip("Por debajo de esta velocidad no se alinea (cae o rueda por el suelo).")]
        [SerializeField] private float velocidadVuelo = 2f;

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
        private Vector3 ultimaVelocidad;

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

        private void FixedUpdate()
        {
            if (clavado || cuerpo.isKinematic) return;
            if (agarre != null && agarre.isSelected) return;

            Vector3 v = cuerpo.linearVelocity;
            ultimaVelocidad = v;
            if (v.magnitude < velocidadVuelo) return;

            Quaternion objetivo = Quaternion.FromToRotation(transform.up, v.normalized) * cuerpo.rotation;
            cuerpo.MoveRotation(Quaternion.Slerp(cuerpo.rotation, objetivo, alineacion * Time.fixedDeltaTime));
            cuerpo.angularVelocity *= 0.8f;
        }

        private void OnCollisionEnter(Collision col)
        {
            if (clavado) return;
            // Los dardos clavados cuelgan de la diana: chocar con uno no es
            // dar en la diana. Antes se quedaba pegado al otro dardo y puntuaba.
            if (col.collider.GetComponentInParent<Dart>() != null) return;

            var diana = col.collider.GetComponentInParent<Dartboard>();
            if (diana == null) return;
            if (col.relativeVelocity.magnitude < velocidadMinima) return;

            clavado = true;

            Vector3 punto = col.contactCount > 0 ? col.GetContact(0).point : transform.position;

            // Hacia dentro de la diana: la normal del contacto, con el signo
            // que va a favor del vuelo, un poco inclinada segun llegaba.
            Vector3 vuelo = ultimaVelocidad.sqrMagnitude > 0.01f ? ultimaVelocidad.normalized : transform.up;
            Vector3 dentro = col.contactCount > 0 ? col.GetContact(0).normal : vuelo;
            if (Vector3.Dot(dentro, vuelo) < 0f) dentro = -dentro;
            // Si roza el canto, la normal sale de lado (a unos 90 grados del vuelo):
            // entonces manda el vuelo. Un lanzamiento suave baja en arco hasta
            // unos 45 grados y ese no debe confundirse con un roce.
            if (Vector3.Angle(dentro, vuelo) > 60f) dentro = vuelo;
            dentro = Vector3.Slerp(dentro, vuelo, 0.25f).normalized;

            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
            cuerpo.isKinematic = true;
            transform.rotation = Quaternion.FromToRotation(transform.up, dentro) * transform.rotation;
            transform.position = punto - dentro * (largoPunta - profundidad);
            transform.SetParent(diana.transform, true);

            if (sfx != null && clipClavado != null) sfx.PlayOneShot(clipClavado);

            diana.Puntuar(punto);
        }
    }
}
