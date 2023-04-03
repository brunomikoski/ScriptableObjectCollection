using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public class ItemSet<ItemType> where ItemType : ScriptableObject, ISOCItem
    {
        [SerializeField]
        private List<CollectionItemIndirectReference<ItemType>> items =
            new List<CollectionItemIndirectReference<ItemType>>();

    }
}
