using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; // TMP 사용을 위해 추가

public class EnemySpawnManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnData
    {
        public GameObject enemyPrefab;
        public int maxSpawnCount = 10;
        public float spawnInterval = 2.0f;
        [HideInInspector] public int currentSpawnedCount = 0;
    }

    [Header("UI 설정")]
    [SerializeField] private TMP_Text remainingEnemyText; // 남은 적 수 표시 TMP

    [Header("스폰 설정")]
    [SerializeField] private EnemySpawnData[] enemyTypes;
    [SerializeField] private Transform[] spawnPoints;

    [Header("시간 설정")]
    [SerializeField] private float startDelay = 3.0f;
    [SerializeField] private float clearDelay = 3.0f;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool isSpawning = false;
    private bool stageCleared = false;

    private void Start()
    {
        // 초기 UI 텍스트 설정
        UpdateRemainingUI();
        StartCoroutine(StartSpawnRoutine());
    }

    private IEnumerator StartSpawnRoutine()
    {
        yield return new WaitForSeconds(startDelay);
        isSpawning = true;

        foreach (var data in enemyTypes)
        {
            StartCoroutine(SpawnEnemy(data));
        }
    }

    private IEnumerator SpawnEnemy(EnemySpawnData data)
    {
        while (data.currentSpawnedCount < data.maxSpawnCount)
        {
            yield return new WaitForSeconds(data.spawnInterval);

            Transform selectedPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            GameObject enemy = Instantiate(data.enemyPrefab, selectedPoint.position, Quaternion.identity);
            activeEnemies.Add(enemy);
            data.currentSpawnedCount++;

            // 적이 생성될 때마다 UI 갱신 (선택사항, Update에서 처리됨)
            UpdateRemainingUI();
        }
    }

    private void Update()
    {
        if (!isSpawning || stageCleared) return;

        // 리스트에서 파괴된(죽은) 적 제거
        int previousCount = activeEnemies.Count;
        activeEnemies.RemoveAll(item => item == null);

        // 적이 죽어서 수가 변했다면 UI 업데이트
        if (previousCount != activeEnemies.Count)
        {
            UpdateRemainingUI();
        }

        if (IsAllEnemyDead())
        {
            stageCleared = true;
            UpdateRemainingUI(); // 0 표시
            StartCoroutine(GoToNextStage());
        }
    }

    private void UpdateRemainingUI()
    {
        if (remainingEnemyText == null) return;

        int totalToSpawn = 0;
        int alreadySpawned = 0;

        foreach (var data in enemyTypes)
        {
            totalToSpawn += data.maxSpawnCount;
            alreadySpawned += data.currentSpawnedCount;
        }

        // 남은 스폰 예정 수 + 현재 필드에 살아있는 수
        int remainingToSpawn = totalToSpawn - alreadySpawned;
        int currentAlive = activeEnemies.Count;
        int totalRemaining = remainingToSpawn + currentAlive;

        remainingEnemyText.text = $"{totalRemaining}";

        // 연출: 적이 줄어들 때마다 살짝 커졌다 작아지는 효과 (선택 사항)
        // remainingEnemyText.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
    }

    private bool IsAllEnemyDead()
    {
        foreach (var data in enemyTypes)
        {
            if (data.currentSpawnedCount < data.maxSpawnCount) return false;
        }
        return activeEnemies.Count == 0;
    }

    private IEnumerator GoToNextStage()
    {
        yield return new WaitForSeconds(clearDelay);
        Debug.Log("모든 적 처치! 다음 스테이지로 이동합니다.");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Stage2Scene");
    }
}