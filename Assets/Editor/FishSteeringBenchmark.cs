using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// CPU-only microbenchmark. Does not measure game FPS, rendering, or moving AI.
public static class FishSteeringBenchmark
{
    const int Frames = 1800;
    const int Trials = 11;
    static Vector2 sink;
    sealed class Group
    {
        public List<FishAgent> members;
        public FishSchool current;
        public FishSteeringBaseline baseline;
        public Action refresh;
    }

    [MenuItem("Tools/Performance/Measure Fish Steering")]
    public static void Run()
    {
        string output = Path.GetFullPath("PerformanceResults/FishSteering");
        Directory.CreateDirectory(output);
        var rows = new List<string> { "school_fish,school_size,trial,before_ms_per_simulated_frame,after_ms_per_simulated_frame,before_bytes,after_bytes" };
        var root = new GameObject("TemporaryFishBenchmark");
        var previousRandom = UnityEngine.Random.state;
        try
        {
            foreach (int size in new[] { 5, 10, 50 })
            {
                var groups = new List<Group>();
                UnityEngine.Random.InitState(12345);
                for (int start = 0; start < 250; start += size)
                {
                    var members = new List<FishAgent>();
                    for (int i = 0; i < size; i++)
                    {
                        var go = new GameObject("BenchmarkFish");
                        go.SetActive(false);
                        go.transform.SetParent(root.transform);
                        go.transform.position = new Vector3((i % 5) * 0.35f, (i / 5) * 0.35f, 0);
                        var fish = go.AddComponent<Fish>();
                        var agent = go.AddComponent<FishAgent>();
                        typeof(FishAgent).GetField("fish", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(agent, fish);
                        typeof(FishAgent).GetField("<Velocity>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(agent, UnityEngine.Random.insideUnitCircle);
                        go.SetActive(true);
                        if (!agent.IsAiActive) throw new Exception("Inactive benchmark fish");
                        members.Add(agent);
                    }
                    var schoolObject = new GameObject("BenchmarkSchool");
                    schoolObject.transform.SetParent(root.transform);
                    var current = schoolObject.AddComponent<FishSchool>();
                    current.Configure(null, members);
                    var refresh = (Action)Delegate.CreateDelegate(typeof(Action), current,
                        typeof(FishSchool).GetMethod("CalculateAllSteering", BindingFlags.Instance | BindingFlags.NonPublic));
                    groups.Add(new Group { members = members, current = current, baseline = new FishSteeringBaseline(members), refresh = refresh });
                }
                // Warm up both implementations before recording. Alternate order per trial.
                Measure(groups, false, out _);
                Measure(groups, true, out _);
                for (int trial = 0; trial < Trials; trial++)
                {
                    double before, after; long beforeBytes, afterBytes;
                    if (trial % 2 == 0)
                    {
                        before = Measure(groups, false, out beforeBytes);
                        after = Measure(groups, true, out afterBytes);
                    }
                    else
                    {
                        after = Measure(groups, true, out afterBytes);
                        before = Measure(groups, false, out beforeBytes);
                    }
                    rows.Add(string.Format(CultureInfo.InvariantCulture, "250,{0},{1},{2:R},{3:R},{4},{5}", size, trial + 1, before, after, beforeBytes, afterBytes));
                }
                foreach (Transform child in root.transform) child.gameObject.SetActive(false);
            }
            File.WriteAllLines(Path.Combine(output, "raw.csv"), rows);
            File.WriteAllText(Path.Combine(output, "environment.txt"),
                $"Unity={Application.unityVersion}\nCPU={SystemInfo.processorType}\nOS={SystemInfo.operatingSystem}\nFrames={Frames}\nTrials={Trials}\nSeed=12345\nRefresh=every 9 simulated frames (ideal 60Hz, 0.15sec)\nStatic dense positions, all 250 school fish active, editor batch CPU only\nSink={sink}\n");
            UnityEngine.Debug.Log("FISH_BENCHMARK_COMPLETE " + output);
        }
        finally
        {
            UnityEngine.Random.state = previousRandom;
            Object.DestroyImmediate(root);
        }
    }

    static double Measure(List<Group> groups, bool optimized, out long bytes)
    {
        GC.Collect();
        long startBytes = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        Vector2 sum = Vector2.zero;
        for (int frame = 0; frame < Frames; frame++)
            for (int g = 0; g < groups.Count; g++)
            {
                Group group = groups[g];
                if (optimized && (frame + g) % 9 == 0) group.refresh();
                foreach (var fish in group.members)
                    sum += optimized ? group.current.GetCachedSteering(fish) : group.baseline.CalculateSteering(fish);
            }
        long elapsed = Stopwatch.GetTimestamp() - start;
        bytes = GC.GetAllocatedBytesForCurrentThread() - startBytes;
        sink = sum;
        return elapsed * 1000.0 / Stopwatch.Frequency / Frames;
    }
}
