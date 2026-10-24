using UnityEngine;

// map.json を読んで、拠点を国の色の丸で地図に置く（城は大きく、町は小さく）
// シーンの空の物（MapView）に付けて使う
public class MapView : MonoBehaviour
{
    [SerializeField] TextAsset mapJson;                          // Assets/Data/map.json
    [SerializeField] Sprite circle;                              // 拠点の印にする丸
    [SerializeField] float castleSize = 14f;                     // 城の丸の大きさ（ユニット＝地図の絵のピクセル）
    [SerializeField] float townSize = 8f;                        // 町の丸の大きさ
    [SerializeField] float outline = 3f;                         // 丸のまわりの黒いふちの太さ
    [SerializeField] Vector2 mapSize = new Vector2(1448, 1086);  // 地図の絵の大きさ

    // ゲームが始まって、最初の Update の前に1回だけ呼ばれる
    void Start()
    {
        MapData map = MapData.FromJson(mapJson.text);
        float scale = mapSize.x / map.PosScale[0];   // 960×720 の点 → 1448×1086 の点（約1.51倍）

        foreach (var pair in map.Nodes)
        {
            NodeData node = pair.Value;
            // 絵の y は下に向かって増え、Unity の y は上に向かって増えるので、y にマイナスをつける
            Vector3 pos = new Vector3(node.Pos[0] * scale, -node.Pos[1] * scale, 0);
            float size = node.Castle ? castleSize : townSize;

            MakeCircle(pair.Key + "_outline", pos, size + outline, Color.black, 1);
            MakeCircle(pair.Key, pos, size, NationColor(map, node.Owner), 2);
        }
        Debug.Log($"拠点を {map.Nodes.Count} か所置いた");
    }

    // 丸を1つ作って、この物の子として置く。order が大きいほど手前に描かれる
    void MakeCircle(string objectName, Vector3 pos, float size, Color color, int order)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size, size, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circle;
        sr.color = color;
        sr.sortingOrder = order;
    }

    static Color NationColor(MapData map, string owner)
    {
        int[] c = map.Nations[owner].Color;
        return new Color32((byte)c[0], (byte)c[1], (byte)c[2], 255);
    }
}
