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

        [NonSerialized]
        private List<ScriptableObjectCollectionItem> editorSerializedItems;
        
        [SerializeField]
        protected List<ScriptableObjectCollectionItem> items = new List<ScriptableObjectCollectionItem>();
        
        [NonSerialized]
        private bool isReadyOnlyListDirty = true;

#pragma warning disable 0414
        [SerializeField]
        private bool automaticallyLoaded = true;
#if UNITY_EDITOR
        [SerializeField]
        private bool generateAsPartialClass = true;
        [SerializeField]
        private string generatedFileLocationPath;
        [SerializeField]
        private string generatedStaticClassFileName;
        [SerializeField]
        private string generateStaticFileNamespace;
#pragma warning restore 0414
#endif
        
        private IReadOnlyList<ScriptableObjectCollectionItem> readOnlyList = new List<ScriptableObjectCollectionItem>();
        public IReadOnlyList<ScriptableObjectCollectionItem> Items
        {
            get
            {
                if (isReadyOnlyListDirty)
                {
                    readOnlyList = items.AsReadOnly();
                    
                    isReadyOnlyListDirty = false;
                }
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
        
        public ScriptableObjectCollectionItem this[int index]
        {
            get => items[index];
            set => throw new NotSupportedException();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<ScriptableObjectCollectionItem> GetEnumerator()
        {
            using (IEnumerator<ScriptableObjectCollectionItem> itemEnum = items.GetEnumerator())
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
            foreach (ScriptableObjectCollectionItem e in this)
            {
                array.SetValue(e, index + i);
                ++i;
            }
        }
        
        public void CopyTo(List<ScriptableObjectCollectionItem> list)
        {
            list.Capacity = Math.Max(list.Capacity, Count);
            foreach (ScriptableObjectCollectionItem e in this)
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
            Add((ScriptableObjectCollectionItem) value);
            return Count - 1;
        }

        public bool Add(ScriptableObjectCollectionItem item)
        {
            if (items.Contains(item))
                return false;
            
            items.Add(item);

            item.SetCollection(this);
            ObjectUtility.SetDirty(this);
            isReadyOnlyListDirty = true;
            return true;
        }

#if UNITY_EDITOR
        
        public ScriptableObjectCollectionItem AddNew(Type collectionType, string assetName = "")
        {
            if (Application.isPlaying)
                throw new NotSupportedException();
            
            ScriptableObjectCollectionItem item = (ScriptableObjectCollectionItem)CreateInstance(collectionType);
            string assetPath = Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(this));
            string parentFolderPath = Path.Combine(assetPath, "Items");
            AssetDatabaseUtils.CreatePathIfDontExist(parentFolderPath);

            string itemName = assetName;

            if (string.IsNullOrEmpty(itemName))
            {
                int count = Count;
                while (true)
                {
                    itemName = $"New{collectionType.Name}{count}";
                    string testPath = Path.Combine(parentFolderPath, itemName);

                    if (!File.Exists(Path.GetFullPath($"{testPath}.asset")))
                        break;
                
                    count++;
                }
            }
            
            item.name = itemName;

            if(itemName.IsReservedKeyword())
                Debug.LogError($"{itemName} is a reserved keyword name, will cause issues with code generation, please rename it");

            string newFileName = Path.Combine(parentFolderPath, item.name + ".asset");
            
            this.Add(item);
            UnityEditor.AssetDatabase.CreateAsset(item, newFileName);
            ObjectUtility.SetDirty(this);
            isReadyOnlyListDirty = true;
            return item;
        }
#endif

        public Type GetItemType()
        {
            Type enumType = GetGenericItemType();
            if (enumType == null) return null;
            return enumType.GetGenericArguments().First();
        }

        private Type GetGenericItemType()
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

        public virtual void Sort()
        {
            items.Sort();
            isReadyOnlyListDirty = true;
            ObjectUtility.SetDirty(this);
        }

        public void Clear()
        {
            items.Clear();
            ObjectUtility.SetDirty(this);
            isReadyOnlyListDirty = true;
        }

        public bool Contains(object value)
        {
            return Contains((ScriptableObjectCollectionItem) value);
        }

        public bool Contains(ScriptableObjectCollectionItem item)
        {
            return items.Contains(item);
        }

        public int IndexOf(object value)
        {
            return IndexOf((ScriptableObjectCollectionItem) value);
        }

        public int IndexOf(ScriptableObjectCollectionItem item)
        {
            return items.IndexOf(item);
        }

        public void Insert(int index, ScriptableObjectCollectionItem item)
        {
            items.Insert(index, item);
            item.SetCollection(this);
            ObjectUtility.SetDirty(this);
            isReadyOnlyListDirty = true;
        }

        public void Insert(int index, object value)
        {
            Insert(index, (ScriptableObjectCollectionItem)value);
        }

        public bool Remove(ScriptableObjectCollectionItem item)
        {
            bool result =  items.Remove(item);
            ObjectUtility.SetDirty(this);
            isReadyOnlyListDirty = true;

            return result;
        }

        public void Remove(object value)
        {
            Remove((ScriptableObjectCollectionItem) value);
        }

        public void RemoveAt(int index)
        {
            items.RemoveAt(index);
            ObjectUtility.SetDirty(this);
            isReadyOnlyListDirty = true;
        }

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (ScriptableObjectCollectionItem) value;
        }

        public void Swap(int targetIndex, int newIndex)
        {
            if (targetIndex >= items.Count || newIndex >= items.Count)
                return;

            ScriptableObjectCollectionItem temp = items[targetIndex];
            items[targetIndex] = items[newIndex];
            items[newIndex] = temp;
            ObjectUtility.SetDirty(this);
            isReadyOnlyListDirty = true;
        }

        public void ClearBadItems()
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i].IsNull() || items[i] == null)
                    items.RemoveAt(i);
            }
            ObjectUtility.SetDirty(this);
            isReadyOnlyListDirty = true;
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
                    
                    if (string.Equals(items[i].GUID, items[j].GUID, StringComparison.Ordinal))
                    {
                        items[j].GenerateNewGUID();
                        Debug.LogWarning($"Found duplicated GUID, please regenerate code of collection {this.name}",
                            this);
                    }
                }
            }
        }
        
        
        public void RefreshCollection()
        {
#if UNITY_EDITOR
            Type collectionType = GetItemType();
            if (collectionType == null)
                return;
            
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{collectionType.Name}");

            for (int i = 0; i < guids.Length; i++)
            {
                ScriptableObjectCollectionItem item =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObjectCollectionItem>(
                        UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]));
                
                if (item == null)
                    continue;

                if (item.Collection != this)
                    continue;

                if (!PathUtility.IsObjectDeeperThanObject(item, this))
                    continue;

                Add(item);
            }

            items = items.Where(o => o != null).Distinct().ToList();
            ObjectUtility.SetDirty(this);
            isReadyOnlyListDirty = true;
