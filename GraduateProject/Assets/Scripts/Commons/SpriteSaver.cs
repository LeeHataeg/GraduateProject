using UnityEngine;
using UnityEditor;
using System.IO;

public class SpriteSaver : MonoBehaviour
{
    [SerializeField] private Sprite sprite;

    [ContextMenu("Save Sprite As PNG")]
    void SaveSpriteAsPng()
    {
        if (sprite == null)
        {
            Debug.LogError("Sprite가 비어있음");
            return;
        }

        Rect r = sprite.rect;
        Texture2D src = sprite.texture;

        // 잘라내기
        Texture2D tex = new Texture2D((int)r.width, (int)r.height);
        tex.SetPixels(src.GetPixels(
            (int)r.x, (int)r.y,
            (int)r.width, (int)r.height));
        tex.Apply();

        // 저장 경로
        string path = $"Assets/{sprite.name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.Refresh();

        Debug.Log($"저장 완료: {path}");
    }
}
