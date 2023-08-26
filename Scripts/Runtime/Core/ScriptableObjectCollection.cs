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
            if (item is not ISOCItem socItem)
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
        public ScriptableObject AddNew(Type itemType, string assetName = "")
        {
            if (Application.isPlaying)
                throw new NotSupportedException();

            if (!typeof(ISOCItem).IsAssignableFrom(itemType))
                throw new Exception($"{itemType} does not implement {nameof(ISOCItem)}");
            
            ScriptableObject newItem = CreateInstance(itemType);
            string assetPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
            string parentFolderPath = Path.Combine(assetPath, "Items" );
            AssetDatabaseUtils.CreatePathIfDoesntExist(parentFolderPath);

            string itemName = assetName;

            if (string.IsNullOrEmpty(itemName))
            {
                int count = Count;
                while (true)
                {
                    itemName = $"New{itemType.Name}{count}";
                    string testPath = Path.Combine(parentFolderPath, itemName);

                    if (!File.Exists(Path.GetFullPath($"{testPath}.asset")))
                        break;
                
                    count++;
                }
            }
            
            newItem.name = itemName;

            if(itemName.IsReservedKeyword())
                Debug.LogError($"{itemName} is a reserved keyword name, will cause issues with code generation, please rename it");

            string newFileName = Path.Combine(parentFolderPath, newItem.name + ".asset");

            ISOCItem socItem = newItem as ISOCItem;
            socItem.GenerateNewGUID();
            
            this.Add(newItem);

            AssetDatabase.CreateAsset(newItem, newFileName);
            ObjectUtility.SetDirty(this);

            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.ApplyModifiedProperties();

            return newItem;
        }
        
        public ISOCItem AddNewBaseItem(string targetName)
        {
            return AddNew(GetItemType(), targetName) as ISOCItem;
        }
        
        public ISOCItem GetOrAddNewBaseItem(string targetName)
        {
            ISOCItem item = Items.FirstOrDefault(o => o.name.Equals(targetName, StringComparison.Ordinal)) as ISOCItem;
            if (item != null)
                return item;

            return AddNewBaseItem(targetName);
        }
