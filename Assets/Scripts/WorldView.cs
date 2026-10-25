using UnityEngine;
using UnityEngine.InputSystem;

// 自動生成した世界を、地形の色と陰影で描く
//   R：シードを変えて作り直す   G：今の大陸を下絵にする／しない   E：地図のはしを 海／荒野 に切り替える
// WorldGen シーンの空の物（World）に付けて使う
public class WorldView : MonoBehaviour
{
    [SerializeField] TextAsset terrainJson;     // 下絵（Assets/Data/terrain.json）
    [SerializeField] int seed = 1;
    [SerializeField] bool useGuide = true;      // 今の大陸を下絵にするか
    [SerializeField] EdgeMode edge = EdgeMode.Ocean;   // 地図のはしをどうするか
    [SerializeField] int width = 360;           // 横のマスの数（今の 90 マスの4倍）
    [SerializeField] int height = 268;          // 縦のマスの数（今の 67 マスの4倍）
    [SerializeField] float cellSize = 4f;       // 1マスが何ユニットか（4 × 360 ＝ 1440 で、今の地図とほぼ同じ広さ）
    [SerializeField] int renderScale = 4;       // 1マスを何ピクセルで描くか（大きいほど寄ったときになめらか）
    [SerializeField] float shading = 6f;        // 陰影の強さ

    TerrainData guide;
    World world;
    Texture2D texture;
    SpriteRenderer spriteRenderer;

    void Start()
    {
        guide = TerrainData.FromJson(terrainJson.text);
        spriteRenderer = new GameObject("WorldMap").AddComponent<SpriteRenderer>();
        spriteRenderer.transform.SetParent(transform, false);
        Build();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.rKey.wasPressedThisFrame)
        {
            seed = Random.Range(1, 1000000);
            Build();
        }
        if (keyboard.gKey.wasPressedThisFrame)
        {
            useGuide = !useGuide;
            Build();
        }
        if (keyboard.eKey.wasPressedThisFrame)
        {
            edge = edge == EdgeMode.Ocean ? EdgeMode.Wasteland : EdgeMode.Ocean;
            Build();
        }
    }

    void Build()
    {
        float t0 = Time.realtimeSinceStartup;
        world = WorldGen.Generate(seed, width, height, cellSize, useGuide ? guide : null, edge);
        float t1 = Time.realtimeSinceStartup;
        Render();
        float t2 = Time.realtimeSinceStartup;
        string edgeName = edge == EdgeMode.Ocean ? "はしは海" : "はしは荒野";
        Debug.Log($"シード {seed}（{(useGuide ? "今の大陸" : "ランダムな大陸")}・{edgeName}）：{Summary()}" +
                  $"　生成 {(t1 - t0) * 1000:F0}ms・描画 {(t2 - t1) * 1000:F0}ms");
    }

    // 地形ごとの割合（陸の中での割合。海は全体の中での割合）
    string Summary()
    {
        var count = new int[System.Enum.GetValues(typeof(LandType)).Length];
        foreach (LandType l in world.Land) count[(int)l]++;
        int total = world.Land.Length, land = total - count[(int)LandType.Sea];
        string s = $"海 {100f * count[(int)LandType.Sea] / total:F0}%｜陸の内訳";
        foreach (LandType l in System.Enum.GetValues(typeof(LandType)))
        {
            if (l == LandType.Sea) continue;
            s += $" {Name(l)} {100f * count[(int)l] / Mathf.Max(1, land):F0}%";
        }
        return s;
    }

    // 1マスを renderScale × renderScale ピクセルで描く。高さは混ぜて（なめらかに）求め、斜面に陰をつける
    void Render()
    {
        int w = width * renderScale, h = height * renderScale;
        if (texture == null || texture.width != w || texture.height != h)
        {
            texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
        }
        var pixels = new Color32[w * h];
        float step = 1f / renderScale;   // 1ピクセルが何マスか

        for (int py = 0; py < h; py++)
        {
            for (int px = 0; px < w; px++)
            {
                float x = (px + 0.5f) * step - 0.5f;   // マスの単位の位置
                float y = (py + 0.5f) * step - 0.5f;
                float e = world.Sample(world.Elevation, x, y);
                float m = world.Sample(world.Moisture, x, y);
                LandType land = world.Sample(world.Barrier, x, y) > 0.5f ? LandType.Wasteland : WorldGen.Classify(e, m);
                Color c = LandColor(land, e);

                if (e >= 0)
                {
                    // 光は左上から：左上より右下が低い（左上を向いた斜面）ほど明るく
                    float slope = world.Sample(world.Elevation, x - step, y - step) - world.Sample(world.Elevation, x + step, y + step);
                    c *= Mathf.Clamp(1 - slope * shading * renderScale, 0.6f, 1.3f);
                    c.a = 1;
                }
                pixels[(h - 1 - py) * w + px] = c;   // 絵は上の行から、テクスチャは下の行から数える
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();

        // 左上を (0, 0) に置く。1ユニットに renderScale / cellSize ピクセル
        spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0, 1), renderScale / cellSize);
    }

    static Color LandColor(LandType land, float elevation)
    {
        switch (land)
        {
            case LandType.Sea:
                // 深いほど暗い青
                return Color.Lerp(new Color(0.30f, 0.52f, 0.70f), new Color(0.10f, 0.20f, 0.40f), Mathf.Clamp01(-elevation / 0.4f));
            case LandType.Plain: return new Color(0.58f, 0.70f, 0.38f);
            case LandType.Forest: return new Color(0.24f, 0.44f, 0.22f);
            case LandType.Hill: return new Color(0.66f, 0.62f, 0.40f);
            case LandType.Mountain: return new Color(0.52f, 0.46f, 0.40f);
            case LandType.Impassable: return new Color(0.92f, 0.93f, 0.96f);   // 雪をかぶった険しい山
            case LandType.Marsh: return new Color(0.38f, 0.50f, 0.42f);
            case LandType.Wasteland: return new Color(0.78f, 0.66f, 0.46f);   // 砂嵐の荒野
        }
        return Color.magenta;
    }

    static string Name(LandType land) => land switch
    {
        LandType.Plain => "平地",
        LandType.Forest => "森",
        LandType.Hill => "丘",
        LandType.Mountain => "山",
        LandType.Impassable => "険しい山",
        LandType.Marsh => "湿地",
        LandType.Wasteland => "荒野",
        _ => "海",
    };
}
