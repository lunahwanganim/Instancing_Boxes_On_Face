using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


public class BakeOutMesh : MonoBehaviour
{

    public GameObject Object;
    private string _FilePathTriangeData = "Assets/FaceBoxes/Data/TriangleIds/TriangleUV.json";
    private bool _Baked = false;

    void BakeMesh()
    {


        var mesh = Object.GetComponentInChildren<MeshFilter>().sharedMesh;


        if (Time.time > 5 && _Baked == false) {

            var vtx_ids = mesh.triangles;
            var num_triangles = vtx_ids.Length / 3;
            var vertices = mesh.vertices;
            var uvs = mesh.uv;
            string str = "";
            str += "[";
            for (int i = 0; i < num_triangles; i++)
            {
                //str += "\n\t{";
                //str += "\n\t\t\"" + i.ToString() + "\" : ";
                str += "\n\t\t\t[";

                str += "\n\t\t\t\t[" + (uvs[vtx_ids[i * 3 + 0]].x).ToString() + ", " +
                    (uvs[vtx_ids[i * 3 + 0]].y).ToString() + ", " +
                    (0.0f).ToString() + "]";
                str += ",";
                str += "\n\t\t\t\t[" + (uvs[vtx_ids[i * 3 + 1]].x).ToString() + ", " +
                    (uvs[vtx_ids[i * 3 + 1]].y).ToString() + ", " +
                    (0.0f).ToString() + "]";
                str += ",";
                str += "\n\t\t\t\t[" + (uvs[vtx_ids[i * 3 + 2]].x).ToString() + ", " +
                    (uvs[vtx_ids[i * 3 + 2]].y).ToString() + ", " +
                    (0.0f).ToString() + "]";

                str += "\n\t\t\t]";
                //str += "\n\t}";

                if (i != num_triangles - 1) str += ",";
            }
            str += "\n]";

            File.WriteAllText(_FilePathTriangeData, str);
            Debug.Log("Baked");


            vtx_ids = null;
            vertices = null;
            uvs = null;



            _Baked = true;





        }

    }



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        BakeMesh();
    }
}
