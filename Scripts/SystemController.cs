using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;

public class SystemController : MonoBehaviour
{
    const int NB_INIT_THREADS_PER_GROUP = 256;
    const int NB_UPDATE_THREADS_PER_GROUP = 256;


    [System.Serializable]
    struct BoxPrimUV
    {
        public uint Sourceprim;
        public Vector2 Sourceprimuv;
    }
    [System.Serializable]
    struct BoxRender
    {
        public Vector3 Position;
        public Vector4 Rot;
        public float Pscale;
    }
    [System.Serializable]
    struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 Tangent;
    }

    public GameObject Face;
    public GameObject FaceRef;
    public ComputeShader Compute;

    public Material InstanceMaterial;


    public Mesh InstanceMesh;
    public Vector3 RenderBounds = new Vector3(100.0f, 100.0f, 100.0f);

    public int NumInstance;
    public Mesh DataMesh;


    private ComputeBuffer _BufferVtxId;
    private ComputeBuffer _BufferVertex;
    private ComputeBuffer _BufferBoxPrimUV;
    private ComputeBuffer _BufferBoxRender;

    private ComputeBuffer _BufferArgsRender;
    private uint[] _ArrayArsRender = new uint[5] { 0, 0, 0, 0, 0 };
    private ComputeShader _CS;
    private int _NumThreadGrp;
    private int _KernelId;


    void InitInstancing()
    {
        #region Data decoding
        _BufferBoxPrimUV = new ComputeBuffer(NumInstance, Marshal.SizeOf(typeof(BoxPrimUV)));
        _BufferBoxRender = new ComputeBuffer(NumInstance, Marshal.SizeOf(typeof(BoxRender)));
        var box_primuv_data_array = new BoxPrimUV[NumInstance];
        var box_render_data_array = new BoxRender[NumInstance];


        /*
        var data_res_x = Data0.width;
        int i_x, i_y;
        int triangle_id;
        Vector2 prim_uv = new Vector2(0.0f, 0.0f);
        for (int i = 0; i < NumInstance; i++)
        {
            i_x = i % data_res_x;
            i_y = i / data_res_x;
            var data_0 = Data0.GetPixel(i_x, i_y);
            var data_1 = Data1.GetPixel(i_x, i_y);
            triangle_id = Mathf.FloorToInt(data_0.g * 100.0f) * 100 +
                        Mathf.FloorToInt(data_0.r * 100.0f);
            prim_uv = new Vector2(data_0.b, data_1.r);
            box_primuv_data_array[i].Sourceprim = (uint)triangle_id;
            box_primuv_data_array[i].Sourceprimuv = prim_uv;
            box_render_data_array[i].Pscale = data_1.g;
        }
        _BufferBoxPrimUV.SetData(box_primuv_data_array);
        _BufferBoxRender.SetData(box_render_data_array);
        box_primuv_data_array = null;
        box_render_data_array = null;
        */

        var data_vertices = DataMesh.vertices;

        for (int i = 0; i < data_vertices.Length; i++)
        {
            var vtx_id = Mathf.FloorToInt(data_vertices[i].x * 100.0f);
            var attrib_val = data_vertices[i].y * 100.0f;

            if (data_vertices[i].z > 0.00f && data_vertices[i].z < 0.01f)
            {
            box_primuv_data_array[vtx_id].Sourceprim = (uint)(Mathf.FloorToInt(attrib_val));
            }
            else if (data_vertices[i].z > 0.01f && data_vertices[i].z < 0.02f)
            {
                box_primuv_data_array[vtx_id].Sourceprimuv.x = attrib_val;
            }
            else if (data_vertices[i].z > 0.02f && data_vertices[i].z < 0.03f)
            {
                box_primuv_data_array[vtx_id].Sourceprimuv.y = attrib_val;
            }
            else if (data_vertices[i].z > 0.03f && data_vertices[i].z < 0.04f)
            {
                box_render_data_array[vtx_id].Pscale = attrib_val;
            }
        }
        _BufferBoxPrimUV.SetData(box_primuv_data_array);
        _BufferBoxRender.SetData(box_render_data_array);
        box_primuv_data_array = null;
        box_render_data_array = null;

        #endregion

        #region Instancing prep

        var mesh = FaceRef.GetComponentInChildren<MeshFilter>().sharedMesh;
        var vtx_id_array = mesh.triangles;
        var vtx_pos_array = mesh.vertices;
        _BufferVtxId = new ComputeBuffer(vtx_id_array.Length, Marshal.SizeOf(typeof(int)));
        _BufferVertex = new ComputeBuffer(vtx_pos_array.Length, Marshal.SizeOf(typeof(Vertex)));

        //_BufferVtxId.SetData(vtx_id_array);  // XXX: Can we do this just once here in Init? 

        vtx_id_array = null;
        vtx_pos_array = null;


        _NumThreadGrp = Mathf.CeilToInt(NumInstance / NB_UPDATE_THREADS_PER_GROUP); // XXX: This number should be correct for build
        // This has caused a build error. 

        //Debug.Log(_NumThreadGrp);


        _CS = Compute;
        _KernelId = _CS.FindKernel("Update");
        //_CS.SetBuffer(_KernelId, "_BufferVtxId", _BufferVtxId);


        #endregion


    }

    void UpdateInstancing()
    {


        #region Get transformation of the face mesh
        var matrix_transform = new Matrix4x4();
        if (Face.GetComponentInChildren<MeshRenderer>() != null)
        {
            matrix_transform = Face.GetComponentInChildren<MeshRenderer>().localToWorldMatrix;
            Face.GetComponentInChildren<MeshRenderer>().enabled = false;
        }
        #endregion



        #region Get data from mesh
        var mesh = new Mesh();
        if (Face.GetComponentInChildren<MeshFilter>() != null)
        {
            mesh = Face.GetComponentInChildren<MeshFilter>().sharedMesh;
        }




        var vtx_id_array = mesh.triangles;  // XXX: Should we update every frame?

        //Debug.Log(vtx_id_array.Length);


        var vtx_pos_array = mesh.vertices;
        var vtx_normal_array = mesh.normals;
        mesh.RecalculateTangents();  // XXX : this is required for getting updated tangent
        var vtx_tangent_array = mesh.tangents;
        var num_vertex = vtx_pos_array.Length;
        var vtx_array = new Vertex[num_vertex];
        for (int i = 0; i < num_vertex; i++)
        {
            vtx_array[i].Position = vtx_pos_array[i];
            vtx_array[i].Normal = vtx_normal_array[i];
            vtx_array[i].Tangent = vtx_tangent_array[i];
        }
        _BufferVtxId.SetData(vtx_id_array);
        _BufferVertex.SetData(vtx_array);
        vtx_pos_array = null;
        vtx_normal_array = null;
        vtx_tangent_array = null;
        vtx_array = null;
        vtx_id_array = null;






        _CS.SetBuffer(_KernelId, "_BufferVtxId", _BufferVtxId);
        _CS.SetBuffer(_KernelId, "_BufferVertex", _BufferVertex);
        _CS.SetBuffer(_KernelId, "_BufferBoxPrimUV", _BufferBoxPrimUV);
        _CS.SetBuffer(_KernelId, "_BufferBoxRender", _BufferBoxRender);

        _CS.SetMatrix("_Transform", matrix_transform);




        _CS.Dispatch(_KernelId, _NumThreadGrp, 1, 1);
        #endregion


    }



    void InitRender()
    {
        _BufferArgsRender = new ComputeBuffer(1, _ArrayArsRender.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        //InstanceMaterial.SetBuffer("_PCDataBuffer", _PCDataBuffer);
    }
    void UpdateRender()
    {
        if (InstanceMaterial == null || !SystemInfo.supportsInstancing) return;
        InstanceMaterial.SetBuffer("_BufferBoxRender", _BufferBoxRender);
        uint num_indices = (InstanceMesh != null) ? (uint)InstanceMesh.GetIndexCount(0) : 0;
        _ArrayArsRender[0] = num_indices;
        _ArrayArsRender[1] = (uint)(NumInstance);
        _BufferArgsRender.SetData(_ArrayArsRender);
        var bounds = new Bounds
        (
            transform.position,
            RenderBounds
        );
        Graphics.DrawMeshInstancedIndirect
        (
            mesh: InstanceMesh,
            submeshIndex: 0,
            material: InstanceMaterial,
            bounds: bounds,
            bufferWithArgs: _BufferArgsRender,
            argsOffset: 0,
            properties: null,
            castShadows: UnityEngine.Rendering.ShadowCastingMode.On,
            receiveShadows: true
        //lightProbeUsage: LightProbeUsage.BlendProbe
        );
    }
    void ReleaseBuffer(ComputeBuffer computeBuffer)
    {
        if (computeBuffer != null)
        {
            computeBuffer.Release();
            computeBuffer = null;
        }
    }
    void Start()
    {
        InitInstancing();
        InitRender();
    }
    void Update()
    {

        UpdateInstancing();
        UpdateRender();
    }
    void OnDestroy()
    {
        ReleaseBuffer(_BufferArgsRender);
        ReleaseBuffer(_BufferBoxPrimUV);
        ReleaseBuffer(_BufferBoxRender);
        ReleaseBuffer(_BufferVtxId);
        ReleaseBuffer(_BufferVertex);



    }
}
