using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Blackout
{
    /// <summary>Hueco del estante. Ordenar la barra marca el avance de apertura.</summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public class ShelfSlot : MonoBehaviour
    {
        public bool Ocupado { get; private set; }
        public event System.Action<ShelfSlot> Cambiado;

        private XRSocketInteractor socket;

        private void Awake()
        {
            socket = GetComponent<XRSocketInteractor>();
        }

        private void OnEnable()
        {
            socket.selectEntered.AddListener(OnEntra);
            socket.selectExited.AddListener(OnSale);
        }

        private void OnDisable()
        {
            socket.selectEntered.RemoveListener(OnEntra);
            socket.selectExited.RemoveListener(OnSale);
        }

        private void OnEntra(SelectEnterEventArgs args)
        {
            Ocupado = true;
            if (Cambiado != null) Cambiado(this);
        }

        private void OnSale(SelectExitEventArgs args)
        {
            Ocupado = false;
            if (Cambiado != null) Cambiado(this);
        }
    }
}
