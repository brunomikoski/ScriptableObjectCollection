using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class ScriptableObjectCollection : ScriptableObject, IList
    {
        [SerializeField]
        private string guid;
        public string GUID
        {
            get
            {
                SyncGUID();
                return guid;
            }
        }

        [SerializeField]
        private List<CollectableScriptableObject> items = new List<CollectableScriptableObject>();

        private bool isReadyListDirty = true;
        private IReadOnlyList<CollectableScriptableObject> readOnlyList = new List<CollectableScriptableObject>();
        public IReadOnlyList<CollectableScriptableObject> Items
        {
            get
            {
                if (isReadyListDirty)
                    readOnlyList = items.AsReadOnly();
                return readOnlyList;
            }
        }

        private void SyncGUID()
        {
            if (!string.IsNullOrEmpty(guid)) 
                return;
            
            guid = Guid.NewGuid().ToString();
#if UNITY_EDITOR
            guid = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(this));
            ObjectUtility.SetDirty(this);
#endif
        }
        
        public CollectableScriptableObject this[int index]
        {
            get => items[index];
            set => throw new NotSupportedException();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<CollectableScriptableObject> GetEnumerator()
        {
            using (IEnumerator<CollectableScriptableObject> itemEnum = items.GetEnumerator())
            {
                while (itemEnum.MoveNext())
                {
                    if (itemEnum.Current.IsNull())
                        continue;
                    yield return itemEnum.Current;
                }
            }
        }

        public void CopyTo(Array array, int index)
        {
            int i = 0;
            foreach (CollectableScriptableObject e in this)
            {
                array.SetValue(e, index + i);
                ++i;
            }
        }
        
        public void CopyTo(List<CollectableScriptableObject> list)
        {
            list.Capacity = Math.Max(list.Capacity, Count);
            foreach (CollectableScriptableObject e in this)
            {
                list.Add(e);
            }
        }

        public int Count => items.Count;

        public object SyncRoot => throw new NotSupportedException();
        public bool IsSynchronized => throw new NotSupportedException();
        
        public bool IsFixedSize => false;
        public bool IsReadOnly => false;

        
        public int Add(object value)
        {
            Add((CollectableScriptableObject) value);
            return Count - 1;
        }

        public bool Add(CollectableScriptableObject item)
        {
            if (items.Contains(item))
                return false;
            
            items.Add(item);
            item.SetCollection(this);
            ObjectUtility.SetDirty(this);
            isReadyListDirty = true;
            return true;
        }

#if UNITY_EDITOR
        public CollectableScriptableObject AddNew(Type collectionType)
        {
            if (Application.isPlaying)
                throw new NotSupportedException();
            
            CollectableScriptableObject item = (CollectableScriptableObject)CreateInstance(collectionType);
            string assetPath = Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(this));
            string parentFolderPath = Path.Combine(assetPath, "Items");
            AssetDatabaseUtils.CreatePathIfDontExist(parentFolderPath);
            
            string itemName;
            int count = Count;
            while (true)
            {
                itemName = $"New{collectionType.Name}{count}";
                string testPath = Path.Combine(parentFolderPath, itemName);

                if (!File.Exists(Path.GetFullPath($"{testPath}.asset")))
                    break;
                
                count++;
            }
            
            item.name = itemName;
            string newFileName = Path.Combine(parentFolderPath, item.name + ".asset");
            
            this.Add(item);
            UnityEditor.AssetDatabase.CreateAsset(item, newFileName);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            isReadyListDirty = true;
            return item;
        }
