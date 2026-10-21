extends Node
## ゲームのデータ（国・拠点・道、マス目の地形、名前）。
## オートロードに登録してあるので、ゲームが始まると最初に1回だけ読み込まれ、
## どのスクリプトからも GameData.nodes のように使える。
## data/ の JSON は Python 版の export_data.py で書き出したもの。

var nations: Dictionary = {}        # 国の呼び名 → {name, color}
var nodes: Dictionary = {}          # 拠点の呼び名 → {name, pos, terrain, castle, owner}
var roads: Array = []               # 道の一覧。1本が [拠点A, 拠点B]
var node_pos_scale: Array = []      # 拠点の位置の物差し（map.png を 960×720 に縮めたとき）

var tile: int = 0                   # 元の絵の何ピクセルが1マスか
var world_size: Array = []          # 元の絵の大きさ
var terrain_chars: Dictionary = {}  # マスの文字 → 地形の名前（"." → "平地" など）
var terrain_rows: Array = []        # マス目の地形。1行が1つの文字列、1文字が1マス

var name_lists: Dictionary = {}     # 国の呼び名 → 名前の一覧
var name_parts: Dictionary = {}     # 国の呼び名 → {head, mid, tail}（一覧を使い切ったときの音）


func _ready() -> void:
	var map := _load_json("res://data/map.json")
	nations = map["nations"]
	nodes = map["nodes"]
	roads = map["roads"]
	node_pos_scale = map["pos_scale"]

	var terrain := _load_json("res://data/terrain.json")
	tile = int(terrain["tile"])
	world_size = terrain["world_size"]
	terrain_chars = terrain["chars"]
	terrain_rows = terrain["rows"]

	var names := _load_json("res://data/names.json")
	name_lists = names["lists"]
	name_parts = names["parts"]

	print("GameData: 国 %d・拠点 %d・道 %d・マス %d×%d・名前 %d" % [
		nations.size(), nodes.size(), roads.size(),
		terrain_rows[0].length(), terrain_rows.size(), _count_names()])


## path の JSON を読んで、中身を Dictionary で返す（読めなければ空の Dictionary）
func _load_json(path: String) -> Dictionary:
	var text := FileAccess.get_file_as_string(path)
	var data = JSON.parse_string(text)
	if data == null:
		push_error("JSON を読み込めません: " + path)
		return {}
	return data


func _count_names() -> int:
	var total := 0
	for list in name_lists.values():
		total += list.size()
	return total
