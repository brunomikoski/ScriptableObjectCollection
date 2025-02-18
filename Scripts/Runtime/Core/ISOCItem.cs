using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public interface ISOCItem
    {
        LongGuid GUID { get; }
        ScriptableObjectCollection Collection { get; }
        string name { get; set; }
        void SetCollection(ScriptableObjectCollection collection);
        void GenerateNewGUID();
        void ClearCollection();
    }

    public interface ISOCColorizedItem
    {
        Color LabelColor { get;}
    }
}