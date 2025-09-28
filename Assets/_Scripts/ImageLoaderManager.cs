using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PolyAndCode.UI;

namespace MyProject
{
    public class ImageLoaderManager : MonoBehaviour, IRecyclableScrollRectDataSource
    {
        [Header("Scroll Rect")]
        [SerializeField] private RecyclableScrollRect _recyclableScrollRect;

        [Header("Pagination Settings")]
        [SerializeField] private int _itemsPerPage = 5;      // Images per API request
        [SerializeField] private int _fetchThreshold = 2;    // Fetch next page when this many items from bottom

        [Header("UI Elements")]
        [SerializeField] private GameObject _loadingScreen;  // Assign your loading panel

        private readonly string url = "https://10.62.159.216:5227/api/images/?";

        [SerializeField] private List<ImageItem> _imageData = new();
        [SerializeField] private int _currentPage = 1;

        private bool _isLoading = false;
        private bool _hasMoreData = true;
        private bool _nextPageTriggered = false; // Prevent multiple triggers

        private void Awake() => Application.targetFrameRate = 60;

        private void Start()
        {
            _recyclableScrollRect.Initialize(this);
            _recyclableScrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            StartCoroutine(FetchImages(_currentPage, _itemsPerPage));
        }


        private IEnumerator FetchImages(int page, int limit)
        {
            if (_isLoading || !_hasMoreData) yield break;
            _isLoading = true;

            // Show loading screen for first page
            if (_loadingScreen != null)
                _loadingScreen.SetActive(true);

            float minLoadingTime = 2.5f;
            float startTime = Time.time;

            UnityWebRequest www = UnityWebRequest.Get($"{url}page={page}&limit={limit}");
            www.certificateHandler = new BypassSSL();
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to fetch images: {www.error}");
            }
            else
            {
                string json = www.downloadHandler.text;

                ImageResponse imageList = JsonUtility.FromJson<ImageResponse>(json);

                if (imageList.images.Count == 0 || _currentPage > imageList.totalPages)
                {
                    // No more data to fetch
                    _hasMoreData = false;
                }
                else
                {
                    // Add new images
                    foreach (var img in imageList.images)
                        _imageData.Add(new ImageItem { url = img.url, number = img.number });

                    _currentPage++;
                    //_recyclableScrollRect.ReloadData();

                    // If we've reached the last page, stop further fetching
                    if (_currentPage > imageList.totalPages)
                        _hasMoreData = false;
                }
            }

            float elapsed = Time.time - startTime;
            if (elapsed < minLoadingTime)
                yield return new WaitForSeconds(minLoadingTime - elapsed);

            _isLoading = false;
            _nextPageTriggered = false;

            if (_loadingScreen != null)
                _loadingScreen.SetActive(false);
        }


        private void OnScrollValueChanged(Vector2 normalizedPosition)
        {
            if (_hasMoreData && !_isLoading && !_nextPageTriggered)
            {
                // Check if we need to fetch the next page
                float verticalPos = normalizedPosition.y;
                if (verticalPos <= 0.1f) // Near bottom
                {
                    _nextPageTriggered = true;
                    StartCoroutine(FetchImages(_currentPage, _itemsPerPage));
                }
            }
        }

        public int GetItemCount() => _imageData.Count;


        public void SetCell(ICell cell, int index)
        {
            var item = (ImageLoader)cell;
            _ = item.LoadImageIfNeeded(_imageData[index]);
        }
    }

    public class BypassSSL : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // Accept all certificates
            return true;
        }
    }

    [Serializable]
    public class ImageResponse
    {
        public int totalItems;
        public int totalPages;
        public List<ImageItem> images;
    }

    [Serializable]
    public class ImageItem
    {
        public int number;
        public string url;
    }
}