#endif

        public virtual Type GetItemType()
        {
            Type itemType = GetGenericItemType();
            return itemType?.GetGenericArguments().First();
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

        public void OrderByName()
        {
            items = items.OrderBy(o => o.name).ToList();
            ObjectUtility.SetDirty(this);
        }

        public void Sort(IComparer<ScriptableObject> comparer)
        {
            items.Sort(comparer);
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

        public bool Remove(LongGuid targetGuid)
        {
            if (TryGetItemByGUID(targetGuid, out ScriptableObject item))
            {
                return Remove(item);
            }

            return false;
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
        
        [ContextMenu("Refresh Collection")]
        public void RefreshCollection()
        {
#if UNITY_EDITOR
            Type collectionItemType = GetItemType();
            if (collectionItemType == null)
                return;

            bool changed = false;
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(assetPath))
                return;
            
            string folder = Path.GetDirectoryName(assetPath);
            string[] guids = AssetDatabase.FindAssets($"t:{collectionItemType.Name}", new []{folder});

            for (int i = 0; i < guids.Length; i++)
            {
                ScriptableObject item =
                    AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
                
                if (item == null)
                    continue;

                if (item is not ISOCItem socItem)
                    continue;

                

                if (socItem.Collection != null)
                {
                    if (socItem.Collection != this)
                        continue;

                    if (socItem.Collection.Contains(item))
                        continue;
                }

                if (Add(item))
                    changed = true;
            }

            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i] == null)
                {
                    RemoveAt(i);
                    Debug.Log($"Removing item at index {i} as it is null");
                    changed = true;

                }
                
                ScriptableObject scriptableObject = items[i];
                if (scriptableObject.GetType() == GetItemType() || scriptableObject.GetType().IsSubclassOf(GetItemType()))
                    continue;
                
                RemoveAt(i);
                Debug.Log($"Removing item at index {i} {scriptableObject} since it is not of type {GetItemType()}");
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

        public bool TryGetItemByGUID<T>(LongGuid itemGUID, out T scriptableObjectCollectionItem)
            where T : ScriptableObject
        {
            if (itemGUID.IsValid())
            {
                for (int i = 0; i < items.Count; i++)
                {
                    ScriptableObject item = items[i];
                    ISOCItem socItem = item as ISOCItem;
                    if (socItem == null)
                        continue;
                
                    if (socItem.GUID == itemGUID)
                    {
                        scriptableObjectCollectionItem = item as T;
                        return scriptableObjectCollectionItem != null;
                    }
                }
            }

            scriptableObjectCollectionItem = null;
            return false;
        }
        public bool TryGetItemByGUID(LongGuid itemGUID, out ScriptableObject scriptableObjectCollectionItem)
        {
            return TryGetItemByGUID<ScriptableObject>(itemGUID, out scriptableObjectCollectionItem);
        }

        protected virtual void ClearCachedValues()
        {
        }
    }

    public class ScriptableObjectCollection<TObjectType> : ScriptableObjectCollection, IList<TObjectType>
        where TObjectType : ScriptableObject, ISOCItem
    {
        private static List<TObjectType> cachedValues;
        public static IReadOnlyList<TObjectType> Values
        {
            get
            {
                if (cachedValues == null)
                    cachedValues = CollectionsRegistry.Instance.GetAllCollectionItemsOfType<TObjectType>();
                return cachedValues;
            }
        }

        public new TObjectType this[int index]
        {
            get => (TObjectType)base[index];
            set => base[index] = value;
        }


        public new IEnumerator<TObjectType> GetEnumerator()
        {
            using (IEnumerator<ScriptableObject> itemEnum = base.GetEnumerator())
            {
                while (itemEnum.MoveNext())
                {
                    if (itemEnum.Current.IsNull())
                        continue;
                    TObjectType obj = itemEnum.Current as TObjectType;
                    if (obj == null)
                        continue;
                    yield return obj;
                }
            }
        }

#if UNITY_EDITOR

        public T GetOrAddNew<T>(string targetName = null) where T : TObjectType
        {
            if (!string.IsNullOrEmpty(targetName))
            {
                T item = Items.FirstOrDefault(o => o.name.Equals(targetName, StringComparison.Ordinal)) as T;
                if (item != null)
                    return item;
            }

            return (T) AddNew(typeof(T), targetName);
        }
        
        public TObjectType GetOrAddNew(Type collectionType, string targetName)
        {
            TObjectType item = Items.FirstOrDefault(o => o.name.Equals(targetName, StringComparison.Ordinal)) as TObjectType;
            if (item != null)
                return item;

            return (TObjectType) AddNew(collectionType, targetName);
        }
        
        public TObjectType GetOrAddNew(string targetName)
        {
            TObjectType item = Items.FirstOrDefault(o => o.name.Equals(targetName, StringComparison.Ordinal)) as TObjectType;
            if (item != null)
                return item;

            return AddNew(targetName);
        }
        
        public TObjectType AddNew(string targetName)
        {
            return (TObjectType) AddNew(GetItemType(), targetName);
        } 
        
        public TObjectType AddNew() 
        {
            return (TObjectType)AddNew(GetItemType());
        } 
#endif

        [Obsolete("GetItemByGUID(string targetGUID) is obsolete, please regenerate your static class")]
        public TObjectType GetItemByGUID(string targetGUID)
        {
            throw new Exception(
                $"GetItemByGUID(string targetGUID) is obsolete, please regenerate your static class");
        }

        public TObjectType GetItemByGUID(LongGuid targetGUID)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                ScriptableObject item = Items[i];
                ISOCItem socItem = item as ISOCItem;
                if (socItem == null)
                    continue;

                if (socItem.GUID == targetGUID)
                    return (TObjectType) item;
            }

            return null;
        }

        public bool TryGetItemByName<T>(string targetItemName, out T scriptableObjectCollectionItem) where T : TObjectType
        {
            for (int i = 0; i < items.Count; i++)
            {
                ScriptableObject item = items[i];
                if (string.Equals(item.name, targetItemName, StringComparison.Ordinal))
                {
                    scriptableObjectCollectionItem = item as T;
                    return scriptableObjectCollectionItem != null;
                }
            }

            scriptableObjectCollectionItem = null;
            return false;
        }

        public void Add(TObjectType item)
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

        public bool Contains(TObjectType item)
        {
            return base.Contains(item);
        }

        public void CopyTo(TObjectType[] array, int arrayIndex)
        {
            base.CopyTo(array, arrayIndex);
        }

        public int IndexOf(TObjectType item)
        {
            return base.IndexOf(item);
        }

        public void Insert(int index, TObjectType item)
        {
            base.Insert(index, item);
            ClearCachedValues();
        }

        public bool Remove(TObjectType item)
        {
            bool remove = base.Remove(item);
            ClearCachedValues();
            return remove;
        }
        
        
        IEnumerator<TObjectType> IEnumerable<TObjectType>.GetEnumerator()
        {
            using (IEnumerator<ScriptableObject> enumerator = base.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    yield return (TObjectType)enumerator.Current;
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
