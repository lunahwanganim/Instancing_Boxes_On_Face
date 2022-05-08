using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;

public class GPUInstancing: MonoBehaviour
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
    }
}
