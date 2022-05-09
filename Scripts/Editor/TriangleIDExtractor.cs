using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TriangleIDExtractor : EditorWindow
{


    private GameObject _Object;
    private string _FilePathTriangeData = "Assets/Data/Triangle.json";

    private bool _Has_GameObject;
    private bool _Has_FilePathTriangeData;


    [MenuItem("TriangleID/TriangleIDExtractor")]
    static void OpenWindow()
    {
        //create window
        TriangleIDExtractor window = EditorWindow.GetWindow<TriangleIDExtractor>();
        window.Show();
        window.CheckInput();
    }
    void OnGUI()
    {
        EditorGUILayout.HelpBox("Set the GameObject you want to get the traiangle data from " +
                "and location of the texture you want to bake to, then press the \"Bake\" button.", MessageType.None);

        using (var check = new EditorGUI.ChangeCheckScope())
        {
            _Object = (GameObject)EditorGUILayout.ObjectField("Game Object", _Object, typeof(GameObject), false);
            _FilePathTriangeData = FileField(_FilePathTriangeData);

            if (check.changed)
            {
                CheckInput();
            }
        }

        GUI.enabled = _Has_GameObject && _Has_FilePathTriangeData;

        if (GUILayout.Button("Bake"))
        {
            ExtractTriangleIDs();
        }
        GUI.enabled = true;
        if (!_Has_GameObject)
        {
            EditorGUILayout.HelpBox("No Game Object", MessageType.Warning);
        }
        if (!_Has_FilePathTriangeData)
        {
            EditorGUILayout.HelpBox("No .json to save the triangle data", MessageType.Warning);
        }

    }
    void CheckInput()
    {
        _Has_FilePathTriangeData = false;
        try
        {
            string ext = Path.GetExtension(_FilePathTriangeData);
            _Has_FilePathTriangeData = ext.Equals(".json");
        }
        catch (ArgumentException) { }
        _Has_GameObject = _Object != null;
    }

    string FileField(string path)
    {
        //allow the user to enter output file both as text or via file browser
        EditorGUILayout.LabelField("Image Path");
        using (new GUILayout.HorizontalScope())
        {
            path = EditorGUILayout.TextField(path);
            if (GUILayout.Button("choose"))
            {
                string directory = "Assets";
                string fileName = "Triangle.json";
                try
                {
                    directory = Path.GetDirectoryName(path);
                    fileName = Path.GetFileName(path);
                }
                catch (ArgumentException) { }
                string chosenFile = EditorUtility.SaveFilePanelInProject("Choose json file", fileName,
        "json", "Please enter a file name to save the triangle data to", directory);
                if (!string.IsNullOrEmpty(chosenFile))
                {
                    path = chosenFile;
                }
                //repaint editor because the file changed and we can't set it in the textfield retroactively
                Repaint();
            }
        }
        return path;
    }


    void ExtractTriangleIDs()
    {
 

        var mesh = _Object.GetComponentInChildren<MeshFilter>().sharedMesh;



        var vtx_ids = mesh.triangles;
        var num_triangles = vtx_ids.Length / 3;
        var vertices = mesh.vertices;
        string str = "";
        str += "[";
        for (int i = 0; i < num_triangles; i++)
        {
            //str += "\n\t{";
            //str += "\n\t\t\"" + i.ToString() + "\" : ";
            str += "\n\t\t\t[";
            str += "\n\t\t\t\t[" + (-100.0f * vertices[vtx_ids[i * 3 + 0]].x).ToString() + ", " +
                (100.0f * vertices[vtx_ids[i * 3 + 0]].y).ToString() + ", " +
                (100.0f * vertices[vtx_ids[i * 3 + 0]].z).ToString() + "]";
            str += ",";
            str += "\n\t\t\t\t[" + (-100.0f * vertices[vtx_ids[i * 3 + 1]].x).ToString() + ", " +
                (100.0f * vertices[vtx_ids[i * 3 + 1]].y).ToString() + ", " +
                (100.0f * vertices[vtx_ids[i * 3 + 1]].z).ToString() + "]";
            str += ",";
            str += "\n\t\t\t\t[" + (-100.0f * vertices[vtx_ids[i * 3 + 2]].x).ToString() + ", " +
                (100.0f * vertices[vtx_ids[i * 3 + 2]].y).ToString() + ", " +
                (100.0f * vertices[vtx_ids[i * 3 + 2]].z).ToString() + "]";
            str += "\n\t\t\t]";
            //str += "\n\t}";

            if (i != num_triangles - 1) str += ",";
        }
        str += "\n]";
        
        File.WriteAllText(_FilePathTriangeData, str);

        vtx_ids = null;
        vertices = null;

    }
}