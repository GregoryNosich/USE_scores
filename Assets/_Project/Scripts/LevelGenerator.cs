using UnityEngine;
using UnityEngine.Rendering;

public class LevelGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform chunksParent;

    [Header("Chunk Settings")]
    [SerializeField] private GameObject[] chunkPrefabs;
    [SerializeField] private float startY = 0f;
    [SerializeField] private float chunkHeight = 10f;
    [SerializeField] private int chunksCount = 20;

    [Header("Generation Settings")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool avoidRepeatingSameChunk = true;

    [Header("Sorting Settings")]
    [SerializeField] private int chunkBaseSortingOrder = 10;
    [SerializeField] private int chunkSortingOrderStep = 1;
    [SerializeField] private int chunkVisualSortingOrder = 0;
    [SerializeField] private int zoneVisualSortingOrder = 10;

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
                startY + i * chunkHeight,
                0f
            );

            GameObject chunk = Instantiate(
                chunkPrefabs[chunkIndex],
                spawnPosition,
                Quaternion.identity,
                chunksParent
            );

            chunk.name = $"{chunkPrefabs[chunkIndex].name}_{i}";
            ApplyChunkSortingOrder(chunk, i);
            ApplyChunkContentSorting(chunk);
        }
    }

    private void ApplyChunkSortingOrder(GameObject chunk, int chunkIndex)
    {
        SortingGroup sortingGroup = chunk.GetComponent<SortingGroup>();

        if (sortingGroup == null)
        {
            sortingGroup = chunk.AddComponent<SortingGroup>();
        }

        sortingGroup.sortingOrder = chunkBaseSortingOrder - chunkIndex * chunkSortingOrderStep;
    }

    private void ApplyChunkContentSorting(GameObject chunk)
    {
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        int boostLayer = LayerMask.NameToLayer("Boost");
        SpriteRenderer[] renderers = chunk.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = renderers[i];

            if (spriteRenderer == null)
            {
                continue;
            }

            Transform zoneRoot = FindZoneRoot(
                spriteRenderer.transform,
                chunk.transform,
                obstacleLayer,
                boostLayer
            );

            if (zoneRoot == null)
            {
                spriteRenderer.sortingOrder = chunkVisualSortingOrder;
                continue;
            }

            bool isZoneRootRectangle = spriteRenderer.transform == zoneRoot &&
                HasChildSpriteRenderer(zoneRoot);

            spriteRenderer.sortingOrder = isZoneRootRectangle
                ? chunkVisualSortingOrder
                : zoneVisualSortingOrder;
        }
    }

    private Transform FindZoneRoot(
        Transform start,
        Transform chunkRoot,
        int obstacleLayer,
        int boostLayer
    )
    {
        Transform current = start;
        Transform zoneRoot = null;

        while (current != null && current != chunkRoot)
        {
            if (current.gameObject.layer == obstacleLayer ||
                current.gameObject.layer == boostLayer)
            {
                zoneRoot = current;
            }

            current = current.parent;
        }

        return zoneRoot;
    }

    private bool HasChildSpriteRenderer(Transform root)
    {
        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].transform != root)
            {
                return true;
            }
        }

        return false;
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
