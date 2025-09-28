using System.Threading.Tasks;
using PolyAndCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MyProject
{
    public class ImageLoader : MonoBehaviour, ICell
    {
        [SerializeField] private RawImage _rawImage;
        
        [SerializeField] private TextMeshProUGUI _text;

        private const int maxWidth = 512;
        private const int maxHeight = 512;


        public async Task LoadImageIfNeeded(ImageItem imageData)
        {
            if (imageData == null || string.IsNullOrEmpty(imageData.url))
                return;

            _text.SetText(imageData.number.ToString());

            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageData.url, nonReadable: true);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Image download failed: {request.error}");
                return;
            }

            Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);

            // Scale if needed
            int targetWidth = downloadedTexture.width;
            int targetHeight = downloadedTexture.height;

            if (targetWidth > maxWidth || targetHeight > maxHeight)
            {
                float scale = Mathf.Min((float)maxWidth / targetWidth, (float)maxHeight / targetHeight);
                targetWidth = Mathf.RoundToInt(targetWidth * scale);
                targetHeight = Mathf.RoundToInt(targetHeight * scale);
            }

            // Use GPU to rescale into a *new permanent RenderTexture*
            RenderTexture rt = new(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Bilinear
            };

            Graphics.Blit(downloadedTexture, rt);

            _rawImage.texture = rt;

            // Free CPU memory immediately
            Destroy(downloadedTexture);
        }
    }
}