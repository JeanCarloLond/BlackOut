using System;
using System.Collections;
using UnityEngine;

namespace Blackout
{
    /// <summary>Fases del recorrido de apertura del bar.</summary>
    public enum BarPhase
    {
        Street = 0, // fuera, con la reja abajo
        Inside = 1, // dentro y en penumbra
        Lit    = 2, // luces rojas encendidas: se habilitan las estaciones
        Open   = 3  // letrero girado a OPEN
    }

    /// <summary>
    /// Estado central de la experiencia. Las estaciones consultan la fase y la avanzan.
    /// Encender las luces (02) es lo que habilita el resto de estaciones: guia sin forzar.
    /// </summary>
    public class BarStateManager : MonoBehaviour
    {
        public static BarStateManager Instance { get; private set; }

        [Header("Ambiente sonoro")]
        [Tooltip("Ruido de la calle. Baja al cruzar la puerta.")]
        [SerializeField] private AudioSource streetAmbience;
        [Tooltip("Ambiente interior del bar.")]
        [SerializeField] private AudioSource barAmbience;
        [SerializeField] private float crossfadeSeconds = 2.5f;

        [Header("Narrativa explicita")]
        [SerializeField] private AudioSource voiceOver;
        [Tooltip("Voz del dueno al entrar: 'otra noche, a encender esto'.")]
        [SerializeField] private AudioClip vozEntrada;
        [Tooltip("Cierre al girar el letrero a OPEN.")]
        [SerializeField] private AudioClip vozCierre;
        [Tooltip("El rumor de la gente que entra al abrir.")]
        [SerializeField] private AudioClip genteEntrando;

        [Header("Gating")]
        [Tooltip("Estaciones que solo se habilitan cuando el bar tiene luz.")]
        [SerializeField] private GameObject[] estacionesConLuz;

        public event Action<BarPhase> PhaseChanged;
        public BarPhase Phase { get; private set; } = BarPhase.Street;

        private Coroutine fadeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            SetStationsActive(false);
            if (barAmbience != null) { barAmbience.volume = 0f; barAmbience.loop = true; }
            if (streetAmbience != null) { streetAmbience.loop = true; if (!streetAmbience.isPlaying) streetAmbience.Play(); }
            PhaseChanged?.Invoke(Phase);
        }

        /// <summary>Avanza la fase. Nunca retrocede: el recorrido es de un solo sentido.</summary>
        public void AdvanceTo(BarPhase next)
        {
            if (next <= Phase) return;
            Phase = next;

            switch (next)
            {
                case BarPhase.Inside:
                    if (fadeRoutine != null) StopCoroutine(fadeRoutine);
                    fadeRoutine = StartCoroutine(Crossfade(streetAmbience, barAmbience));
                    PlayVoice(vozEntrada);
                    break;

                case BarPhase.Lit:
                    SetStationsActive(true);
                    break;

                case BarPhase.Open:
                    PlayVoice(vozCierre);
                    if (voiceOver != null && genteEntrando != null)
                        voiceOver.PlayOneShot(genteEntrando);
                    break;
            }

            PhaseChanged?.Invoke(Phase);
        }

        public bool HasLight => Phase >= BarPhase.Lit;

        private void PlayVoice(AudioClip clip)
        {
            if (voiceOver == null || clip == null) return;
            voiceOver.PlayOneShot(clip);
        }

        private void SetStationsActive(bool active)
        {
            if (estacionesConLuz == null) return;
            foreach (var go in estacionesConLuz)
                if (go != null) go.SetActive(active);
        }

        private IEnumerator Crossfade(AudioSource outSrc, AudioSource inSrc)
        {
            float outStart = outSrc != null ? outSrc.volume : 0f;
            if (inSrc != null && !inSrc.isPlaying) { inSrc.volume = 0f; inSrc.Play(); }

            float t = 0f;
            while (t < crossfadeSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / crossfadeSeconds);
                if (outSrc != null) outSrc.volume = Mathf.Lerp(outStart, 0.08f, k);
                if (inSrc != null) inSrc.volume = Mathf.Lerp(0f, 1f, k);
                yield return null;
            }
            fadeRoutine = null;
        }
    }
}
