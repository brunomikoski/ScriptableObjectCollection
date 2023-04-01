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
        private LongGuid guid;
        public LongGuid GUID
        {
            get
            {
                if (guid.IsValid())
                    return guid;
                
                GenerateNewGUID();
                return guid;
            }
        }


        [NonSerialized]
        private List<ScriptableObject> editorSerializedItems = new List<ScriptableObject>();
        
        [SerializeField]
        protected List<ScriptableObject> items = new List<ScriptableObject>();
        public List<ScriptableObject> Items => items;

        [SerializeField]
        private bool automaticallyLoaded = true;
        internal bool AutomaticallyLoaded => automaticallyLoaded;

#pragma warning disable CS0414
        [SerializeField]
        private bool generateAsPartialClass = true;

        [SerializeField]
        private bool generateAsBaseClass;

        [SerializeField]
        private string generatedFileLocationPath;

        [SerializeField]
        private string generatedStaticClassFileName;

        [SerializeField]
        private string generateStaticFileNamespace;
#pragma warning restore CS0414
   
        public ScriptableObject this[int index]
        {
            get => items[index];
            set => throw new NotSupportedException();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<ScriptableObject> GetEnumerator()
        {
            using (IEnumerator<ScriptableObject> itemEnum = items.GetEnumerator())
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
            foreach (ScriptableObject e in this)
            {
                array.SetValue(e, index + i);
                ++i;
            }
        }
        
        public void CopyTo(List<ScriptableObject> list)
        {
            list.Capacity = Math.Max(list.Capacity, Count);
            foreach (ScriptableObject e in this)
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
            Add((ScriptableObject) value);
            return Count - 1;
        }

        public bool Add(ScriptableObject item)
        {
            ISOCItem socItem = item as ISOCItem;
            if (socItem == null)
                return false;
            if (items.Contains(item))
                return false;
            
            items.Add(item);

                socItem.SetCollection(this);
            
            ObjectUtility.SetDirty(this);
            ClearCachedValues();
            return true;
        }

        internal void GenerateNewGUID()
        {
            guid = LongGuid.NewGuid();
            ObjectUtility.SetDirty(this);
        }

#if UNITY_EDITOR
        public ScriptableObject AddNew(Type collectionType, string assetName = "")
        {
            if (Application.isPlaying)
                throw new NotSupportedException();
            
            ScriptableObject item = CreateInstance(collectionType);
            string assetPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
            string parentFolderPath = Path.Combine(assetPath, "Items" );
            AssetDatabaseUtils.CreatePathIfDoesntExist(parentFolderPath);

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
            return Contains((ScriptableObject) value);
        }

        public bool Contains(ScriptableObject item)
        {
            return items.Contains(item);
        }

        public int IndexOf(object value)
        {
            return IndexOf((ScriptableObject) value);
        }

        public int IndexOf(ScriptableObject item)
        {
            return items.IndexOf(item);
        }

        public void Insert(int index, ScriptableObject item)
        {
            items.Insert(index, item);
            if (item is ISOCItem socItem)
                socItem.SetCollection(this);
            
            ObjectUtility.SetDirty(this);
        }

        public void Insert(int index, object value)
        {
            Insert(index, (ScriptableObject)value);
        }

        public bool Remove(ScriptableObject item)
        {
            bool result =  items.Remove(item);
            ObjectUtility.SetDirty(this);
            return result;
        }

        public void Remove(object value)
        {
            Remove((ScriptableObject) value);
        }

        public void RemoveAt(int index)
        {
            items.RemoveAt(index);
            ObjectUtility.SetDirty(this);
        }

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (ScriptableObject) value;
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
                ScriptableObject scriptableObject = items[i];
                if (scriptableObject.IsNull() || scriptableObject == null)
                    items.RemoveAt(i);

                ISOCItem socItem = scriptableObject as ISOCItem;
                if (socItem == null)
                {
                    items.RemoveAt(i);
                    continue;
                }

                if (socItem.Collection.IsNull() || socItem.Collection == null)
                    items.RemoveAt(i);
            }
            
            ObjectUtility.SetDirty(this);
        }
        
        [ContextMenu("Refresh Collection")]
        public void RefreshCollection()
        {
#if UNITY_EDITOR
            
            Type collectionType = GetItemType();
            if (collectionType == null)
                return;

            bool changed = false;
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
            string[] guids = AssetDatabase.FindAssets($"t:{collectionType.Name}", new []{folder});

            for (int i = 0; i < guids.Length; i++)
            {
                ScriptableObject item =
                    AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
                
                if (item == null)
                    continue;

                if (!(item is ISOCItem socItem))
                    continue;
                
                if (socItem.Collection != this)
                    continue;

                if (socItem.Collection.Contains(item))
                    continue;
                
                Debug.Log($"Adding {item.name} to the Collection {this.name} its inside of the folder {folder}");
                Add(item);
                changed = true;
            }

            for (int i = items.Count - 1; i >= 0; i--)
            {
                ScriptableObject scriptableObject = items[i];

                if (scriptableObject is ISOCItem)
                    continue;
                
                RemoveAt(i);
                changed = true;
            }

            if (changed)
                ObjectUtility.SetDirty(this);
#endif
        }

        public bool TryGetItemByName(string targetItemName, out ScriptableObject scriptableObjectCollectionItem)
        {
            for (int i = 0; i < items.Count; i++)
            {
                ScriptableObject item = items[i];
                if (string.Equals(item.name, targetItemName, StringComparison.Ordinal))
                {
                    scriptableObjectCollectionItem = item;
                    return true;
                }
            }

            scriptableObjectCollectionItem = null;
            return false;
        }

        public bool TryGetItemByGUID(LongGuid itemGUID, out ScriptableObject scriptableObjectCollectionItem)
        {
            for (int i = 0; i < items.Count; i++)
            {
                ScriptableObject item = items[i];
                ISOCItem socItem = item as ISOCItem;
                if (socItem == null)
                    continue;
                
                if (socItem.GUID == itemGUID)
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
            editorSerializedItems.Clear();
            editorSerializedItems.AddRange(items);
        }

        internal void PrepareForEditorMode()
        {
            items.Clear();
            items.AddRange(editorSerializedItems);
            editorSerializedItems.Clear();
        }

        protected virtual void ClearCachedValues()
        {
        }
    }

    public class ScriptableObjectCollection<ObjectType> : ScriptableObjectCollection, IList<ObjectType>
        where ObjectType : ScriptableObject, ISOCItem
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
            using (IEnumerator<ScriptableObject> itemEnum = base.GetEnumerator())
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

        [Obsolete("GetItemByGUID(string targetGUID) is obsolete, please regenerate your static class")]
        public ObjectType GetItemByGUID(string targetGUID)
        {
            throw new Exception(
                $"GetItemByGUID(string targetGUID) is obsolete, please regenerate your static class");
        }

        public ObjectType GetItemByGUID(LongGuid targetGUID)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                ScriptableObject item = Items[i];
                ISOCItem socItem = item as ISOCItem;
                if (socItem == null)
                    continue;

                if (socItem.GUID == targetGUID)
                    return (ObjectType) item;
            }

            return null;
        }

        public void Add(ObjectType item)
        {
            base.Add(item);
            ClearCachedValues();
        }

        public int Add(Type itemType = null)
        {
            int count = base.Add(itemType);
            ClearCachedValues();
            return count;
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
            using (IEnumerator<ScriptableObject> enumerator = base.GetEnumerator())
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
    }
}
