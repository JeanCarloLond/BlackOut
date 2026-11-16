using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Blackout
{
    /// <summary>
    /// Va en cada mando: un trigger esferico que sigue la mano y, si choca
    /// con el parroquiano lo bastante rapido, le transmite el golpe. Es trigger
    /// y no collider solido para que la mano no empuje botellas ni vasos sin
    /// querer: solo el parroquiano reacciona.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class GolpeMano : MonoBehaviour
    {
        [Tooltip("Velocidad minima de la mano para que cuente como golpe.")]
        [SerializeField] private float velocidadMinima = 1.2f;
        [Tooltip("Impulso por cada m/s de la mano. Un punetazo normal ronda los 3-5 m/s.")]
        [SerializeField] private float fuerza = 2.2f;

        [Header("Haptico")]
        [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.7f;
        [SerializeField] private float hapticDuration = 0.08f;

        private Vector3 ultimaPos;
        private Vector3 velocidad;
        private XRBaseInputInteractor interactor;

        private void Awake()
        {
            GetComponent<SphereCollider>().isTrigger = true;

            // Un trigger solo recibe eventos de cuerpos cinematicos si tiene Rigidbody.
            var rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            if (transform.parent != null)
                interactor = transform.parent.GetComponentInChildren<XRBaseInputInteractor>();
            ultimaPos = transform.position;
        }

        private void FixedUpdate()
        {
            velocidad = (transform.position - ultimaPos) / Time.fixedDeltaTime;
            ultimaPos = transform.position;
        }

        private void OnTriggerEnter(Collider otro)
        {
            var parte = otro.attachedRigidbody;
            if (parte == null) return;
            var npc = parte.GetComponentInParent<Parroquiano>();
            if (npc == null || npc.Agarrado) return;
            if (velocidad.magnitude < velocidadMinima) return;

            npc.Recibir(parte, velocidad * fuerza, otro.ClosestPoint(transform.position));
            HapticFeedback.Send(interactor, hapticAmplitude, hapticDuration);
        }
    }
}
