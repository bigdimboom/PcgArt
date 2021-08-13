using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class PcgViewer : MonoBehaviour
{
    private PcgDownloader pcgDownloader;
    public Mesh pointMesh;
    public Material pointMaterial;
    public float volumeSize = 6;
    public bool useRealColor = false;

    [Range(0.0001f, 1.5f)] public float pointSize = 1.0f;

    private static readonly int
        positionsId = Shader.PropertyToID("_VPositions"),
        colorsId = Shader.PropertyToID("_VColors"),
        pointSizeId = Shader.PropertyToID("_PointSize"),
        colorModeId = Shader.PropertyToID("_ColorMode"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        timeId = Shader.PropertyToID("_Time"),
        speedId = Shader.PropertyToID("_Speed");

    private bool started = false;
    private float time = 0.0f;
    private float speed = 7f;
    private const int Resolution = 20000;
    
    
    [SerializeField] ComputeShader computeShader;

    // Start is called before the first frame update
    void Start()
    {
        pcgDownloader = GetComponent<PcgDownloader>();
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pointMesh = cube.GetComponent<MeshFilter>().sharedMesh;
        Destroy(cube);
    }

    IEnumerator Explode()
    {
        yield return new WaitForSeconds(3);
        for (int i = 0; i < 20000; ++i)
        {
            if (time >= 2)
            {
                time = 0;
                speed = -speed;
                // pcgDownloader.Data.vertsCB.SetData(pcgDownloader.Data.verts);
                // yield return new WaitForSeconds(3);
            }
            
            time += Time.deltaTime;
            
            int nVerts = pcgDownloader.Data.verts.Length;
            int h = nVerts / Resolution;
                
            computeShader.SetFloat(timeId, time);
            computeShader.SetFloat(speedId, speed);
            computeShader.SetInt(resolutionId, Resolution);
            var kernel = computeShader.FindKernel("CSMain");
            computeShader.SetBuffer(kernel, "_VPositions",  pcgDownloader.Data.vertsCB);
            computeShader.Dispatch(kernel, Resolution / 8, h / 8, 1);
            yield return null;
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        if (pcgDownloader.Data != null)
        {
            if (started == false)
            {
                started = true;
                StartCoroutine(Explode());
            }
            
            //UpdateFunctionOnGPU();
            var positionsBuffer = pcgDownloader.Data.vertsCB;
            var colorsBuffer = pcgDownloader.Data.vcolorCB;
            pointMaterial.SetBuffer(positionsId, positionsBuffer);
            pointMaterial.SetBuffer(colorsId, colorsBuffer);
            pointMaterial.SetFloat(pointSizeId, pointSize);
            pointMaterial.SetFloat(colorModeId, useRealColor ? 1 : 0);
            var bounds = new Bounds(Vector3.zero, new Vector3(volumeSize, volumeSize, volumeSize));
            Graphics.DrawMeshInstancedProcedural(
                pointMesh, 0, pointMaterial, bounds, positionsBuffer.count
            );
        }
    }

    private void OnDestroy()
    {
        pcgDownloader.Data.vertsCB?.Release();
        pcgDownloader.Data.vcolorCB?.Release();
    }
}