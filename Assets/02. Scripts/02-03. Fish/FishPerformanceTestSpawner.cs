using System.Collections.Generic;
using UnityEngine;

/// <summary>성능 비교를 위해 지정한 수의 물고기를 동일한 조건으로 생성합니다.</summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class FishPerformanceTestSpawner : MonoBehaviour
{
    [Header("테스트 조건")]
    [SerializeField, Min(0)] private int fishCount = 200;
    [SerializeField] private int randomSeed = 12345;
    [Tooltip("씬에 원래 배치된 물고기를 비활성화하고 생성 수를 정확히 맞춥니다.")]
    [SerializeField] private bool disableExistingFish = true;
    [Tooltip("전체 물고기의 절반을 떼로 구성할 때 한 떼에 포함할 마릿수입니다.")]
    [SerializeField, Min(2)] private int fishPerSchool = 10;

    [Header("생성할 물고기")]
    [SerializeField] private GameObject[] fishPrefabs;

    private FishArea[] areas;
    private Transform spawnedFishRoot;

    private void Awake()
    {
        areas = FindObjectsByType<FishArea>(FindObjectsSortMode.None);

        if (disableExistingFish)
        {
            FishAgent[] existingFish = FindObjectsByType<FishAgent>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );
            foreach (FishAgent fish in existingFish)
                fish.gameObject.SetActive(false);

            FishSchool[] existingSchools = FindObjectsByType<FishSchool>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );
            foreach (FishSchool school in existingSchools)
                school.enabled = false;
        }
    }

    private void Start()
    {
        if (fishCount <= 0) return;
        if (fishPrefabs == null || fishPrefabs.Length == 0)
        {
            Debug.LogError("성능 테스트에 사용할 물고기 프리팹이 없습니다.", this);
            return;
        }
        if (areas == null || areas.Length == 0)
        {
            Debug.LogError("물고기를 생성할 FishArea가 없습니다.", this);
            return;
        }

        GameObject root = new("SpawnedPerformanceTestFish");
        spawnedFishRoot = root.transform;

        Random.State previousRandomState = Random.state;
        Random.InitState(randomSeed);
        int schoolFishCount = fishCount / 2;
        Dictionary<FishArea, List<FishAgent>> schoolMembersByArea = new();

        for (int i = 0; i < fishCount; i++)
        {
            GameObject prefab = fishPrefabs[Random.Range(0, fishPrefabs.Length)];
            FishArea area = areas[Random.Range(0, areas.Length)];
            if (prefab == null || area == null) continue;

            GameObject instance = Instantiate(
                prefab,
                area.RandomPoint(),
                Quaternion.identity,
                spawnedFishRoot
            );
            instance.name = $"PerformanceFish_{i + 1:0000}_{prefab.name}";

            FishAgent agent = instance.GetComponent<FishAgent>();
            if (agent == null) continue;

            if (i < schoolFishCount)
            {
                if (!schoolMembersByArea.TryGetValue(area, out List<FishAgent> members))
                {
                    members = new List<FishAgent>();
                    schoolMembersByArea.Add(area, members);
                }
                members.Add(agent);
            }
            else
            {
                agent.AssignArea(area);
            }
        }

        CreateSchools(schoolMembersByArea);

        Random.state = previousRandomState;
        Debug.Log(
            $"성능 테스트 물고기 {fishCount}마리를 생성했습니다. "
            + $"떼 물고기 {schoolFishCount}마리, 개별 물고기 {fishCount - schoolFishCount}마리",
            this
        );
    }

    private void CreateSchools(Dictionary<FishArea, List<FishAgent>> membersByArea)
    {
        int maximumSchoolSize = Mathf.Max(2, fishPerSchool);
        int schoolIndex = 1;

        foreach ((FishArea area, List<FishAgent> areaMembers) in membersByArea)
        {
            for (int startIndex = 0; startIndex < areaMembers.Count; startIndex += maximumSchoolSize)
            {
                int count = Mathf.Min(maximumSchoolSize, areaMembers.Count - startIndex);
                List<FishAgent> schoolMembers = areaMembers.GetRange(startIndex, count);
                GameObject schoolObject = new($"PerformanceFishSchool_{schoolIndex:000}");
                schoolObject.transform.SetParent(spawnedFishRoot);
                FishSchool school = schoolObject.AddComponent<FishSchool>();
                school.Configure(area, schoolMembers);
                schoolIndex++;
            }
        }
    }
}
