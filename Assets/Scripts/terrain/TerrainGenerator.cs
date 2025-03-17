using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEditor.PackageManager.UI;

public class TerrainGenerator : MonoBehaviour
{

    [SerializeField] private int texWidth;
    [SerializeField] private int texHeight;
    [SerializeField] private int seed;
    [SerializeField] private float waterLevel;
    [SerializeField] private Terrain terrain;
    [SerializeField] private float heightMul = 0.05f;

    private float sampleXOrg = 0f;
    private float sampleYOrg = 0f;
    private float terrainMaximumHeight;
    private float waterOffset = 0.03f;

    private Vector2 backupWater = new Vector2(0, 0);
    //private float scale;

    private Texture2D noiseTex;
    private Texture2D terrainOverlay;
    private Texture2D oldTerrainTex;
    private Color[] pix;
    private Renderer rend;
    private float[,] heightMap;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int res = terrain.terrainData.heightmapResolution;
        terrainMaximumHeight = terrain.terrainData.heightmapScale.y;
        Debug.Log(string.Format("Terrain resolution: {0}, heightMap maximum height: {1}", res, terrainMaximumHeight));

        waterOffset = 3 / terrainMaximumHeight;
        rend = GetComponent<Renderer>();

        noiseTex = new Texture2D(res, res);
        pix = new Color[noiseTex.width * noiseTex.height];
        rend.material.mainTexture = noiseTex;

        CalcNoise();
        Debug.Log(string.Format("Backup water coordinate: ({0}, {1})", backupWater.y, backupWater.x));

    }

    // Update is called once per frame
    void Update()
    {

    }

    void CalcNoise()
    {
        var random = new System.Random(seed);
        float scale = 0.5f;
        int iterCount = 5;
        float[] mid = new float[noiseTex.width * noiseTex.height];
        heightMap = new float[noiseTex.height, noiseTex.width];

        for (int iter = 0; iter < iterCount; iter++)
        {
            sampleXOrg = random.Next(0, 65536);
            sampleYOrg = random.Next(0, 65536);
            for (float y = 0f; y < noiseTex.height; y++)
            {
                for (float x = 0f; x < noiseTex.width; x++)
                {
                    float xCoord = sampleXOrg + x / noiseTex.width * scale;
                    float yCoord = sampleYOrg + y / noiseTex.height * scale;
                    float sample = Mathf.Clamp(Mathf.PerlinNoise(xCoord, yCoord), 0f, 1f) / iterCount;
                    int pixAddr = (int)y * noiseTex.width + (int)x;
                    //pix[pixAddr].a = 1;
                    //pix[pixAddr].r += sample;
                    //pix[pixAddr].b += sample;
                    //pix[pixAddr].g += sample;
                    mid[pixAddr] += sample;
                }
            }
            scale *= 2;
        }

        for (int p = 0; p < mid.Length; p++)
        {
            pix[p].a = 1f;
            if (mid[p] > waterLevel)
            {
                pix[p].g = 1f;
                pix[p].r = mid[p];
                pix[p].b = mid[p];
            } else
            {
                pix[p].b = 2 * mid[p];
                pix[p].r = mid[p];
                pix[p].g = mid[p];
            }

            int y = p / noiseTex.height;
            int x = p % noiseTex.height;

            heightMap[y, x] = waterLevel + (mid[p] - waterLevel) * heightMul;
            if (backupWater == new Vector2(0, 0) && y != 0 && x != 0 && heightMap[y, x] < waterLevel - waterOffset)
            {
                backupWater = new Vector2((int)y, (int)x);
            }
        }

        Render();
    }

    void Render()
    {
        noiseTex.SetPixels(pix);
        noiseTex.Apply();

        terrainOverlay = new Texture2D(terrain.terrainData.baseMapResolution, terrain.terrainData.baseMapResolution);

        terrain.terrainData.SetHeights(0, 0, heightMap);

        TerrainLayer[] layers = (terrain.terrainData != null) ? terrain.terrainData.terrainLayers : null;
        if (layers != null && layers.Length > 0)
        {
            oldTerrainTex = layers[0].diffuseTexture;
            layers[0].diffuseTexture = noiseTex;            
            Debug.Log("Trying to modify terrain texture.");
        } 
        else if (layers != null && layers.Length == 0)
        {
            Debug.Log("Terrain data has valid terrainLayers but contains no layers!");
        }
        else if (layers == null)
        {
            Debug.Log("Terraindata.terrainLayers is null!");
        }
    }

    private void OnDestroy()
    {
        //TerrainLayer[] layers = (terrain.terrainData != null) ? terrain.terrainData.terrainLayers : null;
        //if (layers != null && layers.Length > 0)
        //{
        //    layers[0].diffuseTexture = oldTerrainTex;
        //}
    }

    public float[,] GetHeightMap()
    {
        return heightMap;
    }
}
