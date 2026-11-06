#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CriarMenuSelecaoMinigames
{
    [MenuItem("Tools/Indústria/Criar MENU_ROOT de Minigames")]
    public static void Criar()
    {
        MenuSelecaoMinigames existente = Object.FindObjectOfType<MenuSelecaoMinigames>();
        if (existente != null)
        {
            Selection.activeGameObject = existente.gameObject;
            EditorGUIUtility.PingObject(existente.gameObject);
            Debug.Log("MENU_ROOT já existe na cena. O objeto existente foi selecionado.");
            return;
        }

        GameObject root = new GameObject("MENU_ROOT");
        Undo.RegisterCreatedObjectUndo(root, "Criar MENU_ROOT de Minigames");
        root.AddComponent<MenuSelecaoMinigames>();
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        Debug.Log("MENU_ROOT criado. Configure as cenas e os AudioClips no Inspector e pressione Play.");
    }
}
#endif
