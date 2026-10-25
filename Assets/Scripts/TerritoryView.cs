using System.Collections.Generic;
using UnityEngine;

// 領地の塗り分け（HOI4 風）：地図の上に国の色を半透明で重ねる。国境は太く濃く、地域の境目は細く
//   地域：陸の各点を、いちばん近い拠点のものにする（Python 版と同じ決め方）
//   これを地図の絵の pixelStep ピクセルごとに決めるので、Python 版（16ピクセルのマスごと）より国境がなめらか
//   拠点の持ち主が変わると、塗り直す（地域ごとまとめて色が変わる）
// MapView と同じ物に付けて使う
public class TerritoryView : MonoBehaviour
{
    [SerializeField] TextAsset terrainJson;                  // Assets/Data/terrain.json（海を塗らないために使う）
    [SerializeField] int pixelStep = 2;                      // 地図の絵の何ピクセルを、塗り分けの1ピクセルにするか
    [SerializeField, Range(0, 1)] float fillAlpha = 0.35f;   // 領地の塗りの濃さ
    [SerializeField, Range(0, 1)] float regionLineAlpha = 0.5f;   // 地域の境目の線の濃さ
    [SerializeField, Range(0, 1)] float borderAlpha = 0.9f;       // 国境の線の濃さ
    [SerializeField] int borderWidth = 2;                    // 国境の太さ（塗り分けのピクセル）

    MapView mapView;
    int width, height;     // 塗り分けの絵の大きさ
    int[] region;          // 塗り分けの1ピクセル → 地域（拠点の番号）。海は -1
    float[] land;          // 塗り分けの1ピクセル → 陸らしさ（0 海 〜 1 陸）。海岸をなめらかにする
    string[] nodeIds;      // 拠点の番号 → 呼び名
    Texture2D texture;
    Color32[] pixels;

    void Start()
    {
        mapView = GetComponent<MapView>();
        TerrainData terrain = TerrainData.FromJson(terrainJson.text);

        width = Mathf.CeilToInt(mapView.MapSize.x / pixelStep);
        height = Mathf.CeilToInt(mapView.MapSize.y / pixelStep);
        nodeIds = new List<string>(mapView.Map.Nodes.Keys).ToArray();

        float startTime = Time.realtimeSinceStartup;
        bool[,] landTiles = LandTiles(terrain);
        BuildLand(terrain, landTiles);
        BuildRegions();
        Debug.Log($"領地の地域分けにかかった時間：{(Time.realtimeSinceStartup - startTime) * 1000:F0} ミリ秒");

        texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,   // 拡大したとき、ピクセルの境目をなめらかに混ぜる
            wrapMode = TextureWrapMode.Clamp,
        };
        pixels = new Color32[width * height];
        MakeSprite();
        Refresh();

