using UnityEditor;
using UnityEngine;

// ========================================
// Sprite 자동 Import 설정
// Assets/Art/Sprites 아래의 PNG 파일을
// 자동으로 Sprite (2D and UI) / Single / Point / Compression None 으로 설정
// ========================================
public class SpriteImportProcessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        // PNG만 처리
        if (!assetPath.EndsWith(".png"))
            return;

        // 우리 게임의 Sprite 폴더 아래만 처리
        if (!assetPath.StartsWith("Assets/Art/Sprites/"))
            return;

        TextureImporter importer =
            assetImporter as TextureImporter;

        if (importer == null)
            return;

        // ========================================
        // 기본 Sprite 설정
        // ========================================
        importer.textureType =
            TextureImporterType.Sprite;

        importer.spriteImportMode =
            SpriteImportMode.Single;

        importer.spritePixelsPerUnit = 100f;

        // 투명 PNG 처리
        importer.alphaIsTransparency = true;

        // 픽셀 느낌 유지
        importer.filterMode =
            FilterMode.Point;

        // 반복 없이 가장자리 고정
        importer.wrapMode =
            TextureWrapMode.Clamp;

        // 압축하지 않음
        importer.textureCompression =
            TextureImporterCompression.Uncompressed;

        Debug.Log(
            $"[Sprite Auto Import] {assetPath}"
        );
    }
}