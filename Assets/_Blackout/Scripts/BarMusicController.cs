using System.Collections;
using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// La musica del local. Un solo disco suena a la vez: elegir que suena
    /// es elegir el animo del bar esta noche.
    /// </summary>
    public class BarMusicController : MonoBehaviour
    {
        [Tooltip("Fuente 2D que se oye en todo el local.")]
        [SerializeField] private AudioSource fuente;
        [SerializeField] private float fadeSegundos = 1.2f;
        [SerializeField, Range(0f, 1f)] private float volumenObjetivo = 0.7f;

        public AudioClip SonandoAhora { get; private set; }

        private Coroutine fade;

        private void Awake()
        {
            if (fuente == null) fuente = GetComponent<AudioSource>();
            if (fuente != null) { fuente.loop = true; fuente.playOnAwake = false; fuente.spatialBlend = 0f; fuente.volume = 0f; }
        }

        public void Reproducir(AudioClip clip)
        {
            if (fuente == null || clip == null) return;
            SonandoAhora = clip;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(Cambiar(clip));
        }

        public void Detener()
        {
            SonandoAhora = null;
            if (fuente == null) return;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(Cambiar(null));
        }

        private IEnumerator Cambiar(AudioClip nuevo)
        {
            float inicio = fuente.volume;
            float t = 0f;
            float mitad = fadeSegundos * 0.5f;

            while (t < mitad)
            {
                t += Time.deltaTime;
                fuente.volume = Mathf.Lerp(inicio, 0f, t / mitad);
                yield return null;
            }

            fuente.Stop();
            if (nuevo == null) { fuente.volume = 0f; fade = null; yield break; }

            fuente.clip = nuevo;
            fuente.Play();

            t = 0f;
            while (t < mitad)
            {
                t += Time.deltaTime;
                fuente.volume = Mathf.Lerp(0f, volumenObjetivo, t / mitad);
                yield return null;
            }
            fuente.volume = volumenObjetivo;
            fade = null;
        }
    }
}
