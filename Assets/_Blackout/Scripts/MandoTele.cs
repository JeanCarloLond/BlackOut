using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Blackout
{
    /// <summary>
    /// 07 El televisor. El documento dice «encenderla y cambiar de canal con
    /// el control»: los botones de la carcasa siguen ahi, pero ahora tambien
    /// hay un mando sobre la barra. Se agarra y con el gatillo cambia de
    /// canal; si la tele esta apagada, el primer gatillo la enciende.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class MandoTele : MonoBehaviour
    {
        [SerializeField] private Television tele;
        [Tooltip("Luz roja que parpadea al pulsar. Opcional.")]
        [SerializeField] private Renderer piloto;

        private XRGrabInteractable agarre;
        private MaterialPropertyBlock bloque;
        private float apagarPiloto;

        private void Awake()
        {
            agarre = GetComponent<XRGrabInteractable>();
            if (tele == null) tele = FindAnyObjectByType<Television>();
            bloque = new MaterialPropertyBlock();
            Piloto(false);
        }

        private void OnEnable() { agarre.activated.AddListener(OnGatillo); }
        private void OnDisable() { agarre.activated.RemoveListener(OnGatillo); }

        private void OnGatillo(ActivateEventArgs args)
        {
            if (tele == null) return;
            if (tele.Encendida) tele.SiguienteCanal(args.interactorObject);
            else tele.Encender(args.interactorObject);
            Piloto(true);
            apagarPiloto = Time.time + 0.15f;
        }

        private void Update()
        {
            if (apagarPiloto > 0f && Time.time > apagarPiloto)
            {
                apagarPiloto = 0f;
                Piloto(false);
            }
        }

        private void Piloto(bool encendido)
        {
            if (piloto == null) return;
            piloto.GetPropertyBlock(bloque);
            bloque.SetColor("_EmissionColor", encendido ? new Color(4f, 0.2f, 0.1f) : Color.black);
            piloto.SetPropertyBlock(bloque);
        }
    }
}
