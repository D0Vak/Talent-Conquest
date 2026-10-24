using System.Collections.Generic;
using UnityEngine;

// 武将のコマ1つ。道順にそって歩き、着いた拠点を自分の国のものにする
// UnitController が作る
public class Unit : MonoBehaviour
{
    public string GeneralName;   // 武将の名前
    public string Nation;        // 国の呼び名
    public string Node;          // 今いる拠点（歩いている間は、最後に通った拠点）
    public float Speed = 60f;    // 1秒に進む長さ（ユニット＝地図の絵のピクセル）

    MapView mapView;
    SpriteRenderer selectedRing;                      // 選ばれているときに出る黄色い輪
    readonly Queue<string> path = new Queue<string>();   // これから通る拠点の並び（先頭が次の拠点）

    public bool IsMoving => path.Count > 0;

    // コマの見た目を作る：黄色い輪（選ばれたとき）→ 白いふち → 国の色の丸
    public void Setup(MapView view, Sprite circle, string generalName, string nation, string node)
    {
        mapView = view;
        GeneralName = generalName;
        Nation = nation;
        Node = node;
        name = generalName;
        transform.position = mapView.NodePos(node);

        selectedRing = MakePart(circle, 26f, Color.yellow, 5);
        selectedRing.enabled = false;
        MakePart(circle, 20f, Color.white, 6);
        MakePart(circle, 15f, mapView.NationColor(nation), 7);
    }

    public void SetSelected(bool selected)
    {
        selectedRing.enabled = selected;
    }

    // 拠点 target へ向かわせる。道順が見つかれば true
    public bool MoveTo(string target)
    {
        List<string> route = mapView.Roads.FindPath(Node, target);
        if (route.Count == 0) return false;

        path.Clear();
        foreach (string n in route)
        {
            path.Enqueue(n);
        }
        return true;
    }

    // 毎フレーム：次の拠点へ少しずつ進む。着いたら、その拠点を取る
    void Update()
    {
        if (path.Count == 0) return;

        string next = path.Peek();
        Vector3 goal = mapView.NodePos(next);
        transform.position = Vector3.MoveTowards(transform.position, goal, Speed * Time.deltaTime);

        if (transform.position == goal)
        {
            path.Dequeue();
            Node = next;
            Arrive(next);
        }
    }

    void Arrive(string node)
    {
        NodeData data = mapView.Map.Nodes[node];
        if (data.Owner != Nation)
        {
            string oldOwner = mapView.Map.Nations[data.Owner].Name;
            mapView.SetOwner(node, Nation);
            Debug.Log($"{GeneralName}が{data.Name}を{oldOwner}から奪った");
        }
    }

    SpriteRenderer MakePart(Sprite circle, float size, Color color, int order)
    {
        var go = new GameObject("part");
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(size, size, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circle;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }
}
