using UnityEngine;

namespace Towers.ScriptableObjects
{
    //Це конфіг основної будівлі, знищення якої має приводити до кінця гри. Тому цей конфіг не додавати в BuildingLibrary
    [CreateAssetMenu(fileName = "NewFortressData", menuName = "Building/Fortress Config")]
    public class FortressConfig : BuildingConfig { }
}
