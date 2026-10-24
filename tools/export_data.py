# 道具：Python 版（Talent-Conquest-Game）のデータを JSON に書き出して、この Unity 版の Assets/Data/ に入れる
#   python tools/export_data.py
#   2つのリポジトリが同じフォルダに並んでいる前提（…/Talent-Conquest-Game と …/Talent-Conquest）
#   Python 版の map_editor.py・terrain_editor.py で地図や地形を直したら、もう一度これを実行する
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent          # この Unity 版のフォルダ
SRC = ROOT.parent / "Talent-Conquest-Game"             # 隣の Python 版
OUT = ROOT / "Assets" / "Data"                         # Unity は Assets の中しか素材として読まない

if not SRC.exists():
    raise SystemExit(f"Python 版のフォルダが見つかりません: {SRC}")
sys.path.insert(0, str(SRC))                           # Python 版のファイルを import できるようにする

import map_data    # noqa: E402
import grid_data   # noqa: E402
import names       # noqa: E402


def write(filename, data):
    """data を OUT/filename に JSON で書き出す（日本語はそのまま読める形で。改行は LF）"""
    path = OUT / filename
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write("\n")
    print("書き出した:", path)


def main():
    OUT.mkdir(exist_ok=True)

    # 国・拠点・道。拠点の位置は map.png を 960×720 に縮めたときの点（そろえるのは段階2で）
    write("map.json", {
        "pos_scale": [960, 720],
        "nations": map_data.NATIONS,
        "nodes": map_data.NODES,
        "roads": map_data.ROADS,
    })

    # マス目の地形。rows の1文字が1マス、chars がその文字の地形
    write("terrain.json", {
        "tile": grid_data.TILE,
        "world_size": [grid_data.WORLD_W, grid_data.WORLD_H],
        "chars": grid_data.CHARS,
        "rows": grid_data.TERRAIN_ROWS,
    })

    # 名前の一覧と、一覧を使い切ったときの音の部品（はじめ・なか・おわり）
    write("names.json", {
        "lists": names.NAME_LISTS,
        "parts": {nation: {"head": head, "mid": mid, "tail": tail}
                  for nation, (head, mid, tail) in names.NAME_PARTS.items()},
    })


if __name__ == "__main__":
    main()
