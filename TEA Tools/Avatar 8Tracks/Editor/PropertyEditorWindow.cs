using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PropertyEditorWindow : EditorWindow
{
		public SerializedObject serializedObject;
		public SerializedProperty currentProperty;

		public static PropertyEditorWindow Open(AudioSource audioSource)
		{
				PropertyEditorWindow window = GetWindow<PropertyEditorWindow>("Audio Setup");
				window.serializedObject = new SerializedObject(audioSource);
				return window;
		}

		public void OnGUI()
		{
				currentProperty = serializedObject.GetIterator();
				DrawProperties(currentProperty, true);
		}

		public void DrawProperties(SerializedProperty prop, bool drawChildren)
		{
				string lastPropPath = string.Empty;
				foreach (SerializedProperty p in prop)
				{
						if (p.isArray && p.propertyType == SerializedPropertyType.Generic)
						{
								EditorGUILayout.BeginHorizontal();
								p.isExpanded = EditorGUILayout.Foldout(p.isExpanded, p.displayName);
								EditorGUILayout.BeginHorizontal();
								if (p.isExpanded)
								{
										EditorGUI.indentLevel++;
										DrawProperties(p, drawChildren);
										EditorGUI.indentLevel--;
								}
						} else if (!string.IsNullOrEmpty(lastPropPath) && p.propertyPath.Contains(lastPropPath))
						{
								continue;
						} else
						{
								lastPropPath = p.propertyPath;
								EditorGUILayout.PropertyField(p, drawChildren);
						}
				}
		}
}
