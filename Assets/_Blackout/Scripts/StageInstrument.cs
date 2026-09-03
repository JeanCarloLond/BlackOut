using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Blackout
{
    /// <summary>
    /// 05 El escenario. Instrumento de la tarima: suena al rasgarlo (gatillo
    /// mientras lo sostienes) o al golpearlo. Sandbox, sin acierto ni fallo.
    /// </summary>
    public class StageInstrument : MonoBehaviour
    {
        public enum Modo
        {
            Rasgar,  // gatillo mientras lo sostienes: cuerda, microfono
            Golpear, // impacto fisico: algo lanzado contra el
            Tocar    // la mano lo roza: platillo, tambor
        }

        [Header("Comportamiento")]
        [SerializeField] private Modo modo = Modo.Golpear;
        [Tooltip("Velocidad de impacto minima para que suene (modo Golpear).")]
        [SerializeField] private float velocidadMinima = 0.55f;
        [Tooltip("Tiempo muerto entre golpes para que no se dispare en bucle.")]
        [SerializeField] private float reboteSegundos = 0.12f;

        [Header("Sonido")]
        [SerializeField] private AudioSource sfx;
        [Tooltip("Se elige una al azar en cada toque.")]
        [SerializeField] private AudioClip[] notas;
        [SerializeField] private Vector2 rangoTono = new Vector2(0.92f, 1.08f);

        [Header("Haptico")]
        [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.55f;
        [SerializeField] private float hapticDuration = 0.1f;

        /// <summary>Se dispara la primera vez y cada vez que suena el instrumento.</summary>
        public event System.Action<StageInstrument> Sonado;

        private XRBaseInteractable interactable;
        private float ultimoToque = -999f;

        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (sfx == null) sfx = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (interactable == null) return;
            if (modo == Modo.Rasgar) interactable.activated.AddListener(OnRasgar);
            else if (modo == Modo.Tocar) interactable.selectEntered.AddListener(OnTocar);
        }

        private void OnDisable()
        {
            if (interactable == null) return;
            if (modo == Modo.Rasgar) interactable.activated.RemoveListener(OnRasgar);
            else if (modo == Modo.Tocar) interactable.selectEntered.RemoveListener(OnTocar);
        }

        private void OnRasgar(ActivateEventArgs args)
        {
            if (!PasaRebote()) return;
            HapticFeedback.Send(args.interactorObject, hapticAmplitude, hapticDuration);
            Sonar();
        }

        private void OnTocar(SelectEnterEventArgs args)
        {
            if (!PasaRebote()) return;
            HapticFeedback.Send(args.interactorObject, hapticAmplitude, hapticDuration);
            Sonar();
        }

        private void OnCollisionEnter(Collision col)
        {
            if (modo != Modo.Golpear) return;
            if (col.relativeVelocity.magnitude < velocidadMinima) return;
            if (!PasaRebote()) return;

            // El golpe pega mas fuerte cuanto mas rapido llega.
            float fuerza = Mathf.Clamp01(col.relativeVelocity.magnitude / 3f);
            Sonar(fuerza);
        }

        private bool PasaRebote()
        {
            if (Time.time - ultimoToque < reboteSegundos) return false;
            ultimoToque = Time.time;
            return true;
        }

        private void Sonar()
        {
            Sonar(1f);
        }

        private void Sonar(float fuerza)
        {
            if (sfx != null && notas != null && notas.Length > 0)
            {
                var nota = notas[Random.Range(0, notas.Length)];
                if (nota != null)
                {
                    sfx.pitch = Random.Range(rangoTono.x, rangoTono.y);
                    sfx.PlayOneShot(nota, Mathf.Lerp(0.4f, 1f, fuerza));
                }
            }
            if (Sonado != null) Sonado(this);
        }
    }
}
