using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class ScriptableObjectCollectionItem : ScriptableObject, IComparable<ScriptableObjectCollectionItem>
    {
        [SerializeField, HideInInspector]
        private string guid;
        public string GUID
        {
            get
            {
                if (!string.IsNullOrEmpty(guid))
                    return guid;
                
#if UNITY_EDITOR
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    guid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
                    ObjectUtility.SetDirty(this);
                }
#else
                guid = Guid.NewGuid().ToString();
#endif
                return guid;
            }
        }

        [SerializeField, HideInInspector]
        private string collectionGUID;
        
        private ScriptableObjectCollection cachedScriptableObjectCollection;
        public ScriptableObjectCollection Collection
        {
            get
            {
                if (cachedScriptableObjectCollection == null)
                {
                    if (!string.IsNullOrEmpty(collectionGUID))
                    {
                        cachedScriptableObjectCollection = CollectionsRegistry.Instance.GetCollectionByGUID(collectionGUID);
                    }
                    else
                    {
                        CollectionsRegistry.Instance.TryGetCollectionFromItemType(GetType(), out cachedScriptableObjectCollection);
                        if (cachedScriptableObjectCollection != null)
                        {
                            Debug.Log($"Collection Item ({this.name}) was missing the Collection GUID, assigned to {cachedScriptableObjectCollection.name}");
                            collectionGUID = cachedScriptableObjectCollection.GUID;
                            ObjectUtility.SetDirty(this);
                        }
                    }
                }
                
                return cachedScriptableObjectCollection;
            }
        }

        public void SetCollection(ScriptableObjectCollection collection)
        {
            cachedScriptableObjectCollection = collection;
            collectionGUID = cachedScriptableObjectCollection.GUID;
            ObjectUtility.SetDirty(this);
        }

        public int CompareTo(ScriptableObjectCollectionItem other)
        {
            return string.Compare(GUID, other.GUID, StringComparison.Ordinal);
        }

        public override bool Equals(object o)
        {
            ScriptableObjectCollectionItem other = o as ScriptableObjectCollectionItem;
            if (other == null)
                return false;

            return ReferenceEquals(this, other);
        }

        public static bool operator==(ScriptableObjectCollectionItem left, ScriptableObjectCollectionItem right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (ReferenceEquals(left, null))
                return false;

            if (ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(ScriptableObjectCollectionItem left, ScriptableObjectCollectionItem right)
        {
            return !(left == right);
        }
        
        public override int GetHashCode()
        {
            return GUID.GetHashCode();
        }
        
        public void InvalidateGUID()
        {
            guid = string.Empty;
        }
    }
}
