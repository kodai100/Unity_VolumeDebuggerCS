using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class VolumeDebugger : MonoBehaviour {

    public CellData[] datas;
    int num_data;

    public Vector3 dims = new Vector3(40, 40, 40);
    float time;

    #region GPU
    int SIMULATION_BLOCK_SIZE = 40;
    int threadGroupSize;
    public ComputeShader cs;
    ComputeBuffer read;
    ComputeBuffer write;
    #endregion GPU

    public Material displayMat;
    MaterialPropertyBlock _block;
    public MaterialPropertyBlock block {
        get {
            if (_block == null) {
                _block = new MaterialPropertyBlock();
            }
            return _block;
        }
    }

    Mesh mesh;

    void Start () {

        num_data = (int)(dims.x * dims.y * dims.z);

        if ( num_data > 65000) {
            Debug.Log("Simulation dims must be less than 65000");
            return;
        }

        time = 0;

        CreateData();

        mesh = BuildMesh(num_data);

        InitComputeBuffers(num_data);

	}
	
	void Update () {
        time += Time.deltaTime;

        cs.SetFloat("_Time", time);

        int kernel = cs.FindKernel("CSMain");
        cs.Dispatch(kernel, threadGroupSize, 1, 1);

        SwapBuffer(ref read, ref write);

        block.SetBuffer("_Datas", read);
        Graphics.DrawMesh(mesh, transform.localToWorldMatrix, displayMat, 0, null, 0, block);
    }

    void OnDestroy() {
        if (read != null) {
            read.Release();
            read = null;
        }
        if (write != null) {
            write.Release();
            write = null;
        }
    }

    // ここでデータ配列を作成する
    void CreateData() {
        datas = new CellData[num_data];

        for (int z = 0; z < dims.z; z++) {
            for (int y = 0; y < dims.y; y++) {
                for (int x = 0; x < dims.x; x++) {
                    int index = (int)(z * dims.y * dims.x + y * dims.x + x);
                    datas[index] = new CellData(new Vector3(x　- dims.x / 2f, y - dims.y / 2f, z - dims.z / 2f), new Vector3(0.5f, 0.5f, 0.5f) + 0.5f * Random.insideUnitSphere);
                }
            }
        }

    }

    Mesh BuildMesh(int n) {
        Mesh particleMesh = new Mesh();
        particleMesh.name = "VolumeDebugger";

        var vertices = new Vector3[n];
        var uvs = new Vector2[n];
        var indices = new int[n];

        for (int z = 0; z < dims.z; z++) {
            for (int y = 0; y < dims.y; y++) {
                for (int x = 0; x < dims.x; x++) {
                    int index = (int)(z * dims.y * dims.x + y * dims.x + x);
                    vertices[index] = datas[index].position;
                    uvs[index] = new Vector2(0, 0);
                    indices[index] = index;
                }
            }
        }

        particleMesh.vertices = vertices;
        particleMesh.uv = uvs;

        particleMesh.SetIndices(indices, MeshTopology.Points, 0);
        //particleMesh.RecalculateBounds();
        //var bounds = particleMesh.bounds;
        //bounds.size = bounds.size;// * 100f;
        //particleMesh.bounds = bounds;

        return particleMesh;
    }

    void InitComputeBuffers(int num_data) {
        read = new ComputeBuffer(num_data, Marshal.SizeOf(typeof(CellData)));
        write = new ComputeBuffer(num_data, Marshal.SizeOf(typeof(CellData)));

        read.SetData(datas);
        write.SetData(datas);

        threadGroupSize = Mathf.CeilToInt(num_data / SIMULATION_BLOCK_SIZE) + 1;
    }

    void SwapBuffer(ref ComputeBuffer src, ref ComputeBuffer dst) {
        ComputeBuffer tmp = src;
        src = dst;
        dst = tmp;
    }
}

public struct CellData {
    public Vector3 position;
    public Vector3 values;
    
    public CellData(Vector3 position, Vector3 values) {
        this.position = position;
        this.values = values;
    }
}