        mapView.OwnerChanged += _ => Refresh();   // 拠点の持ち主が変わるたびに塗り直す
    }

    // マスごとに陸か海か。拠点のあるマスは、海でも陸にする（島など。Python 版と同じ）
    bool[,] LandTiles(TerrainData terrain)
    {
        var tiles = new bool[terrain.Columns, terrain.RowCount];
        for (int row = 0; row < terrain.RowCount; row++)
        {
            for (int col = 0; col < terrain.Columns; col++)
            {
                tiles[col, row] = !terrain.IsSea(col, row);
            }
        }
        foreach (string id in nodeIds)
        {
            Vector2 p = ImagePos(id);
            int col = Mathf.Clamp((int)p.x / terrain.Tile, 0, terrain.Columns - 1);
            int row = Mathf.Clamp((int)p.y / terrain.Tile, 0, terrain.RowCount - 1);
            tiles[col, row] = true;
        }
        return tiles;
    }

    // 陸らしさ：まわりの4マスの「陸なら1・海なら0」を、距離に応じて混ぜる（双線形補間）
    // マスの角でカクカクしていた海岸が、なめらかな曲線になる
    void BuildLand(TerrainData terrain, bool[,] tiles)
    {
        land = new float[width * height];
        float tile = terrain.Tile;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // この点が、マスの中心から見てどこにあるか（マスの単位で）
                float tx = (x + 0.5f) * pixelStep / tile - 0.5f;
                float ty = (y + 0.5f) * pixelStep / tile - 0.5f;
                int c0 = Mathf.FloorToInt(tx), r0 = Mathf.FloorToInt(ty);
                float fx = tx - c0, fy = ty - r0;

                float top = Mathf.Lerp(LandValue(tiles, c0, r0), LandValue(tiles, c0 + 1, r0), fx);
                float bottom = Mathf.Lerp(LandValue(tiles, c0, r0 + 1), LandValue(tiles, c0 + 1, r0 + 1), fx);
                float v = Mathf.Lerp(top, bottom, fy);
                land[y * width + x] = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.4f, 0.6f, v));   // 0.5 を境に、少しだけぼかす
            }
        }
    }

    static float LandValue(bool[,] tiles, int col, int row)
    {
        if (col < 0 || row < 0 || col >= tiles.GetLength(0) || row >= tiles.GetLength(1)) return 0;
        return tiles[col, row] ? 1 : 0;
    }

    // 地域：陸の各ピクセルを、いちばん近い拠点の番号にする
    void BuildRegions()
    {
        int n = nodeIds.Length;
        var nx = new float[n];
        var ny = new float[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 p = ImagePos(nodeIds[i]);
            nx[i] = p.x;
            ny[i] = p.y;
        }

        region = new int[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (land[index] <= 0) { region[index] = -1; continue; }

                float px = (x + 0.5f) * pixelStep, py = (y + 0.5f) * pixelStep;
                int best = 0;
                float bestDist = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    float dx = nx[i] - px, dy = ny[i] - py;
                    float d = dx * dx + dy * dy;   // 比べるだけなので、ルートはとらなくてよい
                    if (d < bestDist) { bestDist = d; best = i; }
                }
                region[index] = best;
            }
        }
    }

    // 拠点の持ち主に合わせて、塗り分けの絵を作り直す
    void Refresh()
    {
        // 拠点の番号 → 持ち主の国の番号と色（国どうしを比べるのを速くするため、番号にしておく）
        var nationIndex = new Dictionary<string, int>();
        var owner = new int[nodeIds.Length];
        var color = new Color[nodeIds.Length];
        for (int i = 0; i < nodeIds.Length; i++)
        {
            string nation = mapView.Map.Nodes[nodeIds[i]].Owner;
            if (!nationIndex.ContainsKey(nation)) nationIndex[nation] = nationIndex.Count;
            owner[i] = nationIndex[nation];
            color[i] = mapView.NationColor(nation);
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                int r = region[index];
                int texIndex = (height - 1 - y) * width + x;   // 絵は上の行から、テクスチャは下の行から数える
                if (r < 0) { pixels[texIndex] = new Color32(0, 0, 0, 0); continue; }

                Color c = color[r];
                float alpha;
                if (IsNationBorder(x, y, owner[r], owner))
                {
                    c *= 0.55f;            // 国境：国の色を暗くして、濃く
                    alpha = borderAlpha;
                }
                else if (IsRegionBorder(x, y, r))
                {
                    c *= 0.45f;            // 地域の境目：暗く、うすく
                    alpha = regionLineAlpha;
                }
                else
                {
                    alpha = fillAlpha;     // 中：うすく塗る
                }
                c.a = alpha * land[index];
                pixels[texIndex] = c;
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
    }

    // (x, y) から borderWidth 以内の上下左右に、別の国の陸があるか
    bool IsNationBorder(int x, int y, int myOwner, int[] owner)
    {
        for (int d = 1; d <= borderWidth; d++)
        {
            if (OtherOwner(x - d, y, myOwner, owner) || OtherOwner(x + d, y, myOwner, owner) ||
                OtherOwner(x, y - d, myOwner, owner) || OtherOwner(x, y + d, myOwner, owner)) return true;
        }
        return false;
    }

    bool OtherOwner(int x, int y, int myOwner, int[] owner)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return false;
        int r = region[y * width + x];
        return r >= 0 && owner[r] != myOwner;
    }

    // (x, y) のすぐ右か下が、別の地域か（両側を塗ると太くなるので、片側だけ見る）
    bool IsRegionBorder(int x, int y, int myRegion)
    {
        return OtherRegion(x + 1, y, myRegion) || OtherRegion(x, y + 1, myRegion);
    }

    bool OtherRegion(int x, int y, int myRegion)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return false;
        int r = region[y * width + x];
        return r >= 0 && r != myRegion;
    }

    // 塗り分けの絵を、地図の絵にぴったり重なるように置く（左上を (0, 0) に）
    void MakeSprite()
    {
        var go = new GameObject("Territory");
        go.transform.SetParent(transform, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0, 1), 1f / pixelStep);
        sr.sortingOrder = 1;   // 地図（0）の上、道（2）の下
    }

    // 拠点の、地図の絵の上での位置（ピクセル。y は下に向かって増える）
    Vector2 ImagePos(string id)
    {
        Vector3 p = mapView.NodePos(id);
        return new Vector2(p.x, -p.y);
    }
}
