using UnityEngine;

public class NetTextureGenerator : MonoBehaviour
{
    void Start()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        Color netColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        int lineWidth = 6; // thickness of net lines in pixels

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                bool onLine = (x % 32 < lineWidth) || (y % 32 < lineWidth);
                tex.SetPixel(x, y, onLine ? netColor : transparent);
            }
        }

        tex.Apply();

        // Save to Assets folder
        byte[] bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(
            Application.dataPath + "/Materials/NetTexture.png", bytes);
        
        Debug.Log("Net texture saved to Assets/Materials/NetTexture.png");
        Destroy(gameObject); // clean up after generating
    }
}