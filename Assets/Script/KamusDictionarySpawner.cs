using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;


public class KamusDictionarySpawner : MonoBehaviour
{
    [Header("Prefab / Layout")]
    public GameObject kamusDictionaryPrefab;
    public GridLayoutGroup beardyLayout; // assign in inspector (your GridLayoutGroup)
    
    [Header("Data")]
    public List<KamusData> sourceItems = new List<KamusData>(); // fill from GameManager or inspector

    [Header("Controls (optional)")]
    public Button nextButton;
    public Button prevButton;
    public Button menu;
    public TextMeshProUGUI pageIndicator; // optional

    // internal state
    private readonly Dictionary<int, GameObject> spawnedByIndex = new Dictionary<int, GameObject>();
    private int startIndex = 0;        // the global index in sourceItems we will begin spawning from
    private int itemsPerPage = 0;      // computed capacity of the grid
    private int currentPage = 0;       // 0-based
    private int totalPages = 0;
    private string lastKata;
    private Coroutine audioCoroutine;
    private AudioSource audioSource;

    private RectTransform layoutRect;

    private void Awake()
    {
        if (beardyLayout == null)
            Debug.LogError("[KamusDictionarySpawner] beardyLayout (GridLayoutGroup) is not assigned!");

        layoutRect = beardyLayout.GetComponent<RectTransform>();

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextPage);
        }
        if (prevButton != null)
        {
            prevButton.onClick.AddListener(PrevPage);
        }
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => GameManager.instance && AssetManager.instance != null);
        // initial compute + spawn
        sourceItems = GameManager.instance.kamusBahasa;
        GameManager.instance.audioKamusKataIndonesia = "";
        GameManager.instance.audioKamusKataDaerah = "";
        lastKata = "";

        ComputeCapacity();
        UpdatePagingInfo();
        SpawnCurrentPage();

        menu.onClick.AddListener(() =>
        {
            // Assuming you have a method to open the main menu
            SceneGameManager.instance.LoadMainMenu();
        });

        audioSource = gameObject.AddComponent<AudioSource>();

    }

    private void Update()
    {
        if (GameManager.instance.audioKamusKataIndonesia != lastKata)
        {
            if(audioCoroutine != null) 
            {
                StopCoroutine(audioCoroutine);
                audioCoroutine = null;
            }

            if(audioCoroutine != null && audioSource.isPlaying) audioSource.Stop();

            lastKata = GameManager.instance.audioKamusKataIndonesia;

            audioCoroutine = 
                StartCoroutine(
                    PlayAudioSequence(
                        gameObject.GetComponent<AudioSource>(),
                        AssetManager.instance.LoadAudio(GameManager.instance.audioKamusKataIndonesia, "Indonesia"),
                        AssetManager.instance.LoadAudio(GameManager.instance.audioKamusKataDaerah, GameManager.instance.bahasa)
                    )
                );
        }
    }


    private void OnRectTransformDimensionsChange()
    {
        // Recompute capacity if the grid area changes (e.g. screen rotate / window resize)
        ComputeCapacity();
        // Adjust current page so it still makes sense and refresh
        UpdatePagingInfo();
        Refresh();
    }

    /// <summary>
    /// Call this to set the list of items to display (e.g. from GameManager)
    /// </summary>
    public void SetSourceItems(List<KamusData> items)
    {
        sourceItems = items ?? new List<KamusData>();
        startIndex = 0;
        currentPage = 0;
        ComputeCapacity();
        UpdatePagingInfo();
        Refresh();
    }

    /// <summary>
    /// Compute how many cells fit in the GridLayoutGroup area.
    /// Uses GridLayoutGroup.cellSize, spacing and padding to calculate columns and rows.
    /// </summary>
    private void ComputeCapacity()
    {
        if (beardyLayout == null || layoutRect == null)
        {
            itemsPerPage = 0;
            return;
        }

        var padding = beardyLayout.padding;
        Vector2 cell = beardyLayout.cellSize;
        Vector2 spacing = beardyLayout.spacing;

        float usableWidth = Mathf.Max(0f, layoutRect.rect.width - padding.left - padding.right);
        float usableHeight = Mathf.Max(0f, layoutRect.rect.height - padding.top - padding.bottom);

        int cols = 1;
        int rows = 1;

        if (cell.x + spacing.x > 0f)
            cols = Mathf.FloorToInt((usableWidth + spacing.x) / (cell.x + spacing.x));
        if (cell.y + spacing.y > 0f)
            rows = Mathf.FloorToInt((usableHeight + spacing.y) / (cell.y + spacing.y));

        cols = Mathf.Max(1, cols);
        rows = Mathf.Max(1, rows);

        itemsPerPage = cols * rows;
        // recompute total pages based on new capacity
        UpdatePagingInfo();
        // Debug:
        // Debug.Log($"[KamusDictionarySpawner] Capacity computed: cols={cols}, rows={rows}, itemsPerPage={itemsPerPage}");
    }

    private void UpdatePagingInfo()
    {
        if (itemsPerPage <= 0) itemsPerPage = 1;
        totalPages = Mathf.CeilToInt(sourceItems.Count / (float)itemsPerPage);
        currentPage = Mathf.Clamp(currentPage, 0, Mathf.Max(0, totalPages - 1));
        startIndex = currentPage * itemsPerPage;
        UpdatePageIndicator();
    }

    private void UpdatePageIndicator()
    {
        if (pageIndicator != null)
        {
            if (totalPages <= 1)
                pageIndicator.text = $"Page 1 / {Mathf.Max(1, totalPages)}";
            else
                pageIndicator.text = $"Page {currentPage + 1} / {Mathf.Max(1, totalPages)}";
        }
    }

    /// <summary>
    /// Remove all spawned objects and spawn the page starting at startIndex for up to itemsPerPage.
    /// </summary>
    public void SpawnCurrentPage()
    {
        ClearSpawned();
        if (kamusDictionaryPrefab == null)
        {
            Debug.LogError("[KamusDictionarySpawner] No prefab assigned!");
            return;
        }

        if (sourceItems == null || sourceItems.Count == 0)
        {
            UpdatePageIndicator();
            return;
        }

        int spawned = 0;
        int idx = startIndex;
        while (spawned < itemsPerPage && idx < sourceItems.Count)
        {
            var data = sourceItems[idx];
            var go = Instantiate(kamusDictionaryPrefab, beardyLayout.transform);
            go.name = $"Kamus_{idx}_{data.bahasaIndonesia}";
            spawnedByIndex[idx] = go;

            // Populate prefab UI � try common fields, adjust to your prefab structure:
            // Example: assume prefab has a script `KamusEntryUI` with SetData(KamusData)
            var ui = go.GetComponent<Image>();
            var kamusCellBehaviour = go.GetComponent<KamusCellBehaviour>();        // optional
            if (ui != null)
            {
                ui.sprite = AssetManager.instance.LoadSprite(data.bahasaIndonesia);
                kamusCellBehaviour.kataIndonesia = data.bahasaIndonesia;
                kamusCellBehaviour.kataDaerah = data.bahasaDaerah;
            }

            spawned++;
            idx++;
            Debug.Log($"[KamusDictionarySpawner] Spawn Object {go.name}");
        }

        UpdatePageIndicator();
    }

    private IEnumerator PlayAudioSequence(AudioSource audioSource, AudioClip indonesiaClip, AudioClip daerahClip)
    {
        if (audioSource == null)
            yield break;

        // Make sure any currently playing audio stops
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Play Indonesia clip once (if available)
        if (indonesiaClip != null)
        {
            audioSource.clip = indonesiaClip;
            audioSource.loop = false;
            audioSource.Play();
            // wait for the clip to finish; fallback: if length 0, wait a tiny amount
            float waitInd = Mathf.Max(0.05f, indonesiaClip.length);
            yield return new WaitForSeconds(waitInd);
        }

        // small pause (optional)
        yield return new WaitForSeconds(0.2f);

        // Play bahasa daerah 2 times with 2s delay between plays
        if (daerahClip != null)
        {
            for (int i = 0; i < 2; i++)
            {
                // if another sequence started and this audioSource was used for it, we might want to check again
                if (audioSource == null) yield break;

                audioSource.clip = daerahClip;
                audioSource.loop = false;
                audioSource.Play();

                float waitDaerah = Mathf.Max(0.05f, daerahClip.length);
                yield return new WaitForSeconds(waitDaerah);

                // between plays only (not after the last)
                if (i < 1)
                    yield return new WaitForSeconds(2f);
            }
        }

    }


    /// <summary>
    /// Clears all spawned items from the grid.
    /// </summary>
    public void ClearSpawned()
    {
        foreach (var kv in spawnedByIndex)
        {
            if (kv.Value != null) Destroy(kv.Value);
        }
        spawnedByIndex.Clear();
        // optionally force immediate layout rebuild:
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);
    }

    /// <summary>
    /// Called by Next button � clears, advances page, and spawns next batch.
    /// </summary>
    public void NextPage()
    {
        if (sourceItems == null || sourceItems.Count == 0) return;
        currentPage++;
        if (currentPage >= totalPages)
        {
            // already last page; clamp and do nothing (or loop back)
            currentPage = totalPages - 1;
            return;
        }
        startIndex = currentPage * itemsPerPage;
        SpawnCurrentPage();
    }

    /// <summary>
    /// Called by Prev button � clears, goes back a page, and spawns previous batch.
    /// </summary>
    public void PrevPage()
    {
        if (sourceItems == null || sourceItems.Count == 0) return;
        currentPage--;
        if (currentPage < 0) currentPage = 0;
        startIndex = currentPage * itemsPerPage;
        SpawnCurrentPage();
    }

    /// <summary>
    /// Removes all spawned items and re-spawns the current page.
    /// Useful if the source list changed or layout resized.
    /// </summary>
    public void Refresh()
    {
        ComputeCapacity();
        UpdatePagingInfo();
        SpawnCurrentPage();
    }

    private void OnDestroy()
    {
        GameManager.instance.audioKamusKataIndonesia = "";
        GameManager.instance.audioKamusKataDaerah = "";
        if (nextButton != null) nextButton.onClick.RemoveListener(NextPage);
        if (prevButton != null) prevButton.onClick.RemoveListener(PrevPage);
    }
}
