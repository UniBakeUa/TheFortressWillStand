using UnityEngine;

namespace Towers.ScriptableObjects
{
    public class ProductConfig : ScriptableObject
    {
        [field: SerializeField] public string StructureName { get; protected set; }
        [field: SerializeField] public int Id { get; protected set; }
        [field: SerializeField] public int BaseCost { get; protected set; }
    }
}
