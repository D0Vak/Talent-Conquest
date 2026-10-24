using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 武将のコマを置いて、マウスで命令する
//   左クリック：コマを選ぶ（何もない所なら選ぶのをやめる）
//   右クリック：選んでいるコマに、その拠点への出陣を命じる
// MapView と同じ物に付けて使う
public class UnitController : MonoBehaviour
{
    [SerializeField] Sprite circle;            // コマの丸
    [SerializeField] float clickRadius = 15f;  // クリックした所から、どれだけ近ければ当たりにするか
    [SerializeField] string startNode = "nw_castle";   // 主人公たちが出陣する城
    [SerializeField] string[] heroes = { "ラインハルト", "ハインリッヒ", "ヴィルヘルム" };

    MapView mapView;
    Camera cam;
    readonly List<Unit> units = new List<Unit>();
    Unit selected;

    void Start()
    {
        mapView = GetComponent<MapView>();
        cam = Camera.main;

        foreach (string hero in heroes)
        {
            var go = new GameObject();
            Unit unit = go.AddComponent<Unit>();
            unit.Setup(mapView, circle, hero, mapView.Map.Nodes[startNode].Owner, startNode);
            units.Add(unit);
        }
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector3 pos = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        pos.z = 0;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Select(UnitAt(pos));
        }
        if (mouse.rightButton.wasPressedThisFrame && selected != null)
        {
            string target = mapView.NodeAt(pos, clickRadius);
            if (target != null && selected.MoveTo(target))
            {
                Debug.Log($"{selected.GeneralName}が{mapView.Map.Nodes[target].Name}へ出陣");
            }
        }
    }

    // 地図の上の点 pos の近くにいるコマ。同じ所に何人もいたら、選んでいないほうを先に（クリックするたびに次の人へ）
    Unit UnitAt(Vector3 pos)
    {
        Unit hit = null;
        foreach (Unit u in units)
        {
            if (Vector3.Distance(pos, u.transform.position) > clickRadius) continue;
            if (hit == null || hit == selected) hit = u;
        }
        return hit;
    }

    void Select(Unit unit)
    {
        if (selected != null) selected.SetSelected(false);
        selected = unit;
        if (selected != null)
        {
            selected.SetSelected(true);
            Debug.Log($"{selected.GeneralName}を選んだ（{mapView.Map.Nodes[selected.Node].Name}）");
        }
    }
}
