using Building;
using Towers;
using UnityEngine;

namespace UI
{
    public class SelectBuildStructure : MonoBehaviour
    {
        [SerializeField] public BuildManager buildManager;
        public void SelectStructure(int id)
        {
            buildManager.SelectStructure(id);
        }
    }
}