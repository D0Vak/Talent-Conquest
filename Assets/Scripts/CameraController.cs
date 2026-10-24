using UnityEngine;
using UnityEngine.InputSystem;

// カメラで地図を見る：ホイールでズーム、中ボタンのドラッグか WASD・矢印で地図を動かす
// Main Camera に付けて使う
public class CameraController : MonoBehaviour
{
    [SerializeField] float minSize = 60f;      // いちばん寄ったときの Size（画面の高さの半分が何ユニットか）
    [SerializeField] float maxSize = 543f;     // いちばん引いたときの Size（地図の高さの半分）
    [SerializeField] float zoomStep = 1.15f;   // ホイール1目盛りで何倍にするか
    [SerializeField] float keySpeed = 1.0f;    // WASD・矢印で1秒に動く量（今見えている高さの何倍か）
    [SerializeField] Vector2 mapSize = new Vector2(1448, 1086);  // 地図の絵の大きさ

    Camera cam;
    Vector3 dragStartWorld;   // 中ボタンを押したときに、マウスの下にあった地図の点

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    // 毎フレーム（1秒に何十回も）呼ばれる
    void Update()
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Zoom(mouse);
            Drag(mouse);
        }
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            MoveByKeys(keyboard);
        }
        KeepInsideMap();
    }

    // ホイール：Size を変えてズームする。マウスの下の点は動かないようにする
    void Zoom(Mouse mouse)
    {
        float scroll = mouse.scroll.ReadValue().y;
        if (scroll == 0) return;

        Vector3 before = MouseWorld(mouse);
        float factor = scroll > 0 ? 1f / zoomStep : zoomStep;   // 奥へ回すと寄る
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * factor, minSize, maxSize);
        Vector3 after = MouseWorld(mouse);
        transform.position += before - after;   // ズームでずれた分だけカメラを戻す
    }

    // 中ボタンのドラッグ：つかんだ地図の点が、ずっとマウスの下にあるようにカメラを動かす
    void Drag(Mouse mouse)
    {
        if (mouse.middleButton.wasPressedThisFrame)
        {
            dragStartWorld = MouseWorld(mouse);
        }
        if (mouse.middleButton.isPressed)
        {
            transform.position += dragStartWorld - MouseWorld(mouse);
        }
    }

    // WASD・矢印：押している方向へ動かす。寄っているときほどゆっくり
    void MoveByKeys(Keyboard keyboard)
    {
        Vector3 dir = Vector3.zero;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) dir.x -= 1;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) dir.x += 1;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) dir.y -= 1;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) dir.y += 1;
        transform.position += dir * (cam.orthographicSize * 2 * keySpeed * Time.deltaTime);
    }

    // カメラの中心が地図の外に出ないようにする（地図は左上が (0, 0)、右下が (1448, -1086)）
    void KeepInsideMap()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, 0, mapSize.x);
        p.y = Mathf.Clamp(p.y, -mapSize.y, 0);
        transform.position = p;
    }

    // マウスの画面の位置を、地図の上の位置（ユニット）に直す
    Vector3 MouseWorld(Mouse mouse)
    {
        Vector3 p = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        p.z = 0;
        return p;
    }
}