#endif

        public Type GetCollectionType()
        {
            Type enumType = GetGenericEnumType();
            if (enumType == null) return null;
            return enumType.GetGenericArguments().First();
        }
        
        public Type GetGenericEnumType()
        {
            Type baseType = GetType().BaseType;

            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(ScriptableObjectCollection<>))
                    return baseType;
                baseType = baseType.BaseType;
            }
            return null;
        }

        public void Clear()
        {
            items.Clear();
            ObjectUtility.SetDirty(this);
            isReadyListDirty = true;
        }

        public bool Contains(object value)
        {
            return Contains((CollectableScriptableObject) value);
        }

        public bool Contains(CollectableScriptableObject item)
        {
            return items.Contains(item);
        }

        public int IndexOf(object value)
        {
            return IndexOf((CollectableScriptableObject) value);
        }

        public int IndexOf(CollectableScriptableObject item)
        {
            return items.IndexOf(item);
        }

        public void Insert(int index, CollectableScriptableObject item)
        {
            items.Insert(index, item);
            item.SetCollection(this);
            ObjectUtility.SetDirty(this);
            isReadyListDirty = true;
        }

        public void Insert(int index, object value)
        {
            Insert(index, (CollectableScriptableObject)value);
        }

        public bool Remove(CollectableScriptableObject item)
        {
            bool result =  items.Remove(item);
            ObjectUtility.SetDirty(this);
            isReadyListDirty = true;

            return result;
        }

        public void Remove(object value)
        {
            Remove((CollectableScriptableObject) value);
        }

        public void RemoveAt(int index)
        {
            items.RemoveAt(index);
            ObjectUtility.SetDirty(this);
            isReadyListDirty = true;
        }

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (CollectableScriptableObject) value;
        }

        public void Swap(int targetIndex, int newIndex)
        {
            if (targetIndex >= items.Count || newIndex >= items.Count)
                return;

            CollectableScriptableObject temp = items[targetIndex];
            items[targetIndex] = items[newIndex];
            items[newIndex] = temp;
            ObjectUtility.SetDirty(this);
            isReadyListDirty = true;
        }

        public void ClearBadItems()
        {
            items = items.Where(o => o != null).Distinct().ToList();
            ObjectUtility.SetDirty(this);
            isReadyListDirty = true;
        }

        public void ValidateGUID()
        {
            SyncGUID();
            for (int i = 0; i < items.Count; i++)
            {
                if(items[i] == null)
                    continue;
                items[i].ValidateGUID();

                for (int j = 0; j < items.Count; j++)
                {
                    if (items[j] == null)
                        continue;
                    
                    if (items[i] == items[j])
                        continue;
                    
                    if (string.Equals(items[i].GUID, Items[j].GUID, StringComparison.Ordinal))
                    {
                        Items[j].GenerateNewGUID();
                        Debug.LogWarning($"Found duplicated GUID, please regenerate code of collection {this.name}",
                            this);
                    }
                }
            }
        }
        
        
        public void RefreshCollection()
        {
#if UNITY_EDITOR
            Type collectionType = GetCollectionType();
            if (collectionType == null)
                return;
            
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{collectionType.Name}");

            for (int i = 0; i < guids.Length; i++)
            {
                CollectableScriptableObject collectable =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<CollectableScriptableObject>(
                        UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]));
                
                if (collectable == null)
                    continue;

                if (collectable.Collection != this)
                    continue;

                if (!PathUtility.IsObjectDeeperThanObject(collectable, this))
                    continue;

                Add(collectable);
            }

            items = items.Where(o => o != null).Distinct().ToList();
            ObjectUtility.SetDirty(this);
            isReadyListDirty = true;
#endif
        }

        public bool TryGetCollectableByGUID(string itemGUID, out CollectableScriptableObject collectableScriptableObject)
        {
            for (int i = 0; i < items.Count; i++)
            {
                CollectableScriptableObject collectable = items[i];
                if (string.Equals(collectable.GUID, itemGUID, StringComparison.Ordinal))
                {
                    collectableScriptableObject = collectable;
                    return true;
                }
            }

            collectableScriptableObject = null;
            return false;
        }
    }

    public class ScriptableObjectCollection<ObjectType> : ScriptableObjectCollection, IList<ObjectType>
        where ObjectType : CollectableScriptableObject
    {
        public new ObjectType this[int index]
        {
            get => (ObjectType)base[index];
            set => base[index] = value;
        }
        
        
        public ObjectType GetCollectableByGUID(string targetGUID)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                CollectableScriptableObject collectable = Items[i];
                if (string.Equals(collectable.GUID, targetGUID, StringComparison.Ordinal))
                    return (ObjectType) collectable;
            }

            return null;
        }
        
        public void Add(ObjectType item)
        {
            base.Add(item);
        }

        public ObjectType Add(Type itemType = null)
        {
            return base.Add(itemType) as ObjectType;
        }

        public bool Contains(ObjectType item)
        {
            return base.Contains(item);
        }

        public void CopyTo(ObjectType[] array, int arrayIndex)
        {
            base.CopyTo(array, arrayIndex);
        }

        public int IndexOf(ObjectType item)
        {
            return base.IndexOf(item);
        }

        public void Insert(int index, ObjectType item)
        {
            base.Insert(index, item);
        }

        public bool Remove(ObjectType item)
        {
            return base.Remove(item);
        }
        
        IEnumerator<ObjectType> IEnumerable<ObjectType>.GetEnumerator()
        {
            using (IEnumerator<CollectableScriptableObject> enumerator = base.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    yield return (ObjectType)enumerator.Current;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return base.GetEnumerator();
        }
    }
}
