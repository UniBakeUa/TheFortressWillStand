using System.IO;
using Towers.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Towers.Buildings.Editor
{
    /// <summary>
    /// Створює стартовий набір карток прокачки і бібліотеку до них.
    ///
    /// Асети робимо кодом, а не руками, щоб усі шість карток мали узгоджені ID,
    /// ваги й описи. Повторний запуск не дублює: наявні асети оновлюються.
    /// Спрайти-іконки треба проставити вручну в інспекторі.
    /// </summary>
    public static class PerkAssetCreator
    {
        private const string PerksFolder = "Assets/Project/Configs/Perks";
        private const string LibraryPath = "Assets/Project/Configs/PerkLibrary.asset";

        [MenuItem("Tools/Perks/Create Default Perk Assets")]
        public static void CreateDefaultPerks()
        {
            EnsureFolder(PerksFolder);

            PerkConfig[] perks =
            {
                CreatePerk("Perk_AntiAirDamage", 0, "Зенітний калібр",
                    "ПВО б'є на +{0:0} дамагу",
                    PerkEffectType.AntiAirDamage, amount: 1f, secondary: 0f,
                    copies: 4, requiredWave: 0,
                    tint: new Color(0.55f, 0.75f, 1f)),

                CreatePerk("Perk_GroundTurretRange", 1, "Далекобійність",
                    "Звичайні турелі стріляють на +{0:P0} далі",
                    PerkEffectType.GroundTurretRange, amount: 0.5f, secondary: 0f,
                    copies: 3, requiredWave: 0,
                    tint: new Color(0.6f, 1f, 0.6f)),

                CreatePerk("Perk_AutoRepairBuildings", 2, "Ремонтна бригада",
                    "+{0:0} HP усім будівлям (крім фортеці) раз на {1:0} с",
                    PerkEffectType.AutoRepairBuildings, amount: 1f, secondary: 5f,
                    copies: 2, requiredWave: 0,
                    tint: new Color(1f, 0.85f, 0.4f)),

                CreatePerk("Perk_AutoRepairFortress", 3, "Гарнізон фортеці",
                    "+{0:0} HP фортеці раз на {1:0} с",
                    PerkEffectType.AutoRepairFortress, amount: 1f, secondary: 10f,
                    copies: 2, requiredWave: 0,
                    tint: new Color(1f, 0.7f, 0.3f)),

                CreatePerk("Perk_FingerStrike", 4, "Німцеріз",
                    "Раз на {1:0} с сам б'є по випадковому ворогу, ніби пальцем",
                    PerkEffectType.AutoFingerStrike, amount: 1f, secondary: 5f,
                    copies: 1, requiredWave: 0,
                    tint: new Color(1f, 0.45f, 0.45f)),

                CreatePerk("Perk_RemoveCollaborator", 5, "Прибрати колаборанта",
                    "Одразу +{0:0} пончики",
                    PerkEffectType.InstantPonchics, amount: 3f, secondary: 0f,
                    copies: 5, requiredWave: 0,
                    tint: new Color(0.9f, 0.6f, 1f)),
            };

            CreateOrUpdateLibrary(perks);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PerkAssetCreator] Готово: {perks.Length} карток у {PerksFolder}, бібліотека - {LibraryPath}. " +
                      "Іконки (Icon) треба проставити вручну.");
        }

        private static PerkConfig CreatePerk(string fileName, int id, string displayName, string description,
            PerkEffectType effectType, float amount, float secondary,
            int copies, int requiredWave, Color tint)
        {
            string path = $"{PerksFolder}/{fileName}.asset";

            PerkConfig perk = AssetDatabase.LoadAssetAtPath<PerkConfig>(path);
            bool isNew = perk == null;

            if (isNew)
            {
                perk = ScriptableObject.CreateInstance<PerkConfig>();
            }

            // Поля приватні через [field: SerializeField], тож пишемо їх через
            // SerializedObject - це єдиний коректний шлях з едітора.
            SerializedObject so = new SerializedObject(perk);
            SetInt(so, "<Id>k__BackingField", id);
            SetString(so, "<DisplayName>k__BackingField", displayName);
            SetString(so, "<Description>k__BackingField", description);
            SetEnum(so, "<EffectType>k__BackingField", (int)effectType);
            SetFloat(so, "<Amount>k__BackingField", amount);
            SetFloat(so, "<SecondaryAmount>k__BackingField", secondary);
            SetInt(so, "<CopiesInDeck>k__BackingField", copies);
            SetInt(so, "<RequiredWave>k__BackingField", requiredWave);
            SetColor(so, "<Tint>k__BackingField", tint);
            so.ApplyModifiedPropertiesWithoutUndo();

            if (isNew)
            {
                AssetDatabase.CreateAsset(perk, path);
            }
            else
            {
                EditorUtility.SetDirty(perk);
            }

            return perk;
        }

        private static void CreateOrUpdateLibrary(PerkConfig[] perks)
        {
            PerkLibrary library = AssetDatabase.LoadAssetAtPath<PerkLibrary>(LibraryPath);
            bool isNew = library == null;

            if (isNew)
            {
                library = ScriptableObject.CreateInstance<PerkLibrary>();
            }

            SerializedObject so = new SerializedObject(library);
            SerializedProperty list = so.FindProperty("_perks");
            if (list != null)
            {
                list.ClearArray();
                for (int i = 0; i < perks.Length; i++)
                {
                    list.InsertArrayElementAtIndex(i);
                    list.GetArrayElementAtIndex(i).objectReferenceValue = perks[i];
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            if (isNew)
            {
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            else
            {
                EditorUtility.SetDirty(library);
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = Path.GetFileName(folder);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void SetInt(SerializedObject so, string path, int value)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null) p.intValue = value;
        }

        private static void SetFloat(SerializedObject so, string path, float value)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null) p.floatValue = value;
        }

        private static void SetString(SerializedObject so, string path, string value)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null) p.stringValue = value;
        }

        private static void SetEnum(SerializedObject so, string path, int value)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null) p.enumValueIndex = value;
        }

        private static void SetColor(SerializedObject so, string path, Color value)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null) p.colorValue = value;
        }
    }
}
