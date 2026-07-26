using UnityEditor;
using UnityEngine;

namespace Towers.Buildings.Editor
{
    // Копіює всі серіалізовані поля (успадковані від TurretBase/BaseBuilding) зі старого
    // компонента на новий, зберігаючи прив'язки в інспекторі (Turret Visual, Muzzle,
    // Bullet Trail, Range Circle тощо), додає новий компонент на той самий GameObject
    // і видаляє старий.
    internal static class TurretConversionUtility
    {
        public static void ConvertTo<TFrom, TTo>(TFrom from)
            where TFrom : TurretBase
            where TTo : TurretBase
        {
            GameObject go = from.gameObject;

            var to = go.AddComponent<TTo>();

            SerializedObject fromSerialized = new SerializedObject(from);
            SerializedObject toSerialized = new SerializedObject(to);

            SerializedProperty prop = fromSerialized.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.propertyPath == "m_Script") continue;

                SerializedProperty targetProp = toSerialized.FindProperty(prop.propertyPath);
                if (targetProp != null)
                {
                    toSerialized.CopyFromSerializedProperty(prop);
                }
            }

            toSerialized.ApplyModifiedProperties();

            Undo.DestroyObjectImmediate(from);
            EditorUtility.SetDirty(go);
        }
    }

    [CustomEditor(typeof(GroundTurret))]
    public class GroundTurretEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            if (GUILayout.Button("Convert to AA Turret"))
            {
                var turret = (GroundTurret)target;
                TurretConversionUtility.ConvertTo<GroundTurret, AATurret>(turret);
            }
        }
    }

    [CustomEditor(typeof(AATurret))]
    public class AATurretEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            if (GUILayout.Button("Convert to Ground Turret"))
            {
                var turret = (AATurret)target;
                TurretConversionUtility.ConvertTo<AATurret, GroundTurret>(turret);
            }
        }
    }
}
