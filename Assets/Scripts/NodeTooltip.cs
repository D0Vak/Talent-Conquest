using UnityEngine;
using UnityEngine.InputSystem;

// 拠点にマウスを近づけると、その拠点の名前・持ち主の国・地形を、マウスの横に出す
// MapView と同じ物に付けて使う
public class NodeTooltip : MonoBehaviour
{
    [SerializeField] float hoverRadius = 15f;   // マウスから、どれだけ近ければ出すか（ユニット＝地図の絵のピクセル）

    MapView mapView;
    Camera cam;
    string text;   // 出す文字（何も出さないときは null）

    void Start()
    {
        mapView = GetComponent<MapView>();
        cam = Camera.main;
    }

    // 毎フレーム、マウスの近くの拠点を調べて、出す文字を決める
    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // 手順1：マウスの画面の位置 → 地図の上の位置
        Vector3 pos = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        pos.z = 0;

        // 手順2：いちばん近い拠点をさがす
        //   使うもの：mapView.Map.Nodes（拠点の呼び名 → 拠点）、mapView.NodePos(呼び名)（拠点の位置）、
        //             Vector3.Distance(a, b)（2点の距離）
        //   いちばん近い拠点の呼び名を best に、その距離を bestDist に入れる
        string best = null;
        float bestDist = float.MaxValue;
        // TODO：ここに書く


        // 手順3：いちばん近い拠点でも hoverRadius より遠ければ、何も出さない（text を null にして return）
        // TODO：ここに書く


        // 手順4：出す文字を作る。例：「オーレハイム城（オーレハイム王国・平地）」
        //   使うもの：mapView.Map.Nodes[best] の Name・Owner・Terrain
        //   Owner は国の呼び名なので、表示名は mapView.Map.Nations[呼び名].Name で引く
        // TODO：ここに書く（text = ...）
        text = best;   // 仮：今は呼び名をそのまま出す。書きかえる
    }

    // 手順5：文字を画面に出す（OnGUI は、画面を描くたびに Unity が呼ぶ）
    void OnGUI()
    {
        if (text == null || Mouse.current == null) return;

        // Input System の y は下から、OnGUI の y は上から数えるので、ひっくり返す
        Vector2 m = Mouse.current.position.ReadValue();
        var style = new GUIStyle(GUI.skin.box) { fontSize = 18 };
        Vector2 size = style.CalcSize(new GUIContent(text));
        GUI.Box(new Rect(m.x + 16, Screen.height - m.y + 16, size.x, size.y), text, style);
    }
}
