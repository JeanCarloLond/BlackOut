using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Blackout
{
    /// <summary>
    /// Lo que se ve despues de beber. Cada trago del vaso o de la botella sube
    /// el nivel y se pasa solo con el tiempo.
    ///
    /// Todo va en el post-procesado, nunca en la camara: en VR mover o
    /// balancear la vista sin que se mueva la cabeza marea de verdad. Por la
    /// misma razon la aberracion cromatica va con tope bajo (la documentacion
    /// de URP la desaconseja en VR) y el peso lo llevan el desenfoque de lo
    /// lejano, la vineta y el color.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class Borrachera : MonoBehaviour
    {
        public static Borrachera Instance { get; private set; }

        [Header("Nivel")]
        [Tooltip("Cuanto sube con un vaso entero. La botella cuenta como varios vasos.")]
        [SerializeField] private float porVaso = 0.35f;
        [Tooltip("Lo que baja por segundo. Con 0.008 se pasa del todo en unos dos minutos.")]
        [SerializeField] private float recuperacion = 0.008f;
        [Tooltip("Suavizado del efecto: sube poco a poco aunque el trago sea de golpe.")]
        [SerializeField] private float segundosSubida = 3f;

        [Header("Comodidad")]
        [Tooltip("Techo del efecto. Bajalo si a alguien le marea.")]
        [SerializeField, Range(0f, 1f)] private float intensidadMaxima = 1f;

        [Header("El color se va de tono")]
        [Tooltip("Grados que oscila el tono a nivel maximo.")]
        [SerializeField] private float tonoMaximo = 25f;
        [Tooltip("Lo lento que oscila. Despacio: es un color que se va, no un parpadeo.")]
        [SerializeField] private float velocidadTono = 0.25f;

        [Header("Sonido")]
        [SerializeField] private AudioSource sfx;
        [Tooltip("Hipo al cruzar ciertos niveles.")]
        [SerializeField] private AudioClip clipHipo;

        /// <summary>0 sobrio, 1 lo maximo.</summary>
        public float Nivel { get; private set; }

        private Volume volumen;
        private ColorAdjustments color;
        private float visible;
        private int ultimoUmbral;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            volumen = GetComponent<Volume>();
            volumen.weight = 0f;
            // profile y no sharedProfile: en Play se trabaja sobre una copia y el
            // asset del proyecto no se toca.
            if (volumen.sharedProfile != null) volumen.profile.TryGet(out color);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Atajo para el vaso y la botella. Cantidad en vasos.</summary>
        public static void Beber(float vasos)
        {
            if (Instance != null) Instance.Sumar(vasos);
        }

        public void Sumar(float vasos)
        {
            if (vasos <= 0f) return;
            Nivel = Mathf.Clamp01(Nivel + vasos * porVaso);
        }

        private void Update()
        {
            Nivel = Mathf.Max(0f, Nivel - recuperacion * Time.deltaTime);

            // El efecto persigue al nivel: sube despacio y baja igual de despacio.
            float objetivo = Nivel * intensidadMaxima;
            visible = Mathf.MoveTowards(visible, objetivo, Time.deltaTime / Mathf.Max(0.1f, segundosSubida));
            volumen.weight = visible;

            if (color != null)
                color.hueShift.value = Mathf.Sin(Time.time * velocidadTono * Mathf.PI * 2f) * tonoMaximo * visible;

            Hipo();
        }

        private void Hipo()
        {
            // Un hipo al pasar de cuarto en cuarto, solo subiendo.
            int umbral = Mathf.FloorToInt(Nivel * 4f);
            if (umbral > ultimoUmbral && sfx != null && clipHipo != null)
            {
                sfx.pitch = Random.Range(0.95f, 1.1f);
                sfx.PlayOneShot(clipHipo);
            }
            ultimoUmbral = umbral;
        }
    }
}
