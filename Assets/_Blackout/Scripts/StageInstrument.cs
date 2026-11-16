using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Blackout
{
    /// <summary>
    /// 05 El escenario. Instrumento de la tarima: suena al rasgarlo (gatillo
    /// mientras lo sostienes) o al golpearlo. Sandbox, sin acierto ni fallo.
    ///
    /// La bateria se toca de verdad: cada pieza suena al pegarle con la mano o
    /// con una baqueta, mas fuerte cuanto mas rapido llega el golpe. Las
    /// guitarras tocan acordes: el que suena depende de donde este la otra
    /// mano sobre el mastil, en cuatro trastes. Sin la otra mano en el mastil
    /// suena el acorde al aire.
    /// </summary>
    public class StageInstrument : MonoBehaviour
    {
        public enum Modo
        {
            Rasgar,  // gatillo mientras lo sostienes: cuerda, microfono
            Golpear, // impacto fisico: algo lanzado contra el
            Tocar,   // agarrarlo lo hace sonar (antiguo modo de la bateria)
            Percutir // pegarle con la mano o una baqueta: bateria
        }

        [Header("Comportamiento")]
        [SerializeField] private Modo modo = Modo.Golpear;
        [Tooltip("Velocidad de impacto minima para que suene (modo Golpear).")]
        [SerializeField] private float velocidadMinima = 0.55f;
        [Tooltip("Tiempo muerto entre golpes para que no se dispare en bucle.")]
        [SerializeField] private float reboteSegundos = 0.12f;

        [Header("Percutir")]
        [Tooltip("Velocidad de la mano o la baqueta a partir de la cual suena.")]
        [SerializeField] private float velocidadPercusion = 0.6f;
        [Tooltip("Velocidad a la que el golpe suena al maximo.")]
        [SerializeField] private float velocidadMaxima = 4f;

        [Header("Acordes (guitarras)")]
        [Tooltip("Un clip por traste, del mas cercano a la cejuela al mas cercano al cuerpo. El primero es tambien el acorde al aire.")]
        [SerializeField] private AudioClip[] acordes;
        [Tooltip("Extremo del mastil junto a la cejuela.")]
        [SerializeField] private Transform mastilInicio;
        [Tooltip("Extremo del mastil donde se une al cuerpo.")]
        [SerializeField] private Transform mastilFin;
        [Tooltip("A cuanto del mastil tiene que estar la otra mano para contar.")]
        [SerializeField] private float distanciaMastil = 0.15f;

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
            if (acordes != null && acordes.Length > 0)
            {
                // Un acorde nuevo apaga el anterior, como al rasgar de verdad.
                if (sfx != null) sfx.Stop();
                SonarClip(acordes[Traste(args.interactorObject)], 1f, 1f);
                return;
            }
            Sonar();
        }

        /// <summary>Un golpe de mano o baqueta. Lo llama GolpeMano.</summary>
        public void Percutir(float velocidad, object interactor)
        {
            if (modo != Modo.Percutir || velocidad < velocidadPercusion) return;
            if (!PasaRebote()) return;
            float fuerza = Mathf.InverseLerp(velocidadPercusion, velocidadMaxima, velocidad);
            HapticFeedback.Send(interactor, Mathf.Lerp(0.25f, hapticAmplitude, fuerza), hapticDuration);
            Sonar(fuerza);
        }

        public bool EsPercusion { get { return modo == Modo.Percutir; } }

        /// <summary>
        /// Que traste pisa la otra mano: la proyecta sobre el segmento del
        /// mastil y lo parte en tantos tramos como acordes. Si no hay ninguna
        /// mano cerca del mastil, suena el primero, al aire.
        /// </summary>
        private int Traste(IXRInteractor rasga)
        {
            if (mastilInicio == null || mastilFin == null || acordes.Length == 1) return 0;
            Vector3 a = mastilInicio.position, b = mastilFin.position;
            Vector3 eje = b - a;
            float largo = eje.magnitude;
            if (largo < 0.01f) return 0;
            eje /= largo;

            Transform propia = rasga != null ? rasga.transform : null;
            float mejor = float.MaxValue;
            float tMejor = -1f;
            foreach (var mano in Manos())
            {
                if (mano == null || !mano.isActiveAndEnabled) continue;
                if (propia != null && Vector3.Distance(mano.transform.position, propia.position) < 0.08f) continue;
                Vector3 p = mano.transform.position;
                float tt = Vector3.Dot(p - a, eje) / largo;
                if (tt < -0.1f || tt > 1.1f) continue;
                float d = Vector3.Distance(p, a + eje * Mathf.Clamp01(tt) * largo);
                if (d < distanciaMastil && d < mejor) { mejor = d; tMejor = tt; }
            }
            if (tMejor < 0f) return 0;
            return Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(tMejor) * acordes.Length), 0, acordes.Length - 1);
        }

        private static readonly List<XRBaseInputInteractor> manos = new List<XRBaseInputInteractor>();
        private static int marcoManos = -1;

        private static List<XRBaseInputInteractor> Manos()
        {
            // Una busqueda por cuadro como mucho, y solo al rasgar.
            if (marcoManos == Time.frameCount) return manos;
            marcoManos = Time.frameCount;
            manos.Clear();
            foreach (var m in FindObjectsByType<XRBaseInputInteractor>(FindObjectsSortMode.None))
                if (!(m is XRRayInteractor)) manos.Add(m);
            return manos;
        }

        private void OnTocar(SelectEnterEventArgs args)
        {
            if (!PasaRebote()) return;
            HapticFeedback.Send(args.interactorObject, hapticAmplitude, hapticDuration);
            Sonar();
        }

        private void OnCollisionEnter(Collision col)
        {
            if (modo != Modo.Golpear && modo != Modo.Percutir) return;
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
            AudioClip nota = notas != null && notas.Length > 0 ? notas[Random.Range(0, notas.Length)] : null;
            SonarClip(nota, Random.Range(rangoTono.x, rangoTono.y), Mathf.Lerp(0.4f, 1f, fuerza));
        }

        private void SonarClip(AudioClip clip, float tono, float volumen)
        {
            if (sfx != null && clip != null)
            {
                sfx.pitch = tono;
                sfx.PlayOneShot(clip, volumen);
            }
            if (Sonado != null) Sonado(this);
        }
    }
}
