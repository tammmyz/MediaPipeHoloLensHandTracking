using System;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace TextureProcUtils
{
    public class TexProcUtils
    {
        // Resize and pad image to target input size
        public static Texture2D resizePad(Texture2D texture2D, int targetSize)
        {
            // Resize input texture
            Texture2D resizedTex;
            if (texture2D.width > texture2D.height)
            {
                int newH = targetSize * texture2D.height / texture2D.width;
                resizedTex = TexProcUtils.resize(texture2D, targetSize, newH);
            }
            else
            {
                int newW = targetSize * texture2D.width / texture2D.height;
                resizedTex = TexProcUtils.resize(texture2D, newW, targetSize);
            }
            Texture2D paddedTex = TexProcUtils.pad(resizedTex);

            return paddedTex;
        }

        // Resize image
        public static Texture2D resize(Texture2D texture2D, int targetX, int targetY)
        {
            RenderTexture rt = new RenderTexture(targetX, targetY, 24, RenderTextureFormat.ARGB32);
            RenderTexture.active = rt;
            Graphics.Blit(texture2D, rt);
            Texture2D result = new Texture2D(targetX, targetY, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
            result.Apply();
            return result;
        }

        // Pad image to make square
        public static Texture2D pad(Texture2D texture2D)
        {
            int maxSize = Math.Max(texture2D.width, texture2D.height);
            int offsetX = (maxSize - texture2D.width) / 2;
            int offsetY = (maxSize - texture2D.height) / 2;
            Texture2D paddedTex = new Texture2D(maxSize, maxSize, TextureFormat.RGB24, false);
            Color[] texPixels = texture2D.GetPixels();
            paddedTex.SetPixels(offsetX, offsetY, texture2D.width, texture2D.height, texPixels);
            paddedTex.Apply();
            RenderTexture.active = null;
            return paddedTex;
        }
    }
}