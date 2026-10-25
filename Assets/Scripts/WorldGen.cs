using UnityEngine;

// 世界の自動生成（段1：高さ、段2：地形）。Unity の画面は使わない、計算だけ
//   同じシード・同じ下絵・同じ設定なら、必ず同じ世界になる
//   下絵（今の大陸の terrain.json）があれば、その輪郭と地形に細かい起伏を足す。なければランダムな大陸を作る

// マスの地形（Unity にも Terrain という名前があるので、LandType にしている）
public enum LandType { Sea, Plain, Forest, Hill, Mountain, Impassable, Marsh, Wasteland }

// 地図のはしをどうするか
public enum EdgeMode
{
    Ocean,       // まわりが海
    Wasteland,   // はしが砂嵐の荒野（通れない）。地続きの大陸が、地図の外へ続いているように見える
}

// 作った世界
public class World
{
    public int Seed;
    public int Width, Height;     // マスの数
    public float CellSize;        // 1マスが何ユニットか（ユニット＝今の地図の絵のピクセル）
    public float[] Elevation;     // マスの高さ（0 が海面。マイナスは海）
    public float[] Moisture;      // マスの湿り気（0〜1）
    public float[] Barrier;       // マスが荒野か（1 荒野・0 ちがう）。絵にするとき混ぜられるように数で持つ
    public LandType[] Land;       // マスの地形

    public int Index(int x, int y) => y * Width + x;

    // マスの単位の位置 (x, y) の値を、まわりの4マスから混ぜて求める（双線形補間）
    public float Sample(float[] values, float x, float y)
    {
        x = Mathf.Clamp(x, 0, Width - 1.001f);
        y = Mathf.Clamp(y, 0, Height - 1.001f);
        int x0 = (int)x, y0 = (int)y;
        float fx = x - x0, fy = y - y0;
        float top = Mathf.Lerp(values[Index(x0, y0)], values[Index(x0 + 1, y0)], fx);
        float bottom = Mathf.Lerp(values[Index(x0, y0 + 1)], values[Index(x0 + 1, y0 + 1)], fx);
        return Mathf.Lerp(top, bottom, fy);
    }
}

public static class WorldGen
{
    // 下絵の地形ごとの、もとの高さと湿り気
    static float GuideElevation(char c) => c switch
    {
        '~' => -0.35f,   // 海
        'S' => 0.04f,    // 湿地
        '.' => 0.10f,    // 平地
        'F' => 0.15f,    // 森
        'H' => 0.32f,    // 丘
        'M' => 0.55f,    // 山
        'X' => 0.80f,    // 険しい山
        _ => 0.10f,
    };

    static float GuideMoisture(char c) => c switch
    {
        'S' => 0.6f,
        'F' => 0.4f,
        _ => 0f,
    };

