using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UNeaty
{
    public class NeatAgentGrid : MonoBehaviour
    {
        [SerializeField] string PathToNeatAgent;

        [SerializeField] GameObject NeatAgentPrefab;
        [SerializeField] NeatAcademy Academy;

        [SerializeField] Vector3Int PrefabSize = Vector3Int.one;

        [SerializeField] Vector3Int PrefabCount = Vector3Int.one;

        [SerializeField] Vector3 Padding = Vector3.one;

        public void UpdateGrid()
        {
            DestroyAllChildren(transform);

            float CenterSizeX = ((PrefabCount.x - 1) * (PrefabSize.x + Padding.x)) / 2;
            float CenterSizeY = ((PrefabCount.y - 1) * (PrefabSize.y + Padding.y)) / 2;
            float CenterSizeZ = ((PrefabCount.z - 1) * (PrefabSize.z + Padding.z)) / 2;

            for (int x = 0; x < PrefabCount.x; x++)
            {
                for (int y = 0; y < PrefabCount.y; y++)
                {
                    for (int z = 0; z < PrefabCount.z; z++)
                    {
                        Transform aSlot = Instantiate(NeatAgentPrefab).transform;

                        aSlot.parent = transform;

                        aSlot.localPosition = new Vector3((PrefabSize.x + Padding.x) * x - CenterSizeX,
                                                          (PrefabSize.y + Padding.y) * y - CenterSizeY,
                                                         -(PrefabSize.z + Padding.z) * z - CenterSizeZ);

                        if (PathToNeatAgent == "")
                            aSlot.GetComponent<NeatAgent>().TheNeatAcademy = Academy;
                        else
                            aSlot.Find(PathToNeatAgent).GetComponent<NeatAgent>().TheNeatAcademy = Academy;
                    }
                }
            }
        }

        void DestroyAllChildren(Transform parent)
        {
            while (parent.childCount > 0)
            {
                Transform child = parent.GetChild(0);
                DestroyAllChildren(child);
                DestroyImmediate(child.gameObject);
            }
        }
    }
}