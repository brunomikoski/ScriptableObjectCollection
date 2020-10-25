using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class CollectableScriptableObject : ScriptableObject, IComparable<CollectableScriptableObject>
    {
        [SerializeField, HideInInspector]
        private string guid;
        public string GUID
        {
            get
            {
                SyncGUID();
                return guid;
            }
        }

        [SerializeField, HideInInspector]
        private ScriptableObjectCollection collection;
        public ScriptableObjectCollection Collection => collection;
        
        public void SetCollection(ScriptableObjectCollection collection)
        {
            this.collection = collection;
            ObjectUtility.SetDirty(this);
        }

        public int CompareTo(CollectableScriptableObject other)
        {
            return string.Compare(GUID, other.GUID, StringComparison.Ordinal);
        }

        public override bool Equals(object o)
        {
            CollectableScriptableObject other = o as CollectableScriptableObject;
            if (other == null)
                return false;

            return ReferenceEquals(this, other);
        }

        public static bool operator==(CollectableScriptableObject left, CollectableScriptableObject right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (ReferenceEquals(left, null))
                return false;

            if (ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(CollectableScriptableObject left, CollectableScriptableObject right)
        {
            return !(left == right);
        }

        private void SyncGUID()
        {
            if (!string.IsNullOrEmpty(guid)) 
                return;
            
            guid = Guid.NewGuid().ToString();
#if UNITY_EDITOR
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                guid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
                ObjectUtility.SetDirty(this);
            }
#endif
            
        }
        
        public override int GetHashCode()
        {
            return GUID.GetHashCode();
        }

        public void ValidateGUID()
        {
            SyncGUID();
        }

        public void GenerateNewGUID()
        {
            guid = string.Empty;
            SyncGUID();
        }
    }
}
