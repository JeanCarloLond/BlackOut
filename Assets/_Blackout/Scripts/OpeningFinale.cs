using System.Collections;
using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// El cierre de la experiencia. Cuando el letrero pasa a OPEN, el rotulo
    /// de la fachada se enciende por primera vez y vuelve el rumor de la
    /// calle: el bar que llevabas toda la sesion despertando ya esta abierto.
    ///
    /// El rotulo arranca apagado a proposito. Llegas de noche a un local
    /// cerrado y el neon es la recompensa, no el decorado.
    /// </summary>
    public class OpeningFinale : MonoBehaviour
    {
        [Header("Rotulo de la fachada")]
        [SerializeField] private Renderer rotulo;
        [SerializeField] private Color colorRotulo = Color.white;
        [SerializeField] private float emisionFinal = 2.6f;
        [SerializeField] private Light luzRotulo;
        [SerializeField] private float intensidadLuz = 4.5f;

        [Header("La calle vuelve")]
        [Tooltip("Ambiente de la calle, que se habia bajado al entrar.")]
        [SerializeField] private AudioSource ambienteCalle;
        [SerializeField, Range(0f, 1f)] private float volumenCalle = 0.55f;

        [Header("Las puertas se abren de par en par")]
        [Tooltip("Hojas de la puerta. Reciben un empujon hacia dentro.")]
        [SerializeField] private Rigidbody[] puertas;
        [SerializeField] private float empujeApertura = 1.6f;

        [Header("Tiempos")]
        [Tooltip("Lo que tarda el neon en prender del todo.")]
        [SerializeField] private float segundosEncendido = 2.2f;
        [Tooltip("Parpadeos de arranque, como un neon viejo.")]
        [SerializeField] private int parpadeos = 3;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock mpb;
        private bool lanzado;

        private void Awake()
        {
            mpb = new MaterialPropertyBlock();
            PintarRotulo(0f);
            if (luzRotulo != null) { luzRotulo.intensity = 0f; luzRotulo.enabled = false; }
        }

        private void OnEnable()
        {
            if (BarStateManager.Instance != null)
                BarStateManager.Instance.PhaseChanged += OnFase;
        }

        private void OnDisable()
        {
            if (BarStateManager.Instance != null)
                BarStateManager.Instance.PhaseChanged -= OnFase;
        }

        private void Start()
        {
            // Por si el manager ya estaba en Open antes de que nos suscribieramos
            if (BarStateManager.Instance != null && BarStateManager.Instance.Phase == BarPhase.Open)
                OnFase(BarPhase.Open);
        }

        private void OnFase(BarPhase fase)
        {
            if (fase != BarPhase.Open || lanzado) return;
            lanzado = true;
            StartCoroutine(Abrir());
        }

        private IEnumerator Abrir()
        {
            if (luzRotulo != null) luzRotulo.enabled = true;

            // Las hojas se abren solas: ya no las empujas tu, entra la gente
            if (puertas != null)
                foreach (var p in puertas)
                    if (p != null) p.AddTorque(Vector3.up * empujeApertura * (p.transform.localPosition.x < 0f ? 1f : -1f),
                                               ForceMode.VelocityChange);

            // Un neon viejo no prende limpio: titubea antes de quedarse
            for (int i = 0; i < parpadeos; i++)
            {
                float k = Random.Range(0.45f, 0.95f);
                PintarRotulo(emisionFinal * k);
                if (luzRotulo != null) luzRotulo.intensity = intensidadLuz * k;
                yield return new WaitForSeconds(Random.Range(0.04f, 0.11f));
                PintarRotulo(0f);
                if (luzRotulo != null) luzRotulo.intensity = 0f;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.14f));
            }

            float t = 0f;
            float volInicial = ambienteCalle != null ? ambienteCalle.volume : 0f;
            while (t < segundosEncendido)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / segundosEncendido);
                PintarRotulo(emisionFinal * k);
                if (luzRotulo != null) luzRotulo.intensity = intensidadLuz * k;
                if (ambienteCalle != null) ambienteCalle.volume = Mathf.Lerp(volInicial, volumenCalle, k);
                yield return null;
            }

            PintarRotulo(emisionFinal);
            if (luzRotulo != null) luzRotulo.intensity = intensidadLuz;
            if (ambienteCalle != null) ambienteCalle.volume = volumenCalle;
        }

        private void PintarRotulo(float intensidad)
        {
            if (rotulo == null) return;
            rotulo.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionColor, colorRotulo * Mathf.Max(0f, intensidad));
            rotulo.SetPropertyBlock(mpb);
        }
    }
}
