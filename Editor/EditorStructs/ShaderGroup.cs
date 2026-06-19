using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEditor.MaterialProperty;

namespace Thry.ThryEditor
{
    public class ShaderGroup : ShaderPart
    {
        public override bool IsPropertyValueDefault
        {
            get
            {
                if(_isPropertyValueDefault == null)
                {
                    _isPropertyValueDefault = Children.All(p => p.IsPropertyValueDefault);
                }
                return _isPropertyValueDefault.Value;
            }
        }

        protected bool? _hasAnimatedDescendant;
        protected bool? _hasRenameAnimatedDescendant;

        public virtual bool HasAnimatedDescendant
        {
            get
            {
                if (_hasAnimatedDescendant == null)
                {
                    _hasAnimatedDescendant = Children.Any(p =>
                        (p is ShaderGroup g && g.HasAnimatedDescendant) ||
                        (p.IsAnimated && !p.IsRenaming));
                }
                return _hasAnimatedDescendant.Value;
            }
        }

        public virtual bool HasRenameAnimatedDescendant
        {
            get
            {
                if (_hasRenameAnimatedDescendant == null)
                {
                    _hasRenameAnimatedDescendant = Children.Any(p =>
                        (p is ShaderGroup g && g.HasRenameAnimatedDescendant) ||
                        (p.IsAnimated && p.IsRenaming));
                }
                return _hasRenameAnimatedDescendant.Value;
            }
        }

        internal void SetAnimatedDescendantStateDirty()
        {
            _hasAnimatedDescendant = null;
            _hasRenameAnimatedDescendant = null;
            (Parent as ShaderGroup)?.SetAnimatedDescendantStateDirty();
        }

        private List<ShaderPart> _children = new List<ShaderPart>();
        private ReadOnlyCollection<ShaderPart> _readonlychildren => new ReadOnlyCollection<ShaderPart>(_children);
        [PublicAPI]
        public ReadOnlyCollection<ShaderPart> Children => _readonlychildren;

        protected bool _isExpanded;
        private bool _isSearchExpanded;

        public ShaderGroup(ShaderEditor shaderEditor) : base(null, 0, "", null, shaderEditor)
        {

        }

        public ShaderGroup(ShaderEditor shaderEditor, MaterialProperty prop, MaterialEditor materialEditor, string displayName, int xOffset, string optionsRaw, int propertyIndex) : base(shaderEditor, prop, xOffset, displayName, optionsRaw, propertyIndex)
        {
            PropertyValueChanged += (PropertyValueEventArgs args) => 
            {
                if(!_doOptionsNeedInitilization && Options.persistent_expand)
                    _isExpanded = this.MaterialProperty.GetNumber() == 1;
            };
        }

        protected override void InitOptions()
        {
            base.InitOptions();
            if (Options.persistent_expand) _isExpanded = this.MaterialProperty.GetNumber() == 1;
            else _isExpanded = Options.default_expand;
        }

        protected bool IsExpanded
        {
            get
            {
                return ShaderEditor.Active.IsInSearchMode ? _isSearchExpanded : _isExpanded;
            }
            set
            {
                if(ShaderEditor.Active.IsInSearchMode)
                {
                    _isSearchExpanded = value;
                    return;
                }
                if (Options.persistent_expand)
                {
                    if (AnimationMode.InAnimationMode())
                    {
#if UNITY_2020_1_OR_NEWER
                        // So we do this instead
                        _isExpanded = value;
#else
                        // This fails when unselecting the object in hirearchy
                        // Then reselecting it
                        // Don't know why
                        // It seems AnimationMode is not working properly in Unity 2022
                        // It worked fine in Unity 2019
                        
                        AnimationMode.StopAnimationMode();
                        this.MaterialProperty.SetNumber(value ? 1 : 0);
                        Undo.SetCurrentGroupName((value ? "Expand" : "Collapse") + $" {Content.text} of {ShaderEditor.Active.TargetName}");
                        RaisePropertyValueChanged();
                        AnimationMode.StartAnimationMode();
#endif
                    }
                    else
                    {
                        this.MaterialProperty.SetNumber(value ? 1 : 0);
                        Undo.SetCurrentGroupName((value ? "Expand" : "Collapse") + $" {Content.text} of {ShaderEditor.Active.TargetName}");
                        RaisePropertyValueChanged();
                    }
                }
                _isExpanded = value;
            }
        }

        public void SetSearchExpanded(bool value)
        {
            _isSearchExpanded = value;
        }

        protected bool DoDisableChildren
        {
            get
            {
                return Options.condition_enable_children != null && !Options.condition_enable_children.Test();
            }
        }

        public void AddPart(ShaderPart part)
        {
            part.SetParent(this);
            _children.Add(part);
        }

        public override void CopyFrom(Material src, bool applyDrawers = true, bool deepCopy = true, bool copyReferenceProperties = true, HashSet<ShaderPropertyType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            if (skipPropertyNames?.Contains(MaterialProperty.name) == true) return;
            if (copyReferenceProperties)
                CopyReferencePropertiesFrom(src, skipPropertyTypes, skipPropertyNames);

            if (deepCopy)
                foreach (ShaderPart p in Children)
                    p.CopyFrom(src, false, true, copyReferenceProperties, skipPropertyTypes, skipPropertyNames);

            if (applyDrawers) MyShaderUI.ApplyDrawers();
        }

