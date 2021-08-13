using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleHTTP;

public class PcgDownloader : MonoBehaviour
{
    public string hostName = "127.0.0.1";
    public int hostPort = 1234;
    public string httpPath = "download_pcg";

    private struct PcgBufferData
    {
        public float[] verts;
        public byte[] vcolors;
    }

    private PcgBufferData pcgData;
    private PcgData pcg;

    IEnumerator DownloadPcg()
    {
        // Create the request object
        UriBuilder builder = new UriBuilder(hostName);
        builder.Path = httpPath;
        builder.Port = hostPort;
        builder.Scheme = "http";
        //Debug.Log(builder.ToString());
        Request request = new Request(builder.ToString()).Get();
        //request = new Request("http://127.0.0.1:1234/test");

        // Instantiate the client
        Client http = new Client();
        // Send the request
        yield return http.Send(request);

        var sss = http.Error();
        var kk = http.ToString();

        // Use the response if the request was successful, otherwise print an error
        if (http.IsSuccessful())
        {
            Response resp = http.Response();
            //Debug.Log("status: " + resp.Status().ToString() + "\nbody: " + resp.RawBody());

            var data = resp.RawBody();
            int nVertices = BitConverter.ToInt32(data, 0);
            // Debug.Log(nVertices);

            pcgData.verts = new float[nVertices * 3];
            pcgData.vcolors = new byte[nVertices * 3];
            Buffer.BlockCopy(data, 4, pcgData.verts, 0, nVertices * 3 * sizeof(float)); // x,y,z
            Buffer.BlockCopy(data, 4 + nVertices * 3 * 4, pcgData.vcolors, 0, nVertices * 3 * sizeof(byte)); //r,g,b

            FillData(nVertices);
        }
        else
        {
            Debug.Log("error: " + http.Error());
        }
    }

    void FillData(int nVertices)
    {
        pcg = new PcgData();
        pcg.verts = new Vector3[nVertices];
        pcg.vcolors = new Color[nVertices];

        for (int i = 0; i < nVertices; ++i)
        {
            pcg.verts[i].x = pcgData.verts[i * 3 + 0];
            pcg.verts[i].y = pcgData.verts[i * 3 + 1];
            pcg.verts[i].z = pcgData.verts[i * 3 + 2];

            pcg.vcolors[i].r = pcgData.vcolors[i * 3 + 0] / 255.0f;
            pcg.vcolors[i].g = pcgData.vcolors[i * 3 + 1] / 255.0f;
            pcg.vcolors[i].b = pcgData.vcolors[i * 3 + 2] / 255.0f;
            pcg.vcolors[i].a = 1.0f;
        }

        pcg.vertsCB = new ComputeBuffer(nVertices, 3 * sizeof(float));
        pcg.vcolorCB = new ComputeBuffer(nVertices, 4 * sizeof(float));

        pcg.vertsCB.SetData(pcg.verts);
        pcg.vcolorCB.SetData(pcg.vcolors);
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DownloadPcg());
    }

    public PcgData Data => pcg; // property
    // get method
}