#endif
        }

        [Obsolete("TryGetCollectableByGUID is deprecated, Use TryGetItemByGUID instead")]
        public bool TryGetCollectableByGUID(string itemGUID, out ScriptableObjectCollectionItem scriptableObjectCollectionItem)
        {
            return TryGetItemByGUID(itemGUID, out scriptableObjectCollectionItem);
        }

        public bool TryGetItemByGUID(string itemGUID, out ScriptableObjectCollectionItem scriptableObjectCollectionItem)
        {
            for (int i = 0; i < items.Count; i++)
            {
                ScriptableObjectCollectionItem item = items[i];
                if (string.Equals(item.GUID, itemGUID, StringComparison.Ordinal))
                {
                    scriptableObjectCollectionItem = item;
                    return true;
                }
            }

            scriptableObjectCollectionItem = null;
            return false;
        }

        internal void PrepareForPlayMode()
        {
            editorSerializedItems = new List<ScriptableObjectCollectionItem>(items);
        }

        internal void PrepareForEditorMode()
        {
            items = new List<ScriptableObjectCollectionItem>(editorSerializedItems);
            ObjectUtility.SetDirty(this);
        }

    }

    public class ScriptableObjectCollection<ObjectType> : ScriptableObjectCollection, IList<ObjectType>
        where ObjectType : ScriptableObjectCollectionItem
    {
        private static ScriptableObjectCollection<ObjectType> instance;
        public static ScriptableObjectCollection<ObjectType> Values
        {
            get
            {
                if (instance == null)
                {
                    if (CollectionsRegistry.Instance.TryGetCollectionFromItemType(out ScriptableObjectCollection<ObjectType> result))
                        instance = result;
                }
                
                return instance;
            }
        }

        public new ObjectType this[int index]
        {
            get => (ObjectType)base[index];
            set => base[index] = value;
        }

        public new IEnumerator<ObjectType> GetEnumerator()
        {
            using (IEnumerator<ScriptableObjectCollectionItem> itemEnum = base.GetEnumerator())
            {
                while (itemEnum.MoveNext())
                {
                    if (itemEnum.Current.IsNull())
                        continue;
                    ObjectType obj = itemEnum.Current as ObjectType;
                    if (obj == null)
                        continue;
                    yield return obj;
                }
            }
        }

#if UNITY_EDITOR

        public T GetOrAddNew<T>(string targetName = null) where T : ObjectType
        {
            if (!string.IsNullOrEmpty(targetName))
            {
                T item = Items.FirstOrDefault(o => o.name.Equals(targetName, StringComparison.Ordinal)) as T;
                if (item != null)
                    return item;
            }

            return (T) AddNew(typeof(T), targetName);
        }
        
        public ObjectType GetOrAddNew(Type collectionType, string targetName)
        {
            ObjectType item = Items.FirstOrDefault(o => o.name.Equals(targetName, StringComparison.Ordinal)) as ObjectType;
            if (item != null)
                return item;

            return (ObjectType) AddNew(collectionType, targetName);
        }
        
        public ObjectType GetOrAddNew(string targetName)
        {
            ObjectType item = Items.FirstOrDefault(o => o.name.Equals(targetName, StringComparison.Ordinal)) as ObjectType;
            if (item != null)
                return item;

            return AddNew(targetName);
        }
        
        public ObjectType AddNew(string targetName)
        {
            return (ObjectType) AddNew(GetItemType(), targetName);
        } 
        
        public ObjectType AddNew() 
        {
            return (ObjectType)AddNew(GetItemType());
        } 
#endif

        [Obsolete("GetCollectableByGUID is deprecated, Use GetItemByGUID instead")]
        public ObjectType GetCollectableByGUID(string targetGUID)
        {
            return GetItemByGUID(targetGUID);   
        }
        
        public ObjectType GetItemByGUID(string targetGUID)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                ScriptableObjectCollectionItem item = Items[i];
                if (string.Equals(item.GUID, targetGUID, StringComparison.Ordinal))
                    return (ObjectType) item;
            }

            return null;
        }

        public void Add(ObjectType item)
        {
            base.Add(item);
        }

        public ObjectType Add(Type itemType = null)
        {
            ObjectType item = base.Add(itemType) as ObjectType;
            return item;
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
            bool remove = base.Remove(item);
            return remove;
        }
        
        IEnumerator<ObjectType> IEnumerable<ObjectType>.GetEnumerator()
        {
            using (IEnumerator<ScriptableObjectCollectionItem> enumerator = base.GetEnumerator())
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
