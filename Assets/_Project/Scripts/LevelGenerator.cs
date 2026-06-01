using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform chunksParent;

    [Header("Chunk Settings")]
    [SerializeField] private GameObject[] chunkPrefabs;
    [SerializeField] private float chunkHeight = 10f;
    [SerializeField] private int chunksCount = 20;

    [Header("Generation Settings")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool avoidRepeatingSameChunk = true;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateLevel();
        }
    }

    public void GenerateLevel()
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0)
        {
            Debug.LogWarning("No chunk prefabs assigned to LevelGenerator.");
            return;
        }

        ClearOldChunks();

        int previousChunkIndex = -1;

        for (int i = 0; i < chunksCount; i++)
        {
            int chunkIndex = GetRandomChunkIndex(previousChunkIndex);
            previousChunkIndex = chunkIndex;

            Vector3 spawnPosition = new Vector3(
                0f,
                i * chunkHeight,
                0f
            );

            GameObject chunk = Instantiate(
                chunkPrefabs[chunkIndex],
                spawnPosition,
                Quaternion.identity,
                chunksParent
            );

            chunk.name = $"{chunkPrefabs[chunkIndex].name}_{i}";
        }
    }

    private int GetRandomChunkIndex(int previousChunkIndex)
    {
        if (!avoidRepeatingSameChunk || chunkPrefabs.Length <= 1)
        {
            return Random.Range(0, chunkPrefabs.Length);
        }

        int index;

        do
        {
            index = Random.Range(0, chunkPrefabs.Length);
        }
        while (index == previousChunkIndex);

        return index;
    }

    private void ClearOldChunks()
    {
        if (chunksParent == null)
        {
            return;
        }

        for (int i = chunksParent.childCount - 1; i >= 0; i--)
        {
            Destroy(chunksParent.GetChild(i).gameObject);
        }
    }
}