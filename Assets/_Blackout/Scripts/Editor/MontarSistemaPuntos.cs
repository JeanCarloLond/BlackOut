using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Blackout.Herramientas
{
    /// <summary>
    /// Monta el sistema de puntos en la escena abierta: BarScore en el
    /// GameManager y la pizarra de la noche junto a la de precios.
    ///
    /// Existe porque el resto de la escena se construyo por el MCP y aqui
    /// hace falta poder rehacerlo sin el. Es idempotente: si la pizarra ya
    /// existe la borra y la vuelve a levantar.
    /// </summary>
    public static class MontarSistemaPuntos
    {
        private const string RutaSfx = "Assets/_Blackout/Audio/SFX/";
        private const string RutaMaterial = "Assets/_Blackout/Materials/Mat_Pizarra_Noche.mat";
        private const string NombrePizarra = "Pizarra_Noche";

        // Junto a la de precios, sobre el estante retroiluminado. La de
        // precios esta en z = 1.5; esta queda centrada sobre la barra.
        private const float SeparacionDePrecios = 1.5f;
        private const float Ancho = 0.9f;
        private const float Alto = 1.06f;

        private static readonly Color Tiza = new Color(0.85f, 0.83f, 0.78f, 0.9f);
        private static readonly Color Neon = new Color(1f, 0.32f, 0.2f);

        [MenuItem("Blackout/Montar sistema de puntos")]
        public static void Montar()
        {
            var gm = GameObject.Find("GameManager");
            if (gm == null) { Debug.LogError("[Puntos] No hay GameManager en la escena abierta."); return; }

            var fuente = BuscarFuente();
            var puntos = MontarBarScore(gm, fuente);
            var pizarra = MontarPizarra(puntos, fuente);

            EditorSceneManager.MarkSceneDirty(gm.scene);
            Selection.activeGameObject = pizarra;
            Debug.Log("[Puntos] Montado. Revisa la pizarra en la escena y guarda (Ctrl+S).");
        }

        [MenuItem("Blackout/Borrar mejor noche")]
        public static void BorrarRecord()
        {
            BarScore.BorrarRecord();
            Debug.Log("[Puntos] Mejor noche borrada.");
        }

        private static TMP_FontAsset BuscarFuente()
        {
            // La misma tiza que el marcador de los dardos.
            var marcador = GameObject.Find("Marcador_Numero");
            var tmp = marcador != null ? marcador.GetComponent<TMP_Text>() : null;
            if (tmp != null && tmp.font != null) return tmp.font;
            return TMP_Settings.defaultFontAsset;
        }

        private static BarScore MontarBarScore(GameObject gm, TMP_FontAsset fuente)
        {
            var puntos = gm.GetComponent<BarScore>();
            if (puntos == null) puntos = Undo.AddComponent<BarScore>(gm);

            var hijo = gm.transform.Find("Puntos_SFX");
            if (hijo == null)
            {
                var go = new GameObject("Puntos_SFX");
                Undo.RegisterCreatedObjectUndo(go, "Puntos_SFX");
                go.transform.SetParent(gm.transform, false);
                hijo = go.transform;
            }
            var fuenteAudio = hijo.GetComponent<AudioSource>();
            if (fuenteAudio == null) fuenteAudio = Undo.AddComponent<AudioSource>(hijo.gameObject);
            fuenteAudio.playOnAwake = false;
            fuenteAudio.spatialBlend = 0f; // recompensa, no objeto: se oye igual en todo el local
            fuenteAudio.volume = 0.8f;

            var so = new SerializedObject(puntos);
            so.FindProperty("fuente").objectReferenceValue = fuente;
            so.FindProperty("sfx").objectReferenceValue = fuenteAudio;
            so.FindProperty("clipPunto").objectReferenceValue = Clip("SFX_Puntaje.wav");
            so.FindProperty("clipVeredicto").objectReferenceValue = Clip("SFX_Barra_Ordenada.wav");
            so.ApplyModifiedPropertiesWithoutUndo();
            return puntos;
        }

        private static GameObject MontarPizarra(BarScore puntos, TMP_FontAsset fuente)
        {
            var vieja = GameObject.Find(NombrePizarra);
            if (vieja != null) Undo.DestroyObjectImmediate(vieja);

            var precios = GameObject.Find("Pizarra_Precios");
            var raiz = new GameObject(NombrePizarra);
            Undo.RegisterCreatedObjectUndo(raiz, NombrePizarra);

            Material matMarco = null;
            if (precios != null)
            {
                raiz.transform.SetParent(precios.transform.parent, false);
                raiz.transform.rotation = precios.transform.rotation;
                raiz.transform.position = precios.transform.position + precios.transform.right * SeparacionDePrecios;

                var marcoPrecios = precios.transform.Find("Marco");
                var r = marcoPrecios != null ? marcoPrecios.GetComponent<Renderer>() : null;
                if (r != null) matMarco = r.sharedMaterial;
            }
            else
            {
                Debug.LogWarning("[Puntos] No encontre Pizarra_Precios: la pizarra queda en el origen, colocala a mano.");
            }

            // Misma construccion que la de precios: marco detras y tabla delante,
            // con la cara +Z hacia la sala (la -Z de un cubo sale invertida).
            Cubo("Marco", raiz.transform, new Vector3(0f, 0f, -0.02f), new Vector3(Ancho, Alto, 0.05f), matMarco);
            Cubo("Tabla", raiz.transform, new Vector3(0f, 0f, 0.008f), new Vector3(Ancho - 0.08f, Alto - 0.08f, 0.01f), MaterialTabla());

            const float z = 0.017f;
            var titulo = Texto("Titulo", raiz.transform, new Vector3(0f, 0.40f, z), new Vector2(0.78f, 0.12f),
                               1.0f, TextAlignmentOptions.Center, Tiza, fuente);
            titulo.text = "ESTA NOCHE";

            var lineas = Texto("Lineas", raiz.transform, new Vector3(0f, 0.07f, z), new Vector2(0.76f, 0.52f),
                               0.48f, TextAlignmentOptions.TopLeft, Tiza, fuente);
            var total = Texto("Total", raiz.transform, new Vector3(0f, -0.26f, z), new Vector2(0.78f, 0.14f),
                              1.3f, TextAlignmentOptions.Center, Neon, fuente);
            var veredicto = Texto("Veredicto", raiz.transform, new Vector3(0f, -0.41f, z), new Vector2(0.78f, 0.12f),
                                  0.6f, TextAlignmentOptions.Center, Tiza, fuente);

            var tablero = raiz.AddComponent<BarScoreBoard>();
            var so = new SerializedObject(tablero);
            so.FindProperty("puntos").objectReferenceValue = puntos;
            so.FindProperty("titulo").objectReferenceValue = titulo;
            so.FindProperty("lineas").objectReferenceValue = lineas;
            so.FindProperty("total").objectReferenceValue = total;
            so.FindProperty("veredicto").objectReferenceValue = veredicto;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Texto de muestra para ver la composicion en el editor; en Play lo reescribe la pizarra.
            lineas.text = "01  LA ENTRADA<pos=60%>0/50\n02  LUCES<pos=60%>0/200\n03  LA BARRA<pos=60%>0/280\n" +
                          "04  TOCADISCOS<pos=60%>0/125\n05  ESCENARIO<pos=60%>0/150\n06  POSTERS<pos=60%>0/105\n" +
                          "07  TELEVISOR<pos=60%>0/75\n08  DARDOS<pos=60%>0/250\n09  ABRIR<pos=60%>0/100";
            total.text = "0";
            veredicto.text = "EL BAR SIGUE CERRADO\n<size=60%>MEJOR NOCHE  0</size>";

            return raiz;
        }

        private static void Cubo(string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escala;
            // Decorado de pared: no debe estorbar a los rayos de teleport ni a los dardos.
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static TextMeshPro Texto(string nombre, Transform padre, Vector3 pos, Vector2 tam,
                                         float fontSize, TextAlignmentOptions alineacion, Color color, TMP_FontAsset fuente)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            // TextMeshPro se lee mirando hacia su forward: girado 180 para leerse desde la sala.
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var tmp = go.AddComponent<TextMeshPro>();
            if (fuente != null) tmp.font = fuente;
            tmp.fontSize = fontSize;
            tmp.alignment = alineacion;
            tmp.color = color;
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = tam;
            return tmp;
        }

        private static Material MaterialTabla()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (mat != null) return mat;

            // Pizarra lisa: la de precios sin su textura, para que la tiza sea texto vivo.
            var basePizarra = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Blackout/Materials/Mat_Pizarra.mat");
            mat = basePizarra != null ? new Material(basePizarra) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", null);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.07f, 0.08f, 0.075f));
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(mat, RutaMaterial);
            return mat;
        }

        private static AudioClip Clip(string archivo)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaSfx + archivo);
            if (clip == null) Debug.LogWarning("[Puntos] Falta " + RutaSfx + archivo);
            return clip;
        }
    }
}
