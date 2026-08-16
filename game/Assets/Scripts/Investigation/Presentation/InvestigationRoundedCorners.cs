using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    /// <summary>
    /// Applies a shared, code-generated nine-sliced rounded rectangle to a
    /// standard uGUI Image without replacing the Image or its serialized links.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class InvestigationRoundedCorners : MonoBehaviour
    {
        private static readonly Dictionary<int, Sprite> Sprites = new Dictionary<int, Sprite>();

        [SerializeField, Min(1f)] private float radius = InvestigationTheme.CornerRadiusControl;

        public float Radius => radius;

        public void Configure(float cornerRadius)
        {
            radius = Mathf.Max(1f, cornerRadius);
            if (Application.isPlaying)
            {
                Apply();
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                Apply();
            }
        }

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
            if (Sprites.TryGetValue(pixelRadius, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }

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
            float halfSize = size * 0.5f;
            float straightHalfExtent = halfSize - pixelRadius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(
                        Mathf.Abs((x + 0.5f) - halfSize),
                        Mathf.Abs((y + 0.5f) - halfSize));
                    Vector2 cornerDistance = point - Vector2.one * straightHalfExtent;
                    Vector2 outside = new Vector2(
                        Mathf.Max(cornerDistance.x, 0f),
                        Mathf.Max(cornerDistance.y, 0f));
                    float signedDistance = outside.magnitude
                        + Mathf.Min(Mathf.Max(cornerDistance.x, cornerDistance.y), 0f)
                        - pixelRadius;
                    float alpha = Mathf.Clamp01(0.5f - signedDistance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Sprites[pixelRadius] = sprite;
            return sprite;
        }
    }
}
