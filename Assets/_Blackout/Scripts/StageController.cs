using System.Collections;
using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// 05 El escenario. Un riff de prueba enciende los focos de la tarima:
    /// los instrumentos esperaban en silencio y el gesto los despierta.
    /// </summary>
    public class StageController : MonoBehaviour
    {
        [Tooltip("Si se deja vacio, toma todos los StageInstrument hijos.")]
        [SerializeField] private StageInstrument[] instrumentos;

        [Header("Focos de la tarima")]
        [SerializeField] private Light[] focos;
        [SerializeField] private float intensidadFinal = 14f;
        [SerializeField] private float segundosEncendido = 1.1f;

        [Header("Sonido")]
        [SerializeField] private AudioSource sfx;
        [Tooltip("Acople del ampli al despertar el escenario.")]
        [SerializeField] private AudioClip clipAcople;

        private bool encendido;

        private void Awake()
        {
            if (instrumentos == null || instrumentos.Length == 0)
                instrumentos = GetComponentsInChildren<StageInstrument>(true);

            if (focos != null)
                foreach (var f in focos)
                    if (f != null) { f.intensity = 0f; f.enabled = false; }
        }

        private void OnEnable()
        {
            foreach (var i in instrumentos)
                if (i != null) i.Sonado += OnInstrumentoSonado;
        }

        private void OnDisable()
        {
            foreach (var i in instrumentos)
                if (i != null) i.Sonado -= OnInstrumentoSonado;
        }

        private void OnInstrumentoSonado(StageInstrument instrumento)
        {
            if (encendido) return;
            encendido = true;

            if (sfx != null && clipAcople != null) sfx.PlayOneShot(clipAcople);
            StartCoroutine(EncenderFocos());
        }

        private IEnumerator EncenderFocos()
        {
            if (focos == null || focos.Length == 0) yield break;

            foreach (var f in focos)
                if (f != null) f.enabled = true;

            float t = 0f;
            while (t < segundosEncendido)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / segundosEncendido);
                foreach (var f in focos)
                    if (f != null) f.intensity = k * intensidadFinal;
                yield return null;
            }

            foreach (var f in focos)
                if (f != null) f.intensity = intensidadFinal;
        }
    }
}
