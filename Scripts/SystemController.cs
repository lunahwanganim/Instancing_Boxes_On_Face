using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;

public class SystemController : MonoBehaviour
{
    const int NB_INIT_THREADS_PER_GROUP = 256;
    [System.Serializable]
    struct PCData
    {
        public Vector3 Position;
    }




    public Material InstanceMaterial;
    public int NumInstance;
    public Mesh InstanceMesh;
    public Vector3 RenderBounds = new Vector3(100.0f, 100.0f, 100.0f);
    public ComputeShader ComputeInit;




    private ComputeBuffer _BufferArgsRender;
    private uint[] _ArrayArsRender = new uint[5] { 0, 0, 0, 0, 0 };
    private ComputeBuffer _PCDataBuffer;
    private ComputeShader _CS;
    private int _NumThreadGrp;
    private int _KernelId;


    [System.Serializable]
    struct BoxPrimUV
    {
        public int Sourceprim;
        public Vector2 Sourceprimuv;
    }

    struct BoxRender
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Tangent;
        public float Pscale;
    }

    public int NumBox;
    public Texture2D Data0;
    public Texture2D Data1;
    private ComputeBuffer _BufferBoxSourceprim;
    private ComputeBuffer _BufferBoxRender;




    void InitDataDecode()
    {
        _BufferBoxSourceprim = new ComputeBuffer(NumBox, Marshal.SizeOf(typeof(BoxPrimUV)));
        _BufferBoxRender = new ComputeBuffer(NumBox, Marshal.SizeOf(typeof(BoxRender)));


        var box_primuv_data_array = new BoxPrimUV[NumBox];
        var box_render_data_array = new BoxRender[NumBox];
        var data_res_x = Data0.width;
        int i_x, i_y;
        int triangle_id;
        Vector2 prim_uv = new Vector2(0.0f, 0.0f);
        for (int i = 0; i < NumBox; i++)
        {
            i_x = i % data_res_x;
            i_y = i / data_res_x;
            var data_0 = Data0.GetPixel(i_x, i_y);
            triangle_id = Mathf.FloorToInt(data_0.g * 100.0f) * 100 +
                        Mathf.FloorToInt(data_0.r * 100.0f);
            prim_uv = new Vector2(data_0.b, data_0.a);
            box_primuv_data_array[i].Sourceprim = triangle_id;
            box_primuv_data_array[i].Sourceprimuv = prim_uv;
            var data_1 = Data1.GetPixel(i_x, i_y);
            box_render_data_array[i].Pscale = data_1.r;
        }
        _BufferBoxSourceprim.SetData(box_primuv_data_array);
        _BufferBoxRender.SetData(box_render_data_array);

        box_primuv_data_array = null;
        box_render_data_array = null;

        /*
        //var data_0 = Data0.



        _PCDataBuffer = new ComputeBuffer(NumInstance, Marshal.SizeOf(typeof(PCData)));
        var pc_data_array = new PCData[NumInstance];
        _PCDataBuffer.SetData(pc_data_array);
        pc_data_array = null;
        _NumThreadGrp = Mathf.CeilToInt(NumInstance / NB_INIT_THREADS_PER_GROUP) + 1;
        _CS = ComputeInit;
        _KernelId = _CS.FindKernel("Init");
        _CS.SetBuffer(_KernelId, "_PCDataBuffer", _PCDataBuffer);
        _CS.Dispatch(_KernelId, _NumThreadGrp, 1, 1);
        */
    }


    void UpdateBoxes()
    {


    }


    void InitCompute()
    {





        _PCDataBuffer = new ComputeBuffer(NumInstance, Marshal.SizeOf(typeof(PCData)));
        var pc_data_array = new PCData[NumInstance];
        _PCDataBuffer.SetData(pc_data_array);
        pc_data_array = null;
        _NumThreadGrp = Mathf.CeilToInt(NumInstance / NB_INIT_THREADS_PER_GROUP) + 1;
        _CS = ComputeInit;
        _KernelId = _CS.FindKernel("Init");
        _CS.SetBuffer(_KernelId, "_PCDataBuffer", _PCDataBuffer);
        _CS.Dispatch(_KernelId, _NumThreadGrp, 1, 1);
    }
    void UpdateCompute()
    {
    }
    void InitRender()
    {
        _BufferArgsRender = new ComputeBuffer(1, _ArrayArsRender.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        //InstanceMaterial.SetBuffer("_PCDataBuffer", _PCDataBuffer);
    }
    void UpdateRender()
    {
        if (InstanceMaterial == null || !SystemInfo.supportsInstancing) return;
        InstanceMaterial.SetBuffer("_PCDataBuffer", _PCDataBuffer);
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
        InitDataDecode();
        InitCompute();
        InitRender();
    }
    void Update()
    {
        UpdateCompute();
        UpdateRender();
    }
    void OnDestroy()
    {
        ReleaseBuffer(_BufferArgsRender);
        ReleaseBuffer(_PCDataBuffer);


        ReleaseBuffer(_BufferBoxSourceprim);
        ReleaseBuffer(_BufferBoxRender);

        



    }
}
