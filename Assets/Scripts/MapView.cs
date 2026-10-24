using System.Collections.Generic;
using UnityEngine;

// map.json を読んで、道を線で引き、拠点を国の色の丸で置く（城は大きく、町は小さく）
// シーンの空の物（MapView）に付けて使う
// 重なりの順番（sortingOrder）：地図 0 → 道 1 → 丸のふち 2 → 丸 3 → コマ 5〜7（大きいほど手前）
public class MapView : MonoBehaviour
{
    [SerializeField] TextAsset mapJson;                          // Assets/Data/map.json
    [SerializeField] Sprite circle;                              // 拠点の印にする丸
    [SerializeField] Material lineMaterial;                      // 道の線の塗り方
    [SerializeField] float castleSize = 14f;                     // 城の丸の大きさ（ユニット＝地図の絵のピクセル）
    [SerializeField] float townSize = 8f;                        // 町の丸の大きさ
    [SerializeField] float outline = 3f;                         // 丸のまわりの黒いふちの太さ
    [SerializeField] float roadWidth = 2.5f;                     // 道の線の太さ
    [SerializeField] Color roadColor = new Color(0.25f, 0.18f, 0.1f, 0.85f);  // 道の色（こげ茶、少し透ける）
    [SerializeField] Vector2 mapSize = new Vector2(1448, 1086);  // 地図の絵の大きさ

    public MapData Map { get; private set; }   // ほかのスクリプトからは読むだけ
    public RoadGraph Roads { get; private set; }

    float scale;   // 拠点の位置の物差し → 地図の絵のピクセル（約1.51倍）
    readonly Dictionary<string, SpriteRenderer> nodeCircles = new Dictionary<string, SpriteRenderer>();  // 拠点 → 国の色の丸

    // Awake は、どのスクリプトの Start よりも先に呼ばれる
    // ほかのスクリプトが Start で地図を使えるように、ここで読み込んでおく
    void Awake()
    {
        Map = MapData.FromJson(mapJson.text);
        scale = mapSize.x / Map.PosScale[0];   // 960×720 の点 → 1448×1086 の点
        Roads = new RoadGraph(Map, NodePos);

        foreach (string[] road in Map.Roads)
        {
            MakeLine(road[0] + "-" + road[1], NodePos(road[0]), NodePos(road[1]));
        }

        foreach (var pair in Map.Nodes)
        {
            float size = pair.Value.Castle ? castleSize : townSize;
            MakeCircle(pair.Key + "_outline", NodePos(pair.Key), size + outline, Color.black, 2);
            nodeCircles[pair.Key] = MakeCircle(pair.Key, NodePos(pair.Key), size, NationColor(pair.Value.Owner), 3);
        }
        Debug.Log($"道を {Map.Roads.Count} 本、拠点を {Map.Nodes.Count} か所置いた");
    }

    // 拠点の地図の上の位置
    // 絵の y は下に向かって増え、Unity の y は上に向かって増えるので、y にマイナスをつける
    public Vector3 NodePos(string id)
    {
        float[] p = Map.Nodes[id].Pos;
        return new Vector3(p[0] * scale, -p[1] * scale, 0);
    }

    // 地図の上の点 pos から radius 以内で、いちばん近い拠点（なければ null）
    public string NodeAt(Vector3 pos, float radius)
    {
        string best = null;
        float bestDist = radius;
        foreach (string id in Map.Nodes.Keys)
        {
            float d = Vector3.Distance(pos, NodePos(id));
            if (d <= bestDist)
            {
                best = id;
                bestDist = d;
            }
        }
        return best;
    }

    // 拠点 id の持ち主を nation に変えて、丸の色も塗り直す
    public void SetOwner(string id, string nation)
    {
        Map.Nodes[id].Owner = nation;
        nodeCircles[id].color = NationColor(nation);
    }

    public Color NationColor(string nation)
    {
        int[] c = Map.Nations[nation].Color;
        return new Color32((byte)c[0], (byte)c[1], (byte)c[2], 255);
    }

    // 線を1本作って、この物の子として置く
    void MakeLine(string objectName, Vector3 from, Vector3 to)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform);

        var line = go.AddComponent<LineRenderer>();
        line.material = lineMaterial;
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.startWidth = line.endWidth = roadWidth;
        line.startColor = line.endColor = roadColor;
        line.sortingOrder = 1;
    }

    // 丸を1つ作って、この物の子として置く。order が大きいほど手前に描かれる
    SpriteRenderer MakeCircle(string objectName, Vector3 pos, float size, Color color, int order)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size, size, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circle;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }
}