    public static World Generate(int seed, int width, int height, float cellSize, TerrainData guide, EdgeMode edge)
    {
        var world = new World
        {
            Seed = seed, Width = width, Height = height, CellSize = cellSize,
            Elevation = new float[width * height],
            Moisture = new float[width * height],
            Barrier = new float[width * height],
            Land = new LandType[width * height],
        };

        // シードから、ノイズをどこから読むか（ずらす量）を決める。シードが違えば別の模様になる
        var rng = new System.Random(seed);
        Vector2 warpOffsetX = RandomOffset(rng), warpOffsetY = RandomOffset(rng);
        Vector2 detailOffset = RandomOffset(rng);
        Vector2 ridgeOffset = RandomOffset(rng);
        Vector2 moistOffset = RandomOffset(rng);
        Vector2 shapeOffset = RandomOffset(rng);
        Vector2 edgeOffset = RandomOffset(rng);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // ドメインワープ：ノイズを読む場所を、別のノイズでずらす。海岸や山脈がうねった形になる
                float wx = x + 30f * Fbm(x, y, 1f / 80, 3, warpOffsetX);
                float wy = y + 30f * Fbm(x, y, 1f / 80, 3, warpOffsetY);

                // 地図のはしからの近さ（真ん中 0 〜 はし 1）。四角の地図なので、縦と横の近いほう
                float edgeDist = Mathf.Max(Mathf.Abs(x / (float)(width - 1) * 2 - 1), Mathf.Abs(y / (float)(height - 1) * 2 - 1));

                float baseHeight, baseMoist, mountainness;
                if (guide != null)
                {
                    // 下絵のマス（16ユニット）の上での位置。少しずらして読むので、下絵のマスの角が消える
                    float gx = (x + 0.3f * (wx - x) + 0.5f) * cellSize / guide.Tile - 0.5f;
                    float gy = (y + 0.3f * (wy - y) + 0.5f) * cellSize / guide.Tile - 0.5f;
                    baseHeight = SampleGuide(guide, gx, gy, GuideElevation);
                    baseMoist = SampleGuide(guide, gx, gy, GuideMoisture);
                    mountainness = Mathf.Clamp01(baseHeight / 0.55f);   // 下絵が山に近いほど 1
                }
                else
                {
                    // 下絵なし：大きなノイズで陸と海の形を作る。海のときだけ、はしに近いほど低くする
                    baseHeight = 0.12f + 0.55f * Fbm(wx, wy, 1f / 140, 4, shapeOffset);
                    if (edge == EdgeMode.Ocean)
                    {
                        baseHeight -= 0.6f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.7f, 1f, edgeDist));
                    }
                    baseMoist = 0;
                    mountainness = Mathf.Clamp01(0.3f + baseHeight * 2f);
                }

                // 細かい起伏：海岸線を複雑にする
                float h = baseHeight + 0.28f * Fbm(wx, wy, 1f / 18, 6, detailOffset);
                // 山脈：山の多い所ほど、尾根のするどいノイズを足す
                h += 0.32f * mountainness * Ridged(wx, wy, 1f / 28, 5, ridgeOffset);

                float m = Mathf.Clamp01(0.5f + 0.55f * Fbm(wx, wy, 1f / 45, 4, moistOffset) + baseMoist);

                // はしの荒野：はしに近い所を、ギザギザの帯で荒野にする
                float barrier = 0;
                if (edge == EdgeMode.Wasteland)
                {
                    float ragged = edgeDist + 0.06f * Fbm(x, y, 1f / 12, 3, edgeOffset);
                    barrier = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.88f, 0.94f, ragged));
                    if (barrier > 0) h = Mathf.Max(h, 0.05f * barrier);   // 荒野は海にしない
                }

                int i = world.Index(x, y);
                world.Elevation[i] = h;
                world.Moisture[i] = m;
                world.Barrier[i] = barrier;
                world.Land[i] = barrier > 0.5f ? LandType.Wasteland : Classify(h, m);
            }
        }
        return world;
    }

    // 高さと湿り気から地形を決める（荒野は、はしの帯で決めるので、ここでは出てこない）
    public static LandType Classify(float h, float m)
    {
        if (h < 0) return LandType.Sea;
        if (h < 0.07f) return m > 0.7f ? LandType.Marsh : LandType.Plain;
        if (h < 0.28f) return m > 0.55f ? LandType.Forest : LandType.Plain;
        if (h < 0.45f) return LandType.Hill;
        if (h < 0.72f) return LandType.Mountain;
        return LandType.Impassable;
    }

    static Vector2 RandomOffset(System.Random rng)
    {
        return new Vector2(rng.Next(-10000, 10000), rng.Next(-10000, 10000));
    }

    // 下絵の値を、まわりの4マスから混ぜて求める
    static float SampleGuide(TerrainData guide, float gx, float gy, System.Func<char, float> value)
    {
        int x0 = Mathf.FloorToInt(gx), y0 = Mathf.FloorToInt(gy);
        float fx = gx - x0, fy = gy - y0;
        float top = Mathf.Lerp(GuideValue(guide, x0, y0, value), GuideValue(guide, x0 + 1, y0, value), fx);
        float bottom = Mathf.Lerp(GuideValue(guide, x0, y0 + 1, value), GuideValue(guide, x0 + 1, y0 + 1, value), fx);
        return Mathf.Lerp(top, bottom, fy);
    }

    static float GuideValue(TerrainData guide, int col, int row, System.Func<char, float> value)
    {
        if (guide.IsSea(col, row)) return value('~');
        return value(guide.Rows[row][col]);
    }

    // フラクタルノイズ：大きな波から細かい波まで、octaves 回重ねる。だいたい -1〜1
    static float Fbm(float x, float y, float frequency, int octaves, Vector2 offset)
    {
        float sum = 0, amplitude = 1, total = 0;
        for (int o = 0; o < octaves; o++)
        {
            float n = Mathf.PerlinNoise(x * frequency + offset.x, y * frequency + offset.y) * 2 - 1;
            sum += n * amplitude;
            total += amplitude;
            amplitude *= 0.5f;     // 細かい波ほど小さく
            frequency *= 2f;       // 細かい波ほど密に
        }
        return sum / total;
    }

    // 尾根のノイズ：ふつうのノイズの「0 の線」を山の尾根にする。だいたい 0〜1
    static float Ridged(float x, float y, float frequency, int octaves, Vector2 offset)
    {
        float sum = 0, amplitude = 1, total = 0;
        for (int o = 0; o < octaves; o++)
        {
            float n = Mathf.PerlinNoise(x * frequency + offset.x, y * frequency + offset.y) * 2 - 1;
            float ridge = 1 - Mathf.Abs(n);
            sum += ridge * ridge * amplitude;
            total += amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }
        return sum / total;
    }
}
