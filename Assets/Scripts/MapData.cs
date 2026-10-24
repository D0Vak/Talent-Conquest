using System.Collections.Generic;
using Newtonsoft.Json;

// map.json（国・拠点・道）の形を、そのまま C# にしたもの
// 欄の名前は JSON の名前と同じにしてある（大文字・小文字は区別しない）
public class MapData
{
    [JsonProperty("pos_scale")]
    public float[] PosScale;                        // 拠点の位置の物差し（map.png を 960×720 に縮めたとき）
    public Dictionary<string, NationData> Nations;  // 国の呼び名 → 国
    public Dictionary<string, NodeData> Nodes;      // 拠点の呼び名 → 拠点
    public List<string[]> Roads;                    // 道の一覧。1本が [拠点A, 拠点B]

    // JSON の文字から MapData を作る
    public static MapData FromJson(string text)
    {
        return JsonConvert.DeserializeObject<MapData>(text);
    }
}

public class NationData
{
    public string Name;   // 表示名（オーレハイム王国 など）
    public int[] Color;   // 色 [赤, 緑, 青]（0〜255）
}

public class NodeData
{
    public string Name;      // 表示名（オーレハイム城 など）
    public float[] Pos;      // 位置 [x, y]（PosScale の物差し。y は下に向かって増える）
    public string Terrain;   // 地形（平地・山・湿地 など）
    public bool Castle;      // true なら城、false なら町
    public string Owner;     // 持ち主の国の呼び名
}
