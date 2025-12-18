using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

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

    [SerializeField] private TMP_Text remainingEnemyText;
    [SerializeField] private EnemySpawnData[] enemyTypes;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float startDelay = 3.0f;
    [SerializeField] private float clearDelay = 3.0f;

    [Header("Next Stage Setting")]
    [SerializeField] private string nextSceneName;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool isSpawning = false;
    private bool stageCleared = false;

    private void Start()
    {
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
            UpdateRemainingUI();
        }
    }

    private void Update()
    {
        if (!isSpawning || stageCleared) return;

        int previousCount = activeEnemies.Count;
        activeEnemies.RemoveAll(item => item == null);

        if (previousCount != activeEnemies.Count)
        {
            UpdateRemainingUI();
        }

        if (IsAllEnemyDead())
        {
            stageCleared = true;
            UpdateRemainingUI();
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

        int remainingToSpawn = totalToSpawn - alreadySpawned;
        int totalRemaining = remainingToSpawn + activeEnemies.Count;

        remainingEnemyText.text = totalRemaining.ToString();
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

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            SceneManager.LoadScene(nextIndex);
        }
    }
}