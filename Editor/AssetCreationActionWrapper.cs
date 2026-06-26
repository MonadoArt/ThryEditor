using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Thry.ThryEditor
{
    // Wraps the Unity asset-creation callback API so the version difference between
    // EndNameEditAction (legacy, int instance ids) and AssetCreationEndAction
    // (Unity 6.5+, EntityId) is handled in a single place.
    public abstract class AssetCreationActionWrapper :
#if UNITY_6000_5_OR_NEWER
        AssetCreationEndAction
#else
        EndNameEditAction
#endif
    {
#if UNITY_6000_5_OR_NEWER
        public sealed override void Action(EntityId entityId, string pathName, string resourceFile) => OnCreateAsset(pathName, resourceFile);
        public sealed override void Cancelled(EntityId entityId, string pathName, string resourceFile) => OnCancelled(pathName, resourceFile);
#else
        public sealed override void Action(int instanceId, string pathName, string resourceFile) => OnCreateAsset(pathName, resourceFile);
        public sealed override void Cancelled(int instanceId, string pathName, string resourceFile) => OnCancelled(pathName, resourceFile);
#endif

        protected abstract void OnCreateAsset(string pathName, string resourceFile);

        protected virtual void OnCancelled(string pathName, string resourceFile) => Selection.activeObject = null;

        public static void StartNameEditing(AssetCreationActionWrapper endAction, string pathName, Texture2D icon = null, string resourceFile = null)
        {
#if UNITY_6000_5_OR_NEWER
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(EntityId.None, endAction, pathName, icon, resourceFile);
#else
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endAction, pathName, icon, resourceFile);
#endif
        }
    }
}
