using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;
using System.Collections.Generic;

namespace BrunoMikoski.Templates
{
    [CreateAssetMenu(menuName = "ScriptableObject Collection/Collections/Create CarIDCollection", fileName = "CarIDCollection", order = 0)]
    public class CarIDCollection : ScriptableObjectCollection<CarID>
    {
    }
}
