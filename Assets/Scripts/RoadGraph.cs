using System;
using System.Collections.Generic;
using UnityEngine;

// 道のつながり（どの拠点がどの拠点と、どれだけの長さの道でつながっているか）
// FindPath で、いちばん短い道順をさがす（ダイクストラ法）
public class RoadGraph
{
    // 拠点 → (となりの拠点, そこまでの道の長さ) の一覧
    readonly Dictionary<string, List<(string next, float length)>> neighbors =
        new Dictionary<string, List<(string, float)>>();

    // nodePos は「拠点の呼び名 → 地図の上の位置」を返す関数（道の長さを測るのに使う）
    public RoadGraph(MapData map, Func<string, Vector3> nodePos)
    {
        foreach (string id in map.Nodes.Keys)
        {
            neighbors[id] = new List<(string, float)>();
        }
        foreach (string[] road in map.Roads)
        {
            float length = Vector3.Distance(nodePos(road[0]), nodePos(road[1]));
            neighbors[road[0]].Add((road[1], length));   // 道はどちら向きにも通れる
            neighbors[road[1]].Add((road[0], length));
        }
    }

    // from から to まで、道の長さの合計がいちばん短い道順（from は入れず、to までの拠点の並び）
    // たどり着けなければ空の一覧
    public List<string> FindPath(string from, string to)
    {
        var dist = new Dictionary<string, float> { [from] = 0 };   // from からの、今わかっている一番短い長さ
        var prev = new Dictionary<string, string>();               // その一番短い道で、1つ手前の拠点
        var done = new HashSet<string>();                          // 一番短い長さが決まった拠点

        while (true)
        {
            // まだ決まっていない拠点の中で、いちばん近いものを選ぶ
            string current = null;
            foreach (var pair in dist)
            {
                if (!done.Contains(pair.Key) && (current == null || pair.Value < dist[current]))
                {
                    current = pair.Key;
                }
            }
            if (current == null) return new List<string>();   // もう選べる拠点がない＝たどり着けない
            if (current == to) break;
            done.Add(current);

            // そこから1本の道で行ける拠点について、今より短く行けるなら書きかえる
            foreach (var (next, length) in neighbors[current])
            {
                float d = dist[current] + length;
                if (!dist.ContainsKey(next) || d < dist[next])
                {
                    dist[next] = d;
                    prev[next] = current;
                }
            }
        }

        // to から prev をさかのぼって、道順を作る
        var path = new List<string>();
        for (string n = to; n != from; n = prev[n])
        {
            path.Add(n);
        }
        path.Reverse();
        return path;
    }
}
