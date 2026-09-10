using UnityEngine;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private RectTransform ednaConversation;
        private Sprite ednaPortrait;
        private Sprite ednaSpeakingPortrait;
        private Sprite ednaAvatar;

        private void EnsureEdnaArtwork()
        {
            if (ednaPortrait == null) ednaPortrait = Resources.Load<Sprite>("Investigation/Edna/edna");
            if (ednaSpeakingPortrait == null) ednaSpeakingPortrait = Resources.Load<Sprite>("Investigation/Edna/edna-speaking");
            if (ednaAvatar != null || ednaPortrait == null) return;
            // Frame the new neutral portrait's complete head; share its texture
            // rather than retaining the old character's separate avatar image.
            Rect r = ednaPortrait.rect;
            ednaAvatar = Sprite.Create(ednaPortrait.texture,
                new Rect(r.x + r.width * .16f, r.y + r.height * .40f, r.width * .68f, r.height * .60f),
                new Vector2(.5f, .5f), ednaPortrait.pixelsPerUnit, 0, SpriteMeshType.FullRect, Vector4.zero, false);
            ednaAvatar.name = "EDNA Avatar";
        }

        private Sprite EdnaSpeakingArtwork => ednaSpeakingPortrait != null ? ednaSpeakingPortrait : ednaPortrait;

        private void RenderEdnaPresentation()
        {
            if (ScenarioWorkspaceActive) RenderScenarioEdnaDock();
        }

        private void RemoveEdnaPresentation()
        {
            RemoveEdnaObject(ednaConversation);
            ednaConversation = null;
        }

        private static void RemoveEdnaObject(RectTransform item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(item.gameObject);
            else DestroyImmediate(item.gameObject);
        }

        private void OnDestroy()
        {
            if (ednaAvatar == null) return;
            if (Application.isPlaying) Destroy(ednaAvatar);
            else DestroyImmediate(ednaAvatar);
        }
    }
}