        public override void CopyFrom(ShaderPart srcPart, bool applyDrawers = true, bool deepCopy = true, bool copyReferenceProperties = true, HashSet<ShaderPropertyType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            if (skipPropertyNames?.Contains(MaterialProperty.name) == true) return;
            if (skipPropertyNames?.Contains(srcPart.MaterialProperty.name) == true) return;
            if (srcPart is ShaderGroup == false) return;
            ShaderGroup src = srcPart as ShaderGroup;
            if (copyReferenceProperties)
                CopyReferencePropertiesFrom(src, skipPropertyTypes, skipPropertyNames);

            // Match children by property name rather than by index. Matching by index breaks when
            // copying between shaders whose modules have added/removed/reordered properties,
            // causing values to land on the wrong properties even when the names line up.
            if (deepCopy)
            {
                Dictionary<string, ShaderPart> targetChildrenByName = BuildChildLookup(Children);
                foreach (ShaderPart srcChild in src.Children)
                {
                    if (srcChild.MaterialProperty == null) continue;
                    if (targetChildrenByName.TryGetValue(srcChild.MaterialProperty.name, out ShaderPart targetChild))
                    {
                        targetChild.CopyFrom(srcChild, false, true, copyReferenceProperties, skipPropertyTypes, skipPropertyNames);
                    }
                }
            }

            if (applyDrawers) MyShaderUI.ApplyDrawers();
        }

        public override void CopyTo(Material[] targets, bool applyDrawers = true, bool deepCopy = true, bool copyReferenceProperties = true, HashSet<ShaderPropertyType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            if (skipPropertyNames?.Contains(MaterialProperty.name) == true) return;
            if (copyReferenceProperties)
                CopyReferencePropertiesTo(targets, skipPropertyTypes, skipPropertyNames);

            if (deepCopy)
                foreach (ShaderPart p in Children)
                    p.CopyTo(targets, false, true, copyReferenceProperties, skipPropertyTypes, skipPropertyNames);

            if (applyDrawers) MaterialEditor.ApplyMaterialPropertyDrawers(targets);
        }

        public override void CopyTo(ShaderPart targetPart, bool applyDrawers = true, bool deepCopy = true, bool copyReferenceProperties = true, HashSet<ShaderPropertyType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            if (skipPropertyNames?.Contains(MaterialProperty.name) == true) return;
            if (skipPropertyNames?.Contains(targetPart.MaterialProperty.name) == true) return;
            if (targetPart is ShaderGroup == false) return;
            ShaderGroup target = targetPart as ShaderGroup;
            if (copyReferenceProperties)
                CopyReferencePropertiesTo(target, skipPropertyTypes, skipPropertyNames);

            // Match children by property name rather than by index, so copying between shaders whose
            // modules have added/removed/reordered properties still aligns correctly.
            if (deepCopy)
            {
                Dictionary<string, ShaderPart> targetChildrenByName = BuildChildLookup(target.Children);
                foreach (ShaderPart srcChild in Children)
                {
                    if (srcChild.MaterialProperty == null) continue;
                    if (targetChildrenByName.TryGetValue(srcChild.MaterialProperty.name, out ShaderPart targetChild))
                    {
                        srcChild.CopyTo(targetChild, false, true, copyReferenceProperties, skipPropertyTypes, skipPropertyNames);
                    }
                }
            }

            if (applyDrawers) MaterialEditor.ApplyMaterialPropertyDrawers(target.MaterialProperty.targets);
        }

        // Builds a property name -> child lookup for name-based copy matching. Children without a backing
        // MaterialProperty (e.g. labels) carry no value and are skipped; on duplicate names, the first wins.
        private static Dictionary<string, ShaderPart> BuildChildLookup(IEnumerable<ShaderPart> children)
        {
            Dictionary<string, ShaderPart> lookup = new Dictionary<string, ShaderPart>();
            foreach (ShaderPart child in children)
            {
                if (child.MaterialProperty == null) continue;
                if (!lookup.ContainsKey(child.MaterialProperty.name)) lookup.Add(child.MaterialProperty.name, child);
            }
            return lookup;
        }

        protected override void DrawInternal(GUIContent content, Rect? rect = null, bool useEditorIndent = false, bool isInHeader = false)
        {
            if (Options.margin_top > 0)
            {
                GUILayoutUtility.GetRect(0, Options.margin_top);
            }
            foreach (ShaderPart part in Children)
            {
                part.Draw();
            }
        }

        public override void FindUnusedTextures(List<string> unusedList, bool isEnabled)
        {
            if (isEnabled && Options.condition_enable != null)
            {
                isEnabled &= Options.condition_enable.Test();
            }
            foreach (ShaderPart p in (this as ShaderGroup).Children)
                p.FindUnusedTextures(unusedList, isEnabled);
        }

        public void UpdateLinkedMaterials()
        {
            if(ShaderEditor.Active.IsInAnimationMode) return;
            IEnumerable<Material> linked_materials = MaterialLinker.GetLinked(MaterialProperty);
            if (linked_materials != null)
                this.CopyTo(linked_materials.ToArray());
        }

        protected void FoldoutArrow(Rect rect, Event e)
        {
            if (e.type == EventType.Repaint)
            {
                Rect arrowRect = new RectOffset(4, 0, 0, 0).Remove(rect);
                arrowRect.width = 13;
                EditorStyles.foldout.Draw(arrowRect, false, false, IsExpanded, false);
            }
        }

        public override bool Search(string searchTerm, List<ShaderGroup> foundHeaders, bool isParentInSearch = false)
        {
            bool found = isParentInSearch
                || this.Content.text.IndexOf(searchTerm, System.StringComparison.OrdinalIgnoreCase) >= 0
                || this.MaterialProperty?.name.IndexOf(searchTerm, System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool foundInChild = false;
            foreach (ShaderPart p in Children)
            {
                if (p.Search(searchTerm, foundHeaders, isParentInSearch || found))
                    foundInChild = true;
            }
            found |= foundInChild;
            if(found && this is ShaderHeader) foundHeaders.Add(this);
            this.has_not_searchedFor = !found;
            return found;
        }
    }

}