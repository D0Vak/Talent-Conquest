using System.Collections.Generic;
using Newtonsoft.Json;

// terrain.json（マス目の地形）の形を、そのまま C# にしたもの
public class TerrainData
{
    public int Tile;                          // 地図の絵の何ピクセルが1マスか（16）
    [JsonProperty("world_size")]
    public int[] WorldSize;                   // 地図の絵の大きさ [幅, 高さ]
    public Dictionary<string, string> Chars;  // マスの文字 → 地形の名前（"~" → "海" など）
    public string[] Rows;                     // 1行が1つの文字列、1文字が1マス（上の行から）

    public int Columns => Rows[0].Length;     // 横のマスの数（90）
    public int RowCount => Rows.Length;       // 縦のマスの数（67）

    public static TerrainData FromJson(string text)
    {
        return JsonConvert.DeserializeObject<TerrainData>(text);
    }

    // マス (col, row) が海か。地図の外も海として扱う
    public bool IsSea(int col, int row)
    {
        if (col < 0 || row < 0 || col >= Columns || row >= RowCount) return true;
        return Rows[row][col] == '~';
    }
}
