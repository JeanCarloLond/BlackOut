using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Blackout
{
    /// <summary>
    /// Lo interactivo se ilumina con un brillo calido cuando una mano o el
    /// rayo lo apuntan. Es la restriccion de usabilidad del documento: «solo
    /// lo interactivo reacciona con un brillo calido», para que el usuario no
    /// tenga que adivinar que se puede tocar en un bar lleno de cosas.
    ///
    /// Trabaja sobre copias de los materiales, creadas la primera vez que se
    /// apunta al objeto: el keyword de emision no se puede activar con un
    /// MaterialPropertyBlock, y tocar el material compartido iluminaria todas
    /// las botellas a la vez. Las copias no rompen el SRP Batcher, que agrupa
    /// por shader y no por material.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class BrilloAgarrable : MonoBehaviour
    {
        [SerializeField] private Color colorBrillo = new Color(1f, 0.55f, 0.25f);
        [SerializeField] private float intensidad = 0.5f;
        [Tooltip("Segundos en aparecer o irse.")]
        [SerializeField] private float transicion = 0.12f;
        [Tooltip("Renderers que no deben brillar (liquidos, chorros, textos).")]
        [SerializeField] private Renderer[] excluir;

        private XRBaseInteractable interactable;
        private readonly List<Material> copias = new List<Material>();
        private readonly List<Color> emisionOriginal = new List<Color>();
        private readonly List<bool> teniaEmision = new List<bool>();
        private bool preparado;
        private int manos;
        private float actual;

        private static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();
        }

        private void OnEnable()
        {
            interactable.hoverEntered.AddListener(OnEntra);
            interactable.hoverExited.AddListener(OnSale);
        }

        private void OnDisable()
        {
            interactable.hoverEntered.RemoveListener(OnEntra);
            interactable.hoverExited.RemoveListener(OnSale);
            manos = 0;
            actual = 0f;
            Aplicar(0f);
        }

        private void OnDestroy()
        {
            foreach (var m in copias) if (m != null) Destroy(m);
        }

        private static bool EsMano(IXRInteractor interactor)
        {
            // Los huecos del estante y de los carteles tambien "apuntan" a lo
            // que tienen cerca. Eso no es el usuario, asi que no cuenta.
            return !(interactor is XRSocketInteractor);
        }

        private void OnEntra(HoverEnterEventArgs args)
        {
            if (EsMano(args.interactorObject)) manos++;
        }

        private void OnSale(HoverExitEventArgs args)
        {
            if (EsMano(args.interactorObject)) manos = Mathf.Max(0, manos - 1);
        }

        private void Update()
        {
            // Mientras se sostiene ya no hace falta senalarlo.
            float objetivo = manos > 0 && !interactable.isSelected ? 1f : 0f;
            if (Mathf.Approximately(actual, objetivo)) return;
            actual = Mathf.MoveTowards(actual, objetivo, Time.deltaTime / Mathf.Max(0.01f, transicion));
            Aplicar(actual);
        }

        private void Preparar()
        {
            preparado = true;
            var fuera = new HashSet<Renderer>();
            if (excluir != null) foreach (var r in excluir) if (r != null) fuera.Add(r);

            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (fuera.Contains(r) || r is ParticleSystemRenderer) continue;
                if (r.GetComponent<TMPro.TMP_Text>() != null) continue;
                var mats = r.materials; // crea las copias
                foreach (var m in mats)
                {
                    if (m == null || !m.HasProperty(IdEmision)) continue;
                    copias.Add(m);
                    teniaEmision.Add(m.IsKeywordEnabled("_EMISSION"));
                    emisionOriginal.Add(m.GetColor(IdEmision));
                }
            }
        }

        private void Aplicar(float t)
        {
            if (!preparado)
            {
                if (t <= 0f) return;
                Preparar();
            }
            Color extra = colorBrillo * (intensidad * t);
            for (int i = 0; i < copias.Count; i++)
            {
                var m = copias[i];
                if (m == null) continue;
                if (t > 0f) m.EnableKeyword("_EMISSION");
                else if (!teniaEmision[i]) m.DisableKeyword("_EMISSION");
                m.SetColor(IdEmision, emisionOriginal[i] + extra);
            }
        }
    }
}
