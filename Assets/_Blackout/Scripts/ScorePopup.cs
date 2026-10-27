using TMPro;
using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// El "+25" que sube y se apaga donde hiciste el gesto. Se crea por codigo
    /// para no depender de un prefab: es texto y poco mas.
    /// Siempre mira a la cabeza, asi se lee desde cualquier lado.
    /// </summary>
    public class ScorePopup : MonoBehaviour
    {
        private TextMeshPro texto;
        private Transform cabeza;
        private Vector3 origen;
        private Color color;
        private float vida;
        private float subida;
        private float t;

        public static ScorePopup Lanzar(TMP_FontAsset fuente, string mensaje, Color color,
                                        Vector3 donde, float tam, float segundos, float subida)
        {
            var go = new GameObject("Popup_Puntos");
            go.transform.position = donde;

            var tmp = go.AddComponent<TextMeshPro>();
            if (fuente != null) tmp.font = fuente;
            tmp.text = mensaje;
            tmp.fontSize = tam;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(1f, 0.3f);

            var p = go.AddComponent<ScorePopup>();
            p.texto = tmp;
            p.origen = donde;
            p.color = color;
            p.vida = Mathf.Max(0.1f, segundos);
            p.subida = subida;
            if (Camera.main != null) p.cabeza = Camera.main.transform;
            p.Orientar();
            return p;
        }

        private void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / vida);

            // Sube rapido al principio y se frena, como humo.
            float sube = 1f - (1f - k) * (1f - k);
            transform.position = origen + Vector3.up * (subida * sube);
            Orientar();

            // Se mantiene legible la mitad de su vida y luego se apaga.
            float alfa = k < 0.5f ? 1f : 1f - (k - 0.5f) / 0.5f;
            texto.color = new Color(color.r, color.g, color.b, color.a * alfa);

            if (k >= 1f) Destroy(gameObject);
        }

        private void Orientar()
        {
            if (cabeza == null) return;
            // TextMeshPro se lee mirando en la direccion de su forward.
            Vector3 dir = transform.position - cabeza.position;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}
