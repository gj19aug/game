using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
public class IntRangeDrawer : PropertyDrawer
{
    //public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    //{
    //    return EditorGUIUtility.singleLineHeight;
    //}

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MinMaxRangeAttribute attr = (MinMaxRangeAttribute) attribute;
        SerializedProperty minProp = property.FindPropertyRelative("min");
        SerializedProperty maxProp = property.FindPropertyRelative("max");

        label = EditorGUI.BeginProperty(position, label, property);
        float min = (float) minProp.intValue;
        float max = (float) maxProp.intValue;
        EditorGUI.MinMaxSlider(position, label, ref min, ref max, attr.min, attr.max);
        minProp.intValue = (int) min;
        maxProp.intValue = (int) max;
        EditorGUI.EndProperty();
    }
}
