using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// Tablero de interruptores. Cuando todas las palancas estan abajo el bar
    /// tiene luz y se habilitan las demas estaciones: guia sin forzar.
    /// </summary>
    public class LightSwitchBank : MonoBehaviour
    {
        [Tooltip("Si se deja vacio, toma todos los LightSwitch hijos.")]
        [SerializeField] private LightSwitch[] palancas;

        [Header("Al completar el tablero")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clipBarAlumbrado;

        private int encendidas;

        private void Awake()
        {
            if (palancas == null || palancas.Length == 0)
                palancas = GetComponentsInChildren<LightSwitch>(true);
        }

        private void OnEnable()
        {
            foreach (var p in palancas)
                if (p != null) p.Cambiado += OnPalancaCambiada;
        }

        private void OnDisable()
        {
            foreach (var p in palancas)
                if (p != null) p.Cambiado -= OnPalancaCambiada;
        }

        private void OnPalancaCambiada(LightSwitch palanca)
        {
            encendidas++;
            if (encendidas < palancas.Length) return;

            if (audioSource != null && clipBarAlumbrado != null)
                audioSource.PlayOneShot(clipBarAlumbrado);

            if (BarStateManager.Instance != null)
                BarStateManager.Instance.AdvanceTo(BarPhase.Lit);
        }
    }
}
