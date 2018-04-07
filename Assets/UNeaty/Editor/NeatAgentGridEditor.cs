#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UNeaty
{
    [CustomEditor(typeof(NeatAgentGrid))]
    public class NeatAgentGridEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Update Grid"))
                ((NeatAgentGrid)target).UpdateGrid();
        }
    }
}
#endif