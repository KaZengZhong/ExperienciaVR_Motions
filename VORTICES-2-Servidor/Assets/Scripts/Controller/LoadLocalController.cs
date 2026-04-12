using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Thirdparty.Scripts;
using UnityEngine;
using UnityEngine.Networking;

namespace Vortices
{
    public enum Result
    {
        Success,
        OnGoing,
        WebRequestError,
        TotalError
    }
// Total means the manager did not accomplish anything, the others secure at least one result to work with

    public class LoadLocalController : MonoBehaviour
    {
        // This class manages loading from disk then converting into texture for shape, eventually will support multiple extensions and batch loading
        // Public data
        public List<Texture2D> textureBuffer;
        public Result result;

        public IEnumerator GetMultipleImage(List<string> imageList, bool asThumbnail)
        {
            result = Result.OnGoing;
            textureBuffer = new List<Texture2D>();

            foreach (string url in imageList.ToList())
            {
                if (asThumbnail)
                {
                    yield return StartCoroutine(GetImageThumbnail(url));
                }
                else
                {
                    yield return StartCoroutine(GetImage(url));
                }

                yield return null;
            }

            if (result == Result.OnGoing)
            {
                result = Result.Success;
            }

            if (textureBuffer.Count < 1)
            {
                result = Result.TotalError;
            }
        }

        public IEnumerator GetImage(string imageLocation)
        {
            // Web retrieving
            string newurl = imageLocation.Replace(@"\", "/");
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(@"file:///" + newurl);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                result = Result.WebRequestError;
            }
            else
            {
                Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;

                texture.Apply(false, true);

                textureBuffer.Add(texture);
            }
        }

        public IEnumerator GetImageThumbnail(string imageLocation)
        {
            // Web retrieving
            string newurl = imageLocation.Replace(@"\", "/");
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(@"file:///" + newurl);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                result = Result.WebRequestError;
            }
            else
            {
                Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;

                ConvertTextureThumbnail(texture);
                ConvertTexturePOT(texture);
                texture.Compress(true);
                texture.Apply(false, true);

                textureBuffer.Add(texture);
            }
        }


        public static string FormatFileSize(long bytes)
        {
            var unit = 1024;
            if (bytes < unit) { return $"{bytes} B"; }

            var exp = (int)(Mathf.Log(bytes) / Mathf.Log(unit));
            return $"{bytes / Mathf.Pow(unit, exp):F2} {("KMGTPE")[exp - 1]}B";
        }

        private void ConvertTextureThumbnail(Texture2D textureToConvert)
        {
            Rect thumbnailRect = new Rect(0, 0, 250, 250);

            if (textureToConvert.height > thumbnailRect.height)
            {
                int finalheight = (int)thumbnailRect.height;
                int finalwidth = (int)(textureToConvert.width * finalheight / textureToConvert.height);
                TextureScaler.Resize(textureToConvert, finalwidth, finalheight, true, FilterMode.Trilinear);
            }

            if (textureToConvert.width > thumbnailRect.width)
            {
                int finalwidth = (int)thumbnailRect.width;
                int finalheight = (int)(textureToConvert.height * finalwidth / textureToConvert.width);
                TextureScaler.Resize(textureToConvert, finalwidth, finalheight, true, FilterMode.Trilinear);
            }
        }

        private void ConvertTexturePOT(Texture2D textureToConvert)
        {
            if (textureToConvert.height % 4 != 0 || textureToConvert.width % 4 != 0)
            {
                int finalWidth = (int)Mathf.Ceil((float)textureToConvert.width / 4) * 4;
                int finalHeight = (int)Mathf.Ceil((float)textureToConvert.height / 4) * 4;

                TextureScaler.Resize(textureToConvert, finalWidth, finalHeight, true, FilterMode.Trilinear);
            }
        }

    }
}