using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Blackout
{
    /// <summary>
    /// Capa haptica del diseno: cada accion responde con una vibracion propia.
    /// Envia el impulso al mando que realmente esta interactuando.
    /// </summary>
    public static class HapticFeedback
    {
        public static void Send(object interactor, float amplitude, float duration)
        {
            if (interactor is XRBaseInputInteractor input)
                input.SendHapticImpulse(Mathf.Clamp01(amplitude), Mathf.Max(0f, duration));
        }
    }
}
