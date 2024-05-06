using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;

namespace BrunoMikoski.Templates
{
    public partial class CarID : ScriptableObjectCollectionItem
    {
        [SerializeField]
        private GameObject carPrefab;
        public GameObject CarPrefab => carPrefab;
    }
}
