using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrunoMikoski.ScriptableObjectCollections
{
    public abstract class ScriptableObjectCollection : ScriptableObject, IList
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
        public List<ScriptableObjectCollectionItem> Items => items;

#pragma warning disable 0414
        [SerializeField]
        private bool automaticallyLoaded = true;
#if UNITY_EDITOR
        [SerializeField]
        private bool generateAsPartialClass = true;
        [SerializeField]
        private bool generateAsBaseClass = false;
        [SerializeField]
        private string generatedFileLocationPath;
        [SerializeField]
        private string generatedStaticClassFileName;
        [SerializeField]
        private string generateStaticFileNamespace;
#pragma warning restore 0414
#endif

        private void SyncGUID()
        {
            if (!string.IsNullOrEmpty(guid)) 
                return;
            
            guid = Guid.NewGuid().ToString();
#if UNITY_EDITOR
            guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
            ObjectUtility.SetDirty(this);
#endif
        }

        internal void GenerateNewGUID()
        {
            guid = string.Empty;
            SyncGUID();
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
            ClearCachedValues();
            return true;
        }

#if UNITY_EDITOR
        
        public ScriptableObjectCollectionItem AddNew(Type collectionType, string assetName = "")
        {
            if (Application.isPlaying)
                throw new NotSupportedException();
            
            ScriptableObjectCollectionItem item = (ScriptableObjectCollectionItem)CreateInstance(collectionType);
            string assetPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
            string parentFolderPath = Path.Combine(assetPath, item.IsReference() ? "References" : "Items");
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

            AssetDatabase.CreateAsset(item, newFileName);
            ObjectUtility.SetDirty(this);

            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.ApplyModifiedProperties();

            return item;
        }

        public abstract void Synchronize();
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
            ObjectUtility.SetDirty(this);
        }

        public void Clear()
        {
            items.Clear();
            ObjectUtility.SetDirty(this);
        }

        public bool Contains(object value)
        {
            return Contains((ScriptableObjectCollectionItem) value);
        }

        public bool Contains(ScriptableObjectCollectionItem item)
        {
            return items.Contains(item);
        }

        public bool ContainsReferenceTo(ScriptableObjectCollectionItem item)
        {
            return items.Exists(collectionItem =>
                collectionItem.TryGetReference(out ScriptableObjectReferenceItem reference) && 
                String.Equals(reference.TargetGuid, item.GUID, StringComparison.OrdinalIgnoreCase));
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
        }

        public void Insert(int index, object value)
        {
            Insert(index, (ScriptableObjectCollectionItem)value);
        }

        public bool Remove(ScriptableObjectCollectionItem item)
        {
            bool result =  items.Remove(item);
            ObjectUtility.SetDirty(this);
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

            (items[targetIndex], items[newIndex]) = (items[newIndex], items[targetIndex]);
            ObjectUtility.SetDirty(this);
        }

        public void ClearBadItems()
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i].IsNull() || items[i] == null)
                    items.RemoveAt(i);
            }
            ObjectUtility.SetDirty(this);
        }

        public void ValidateGUIDs()
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
        
        [ContextMenu("Refresh Collection")]
        public void RefreshCollection()
        {
#if UNITY_EDITOR
            Type collectionType = GetItemType();
            if (collectionType == null)
                return;

            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
            string[] guids = AssetDatabase.FindAssets($"t:{collectionType.Name}", new []{folder});

            for (int i = 0; i < guids.Length; i++)
            {
                ScriptableObjectCollectionItem item =
                    AssetDatabase.LoadAssetAtPath<ScriptableObjectCollectionItem>(AssetDatabase.GUIDToAssetPath(guids[i]));
                
                if (item == null)
                    continue;

                if (item.Collection != this)
                    continue;

                if (item.Collection.Contains(item))
                    continue;
                
                if (item.Collection.ContainsReferenceTo(item))
                    continue;
                
                Debug.Log($"Adding {item.name} to the Collection {this.name} its inside of the folder {folder}");
                Add(item);
            }

            items = items.Where(o => o != null).Distinct().ToList();
            ObjectUtility.SetDirty(this);
#endif
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

        protected virtual void ClearCachedValues()
        {
        }
    }

    public class ScriptableObjectCollection<ObjectType> : ScriptableObjectCollection, IList<ObjectType>
        where ObjectType : ScriptableObjectCollectionItem
    {
        private static List<ObjectType> cachedValues;
        public static IReadOnlyList<ObjectType> Values
        {
            get
            {
                if (cachedValues == null)
                    cachedValues = CollectionsRegistry.Instance.GetAllCollectionItemsOfType<ObjectType>();
                return cachedValues;
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
            ClearCachedValues();
        }

        public ObjectType Add(Type itemType = null)
        {
            ObjectType item = base.Add(itemType) as ObjectType;
            ClearCachedValues();
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
            ClearCachedValues();
        }

        public bool Remove(ObjectType item)
        {
            bool remove = base.Remove(item);
            ClearCachedValues();
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
        
        protected override void ClearCachedValues()
        {
            cachedValues = null;
        }

#if UNITY_EDITOR
        public override void Synchronize()
        {
            List<ObjectType> newList = new List<ObjectType>();

            // purge all invalid entries, this calls GetEnumerator which skips invalid entries
            using (IEnumerator<ObjectType> enumerator = GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    ObjectType item = enumerator.Current;
                    newList.Add(item);
                }
            }

            // add any missing, but existing entries
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(ObjectType));
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ObjectType asset = AssetDatabase.LoadAssetAtPath<ObjectType>(path);
                if (newList.Contains(asset)) 
                    continue;

                newList.Add(asset);
            }

            items.Clear();
            items.AddRange(newList);

            Debug.Log($"{typeof(ObjectType)}: {items.Count}");
        }
#endif
    }
}
