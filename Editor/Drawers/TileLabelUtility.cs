// Powers right-click rename of UV Tile Discard / Face Discard tile buttons.
// Labels are stored as material override tags so the underlying `_UDIMDiscardRow*` shader
// properties stay untouched. Only `_UDIM(Face)?DiscardRow\d_\d` properties opt in; every
// other usage of the host drawers is unaffected.
//
// Original Concept created by an anonymous user (refused credit). Implemented officially by BluWizard LABS.

using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor.Drawers
{
    internal static class TileLabelUtility
    {
        internal const string TAG_PREFIX = "thry_tile_label_";
        internal const string ROW_TOOLTIP = "Right-click any tile button to rename it";

        // Poiyomi's lock-in renames animated properties to `<name>_<suffix>`. Stripping the suffix
        // back to the canonical name keeps the tag key stable across lock/unlock cycles regardless
        // of which rename-suffix the user chose. Read-only — we never write a non-canonical tag.
        static readonly Regex CANONICAL_UDIM_NAME = new Regex(@"^(_UDIM(?:Face)?DiscardRow\d_\d)(?:_.+)?$", RegexOptions.Compiled);

        internal static bool IsUdimProperty(string propertyName)
        {
            return !string.IsNullOrEmpty(propertyName) && CANONICAL_UDIM_NAME.IsMatch(propertyName);
        }

        internal static string CanonicalPropertyName(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return propertyName;
            Match m = CANONICAL_UDIM_NAME.Match(propertyName);
            return m.Success ? m.Groups[1].Value : propertyName;
        }

        // Returns the user-set label, or null if none exists / property is not UDIM / mat is null.
        // Falls back to a tag accidentally saved under the runtime (suffixed) name from before
        // canonicalization, in case a material was tagged while locked.
        internal static string GetTileLabel(Material mat, string propertyName)
        {
            if (mat == null || !IsUdimProperty(propertyName)) return null;
            string canonical = CanonicalPropertyName(propertyName);
            string tag = mat.GetTag(TAG_PREFIX + canonical, false, string.Empty);
            if (!string.IsNullOrEmpty(tag)) return tag;
            if (canonical != propertyName)
            {
                tag = mat.GetTag(TAG_PREFIX + propertyName, false, string.Empty);
                if (!string.IsNullOrEmpty(tag)) return tag;
            }
            return null;
        }

        // Drop into the per-button render loop. Intercepts right-click on `buttonRect` and pops the
        // Rename / Reset context menu. Safe to call every frame; only acts on MouseDown/ContextClick.
        internal static void HandleRightClick(Rect buttonRect, Object[] targets, string propertyName, string defaultLabel)
        {
            if (!IsUdimProperty(propertyName)) return;
            Event evt = Event.current;
            if (evt == null) return;

            if (evt.type == EventType.MouseDown && evt.button == 1 && buttonRect.Contains(evt.mousePosition))
            {
                Vector2 screenPos = GUIUtility.GUIToScreenPoint(evt.mousePosition);
                ShowContextMenu(targets, propertyName, defaultLabel, screenPos);
                evt.Use();
            }
            else if (evt.type == EventType.ContextClick && buttonRect.Contains(evt.mousePosition))
            {
                // Swallow so the host inspector's own context menu (if any) doesn't double-fire.
                evt.Use();
            }
        }

        static void ShowContextMenu(Object[] targets, string propertyName, string defaultLabel, Vector2 screenPos)
        {
            Object[] capturedTargets = new Object[targets != null ? targets.Length : 0];
            if (targets != null) System.Array.Copy(targets, capturedTargets, targets.Length);
            string canonical = CanonicalPropertyName(propertyName);
            string runtimeName = canonical != propertyName ? propertyName : null;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Rename..."), false, () =>
            {
                TileLabelRenamePopup.Show(capturedTargets, canonical, defaultLabel, screenPos);
            });
            menu.AddItem(new GUIContent("Reset to default label"), false, () =>
            {
                ApplyTagToTargets(capturedTargets, canonical, string.Empty);
                // Clear any stale tag saved under the runtime (suffixed) name from before normalisation.
                if (runtimeName != null)
                    ApplyTagToTargets(capturedTargets, runtimeName, string.Empty);
            });
            menu.ShowAsContext();
        }

        internal static void ApplyTagToTargets(Object[] targets, string canonicalPropertyName, string value)
        {
            if (targets == null || targets.Length == 0 || string.IsNullOrEmpty(canonicalPropertyName)) return;

            Undo.RegisterCompleteObjectUndo(targets, "Rename UV Tile Label");
            string tagKey = TAG_PREFIX + canonicalPropertyName;
            foreach (var t in targets)
            {
                if (t is Material mat)
                {
                    mat.SetOverrideTag(tagKey, value ?? string.Empty);
                    EditorUtility.SetDirty(mat);
                }
            }
        }

        internal class TileLabelRenamePopup : EditorWindow
        {
            Object[] _targets;
            string _canonicalPropertyName;
            string _value;
            bool _focusGrabbed;

            public static void Show(Object[] targets, string canonicalPropertyName, string defaultLabel, Vector2 screenPos)
            {
                var win = CreateInstance<TileLabelRenamePopup>();
                win._targets = targets;
                win._canonicalPropertyName = canonicalPropertyName;
                Material firstMat = (targets != null && targets.Length > 0) ? targets[0] as Material : null;
                string current = firstMat != null ? firstMat.GetTag(TAG_PREFIX + canonicalPropertyName, false, string.Empty) : string.Empty;
                win._value = string.IsNullOrEmpty(current) ? (defaultLabel ?? string.Empty) : current;
                win.titleContent = new GUIContent("Rename tile label");
                win.position = new Rect(screenPos.x, screenPos.y, 260f, 80f);
                win.ShowPopup();
                win.Focus();
            }

            void OnGUI()
            {
                Event e = Event.current;
                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
                {
                    Close();
                    e.Use();
                    return;
                }
                bool submitOnEnter = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);

                GUILayout.Space(6);

                GUI.SetNextControlName("LabelField");
                _value = EditorGUILayout.TextField("Label", _value);
                if (!_focusGrabbed)
                {
                    EditorGUI.FocusTextInControl("LabelField");
                    _focusGrabbed = true;
                }

                GUILayout.Space(4);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                bool cancel = GUILayout.Button("Cancel", GUILayout.Width(80));
                bool ok = GUILayout.Button("OK", GUILayout.Width(80));
                GUILayout.EndHorizontal();

                if (cancel)
                {
                    Close();
                    return;
                }
                if (ok || submitOnEnter)
                {
                    ApplyTagToTargets(_targets, _canonicalPropertyName, _value);
                    Close();
                }
            }

            void OnLostFocus()
            {
                Close();
            }
        }
    }
}
