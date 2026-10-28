using System.Text;
using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// La pizarra de la noche, junto a la de precios. Es la lista de apertura
    /// del dueno escrita a tiza: cada estacion con lo que lleva, el total y,
    /// al abrir, como salio la noche. Va en la pared y no en un HUD porque
    /// todo en el bar es diegetico.
    /// </summary>
    public class BarScoreBoard : MonoBehaviour
    {
        [SerializeField] private BarScore puntos;

        [Header("Textos")]
        [SerializeField] private TMPro.TMP_Text titulo;
        [Tooltip("Una linea por estacion.")]
        [SerializeField] private TMPro.TMP_Text lineas;
        [SerializeField] private TMPro.TMP_Text total;
        [Tooltip("Antes de abrir dice que el bar sigue cerrado; despues, como salio la noche.")]
        [SerializeField] private TMPro.TMP_Text veredicto;

        [Header("Estilo")]
        [SerializeField] private string[] nombresEstacion =
        {
            "LA ENTRADA", "LUCES", "LA BARRA", "TOCADISCOS", "ESCENARIO",
            "POSTERS", "TELEVISOR", "DARDOS", "ABRIR"
        };
        [Tooltip("Estacion completa: se repasa con el rojo del neon.")]
        [SerializeField] private Color colorHecho = new Color(1f, 0.32f, 0.2f);
        [Tooltip("Lo que falta queda en tiza tenue.")]
        [SerializeField] private Color colorPendiente = new Color(0.85f, 0.83f, 0.78f, 0.45f);
        [SerializeField] private Color colorEnCurso = new Color(0.85f, 0.83f, 0.78f, 0.9f);

        private readonly StringBuilder sb = new StringBuilder();
        private BarStateManager estado;

        private void Start()
        {
            if (puntos == null) puntos = BarScore.Instance;
            if (puntos != null)
            {
                puntos.Sumado += OnSumado;
                puntos.Calificada += OnCalificada;
            }
            estado = BarStateManager.Instance;
            if (estado != null) estado.PhaseChanged += OnFase;
            Escribir();
        }

        private void OnDestroy()
        {
            if (estado != null) estado.PhaseChanged -= OnFase;
            if (puntos == null) return;
            puntos.Sumado -= OnSumado;
            puntos.Calificada -= OnCalificada;
        }

        private void OnSumado(Hito hito, int suma) { Escribir(); }
        private void OnCalificada(Noche noche) { Escribir(); }
        private void OnFase(BarPhase fase) { Escribir(); }

        private void Escribir()
        {
            // TextMeshPro no recibe luz: con el bar a oscuras la tiza brillaria
            // sola en la pared. Se deja ver cuando hay luz, como todo lo demas.
            bool luz = estado == null || estado.HasLight;
            Visible(titulo, luz);
            Visible(lineas, luz);
            Visible(total, luz);
            Visible(veredicto, luz);

            if (puntos == null) return;

            if (lineas != null)
            {
                sb.Length = 0;
                for (int i = 0; i < nombresEstacion.Length; i++)
                {
                    int estacion = i + 1;
                    int max = puntos.MaximoEn(estacion);
                    int lleva = puntos.GanadoEn(estacion);

                    Color c = lleva >= max && max > 0 ? colorHecho : lleva > 0 ? colorEnCurso : colorPendiente;
                    sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGBA(c)).Append('>');
                    sb.Append(estacion.ToString("00")).Append("  ").Append(nombresEstacion[i]);
                    sb.Append("<pos=60%>").Append(lleva).Append('/').Append(max);
                    sb.Append("</color>\n");
                }
                lineas.text = sb.ToString();
            }

            if (total != null) total.text = puntos.Total.ToString();

            if (veredicto != null)
            {
                string arriba = puntos.Calificado ? BarScore.Nombre(puntos.NocheFinal) : "EL BAR SIGUE CERRADO";
                veredicto.text = arriba + "\n<size=60%>MEJOR NOCHE  " + puntos.Record + "</size>";
            }
        }

        private static void Visible(TMPro.TMP_Text t, bool si)
        {
            if (t != null && t.enabled != si) t.enabled = si;
        }
    }
}
