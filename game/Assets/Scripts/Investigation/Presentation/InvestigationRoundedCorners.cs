using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class InvestigationRoundedCorners : MonoBehaviour
    {
        private static readonly Dictionary<int, Sprite> Sprites = new Dictionary<int, Sprite>();
        [SerializeField, Min(1f)] private float radius = InvestigationTheme.SmallRadius;

        public void Configure(float cornerRadius)
        {
            radius = Mathf.Max(1f, cornerRadius);
            Apply();
        }

        private void OnEnable() => Apply();

        private void Apply()
        {
            Image image = GetComponent<Image>();
            if (image == null) return;
            image.sprite = GetOrCreateSprite(Mathf.RoundToInt(radius));
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.fillCenter = true;
        }

        private static Sprite GetOrCreateSprite(int requestedRadius)
        {
            int pixelRadius = Mathf.Max(1, requestedRadius);
            if (Sprites.TryGetValue(pixelRadius, out Sprite sprite) && sprite != null) return sprite;

            int border = pixelRadius + 1;
            int size = border * 2 + 2;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"Investigation Rounded Rectangle {pixelRadius}px",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color[] pixels = new Color[size * size];
            float half = size * 0.5f;
            float straight = half - pixelRadius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(Mathf.Abs(x + 0.5f - half), Mathf.Abs(y + 0.5f - half));
                    Vector2 corner = point - Vector2.one * straight;
                    Vector2 outside = new Vector2(Mathf.Max(corner.x, 0f), Mathf.Max(corner.y, 0f));
                    float signedDistance = outside.magnitude + Mathf.Min(Mathf.Max(corner.x, corner.y), 0f) - pixelRadius;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - signedDistance));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Sprites[pixelRadius] = sprite;
            return sprite;
        }
    }
}
