#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TerrainTreesToObjects : EditorWindow
{
    [MenuItem("Tools/Convert Terrain Trees to Objects")]
    public static void ConvertTrees()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Активный Terrain не найден на сцене!", "OK");
            return;
        }

        TerrainData data = terrain.terrainData;
        if (data.treeInstances == null || data.treeInstances.Length == 0)
        {
            EditorUtility.DisplayDialog("Внимание", "На выбранном Terrain нет нарисованных деревьев!", "OK");
            return;
        }

        // Проверяем наличие слоя Building
        int buildingLayer = LayerMask.NameToLayer("Building");
        if (buildingLayer == -1)
        {
            Debug.LogWarning("[Конвертер] Слой 'Building' не найден! Убедитесь, что создали его в Tags & Layers.");
        }

        // Включаем GPU Instancing на материалах прототипов деревьев
        HashSet<Material> processedMaterials = new HashSet<Material>();
        foreach (TreePrototype proto in data.treePrototypes)
        {
            if (proto.prefab != null)
            {
                Renderer[] renderers = proto.prefab.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    foreach (Material mat in r.sharedMaterials)
                    {
                        if (mat != null && !processedMaterials.Contains(mat))
                        {
                            mat.enableInstancing = true;
                            EditorUtility.SetDirty(mat);
                            processedMaterials.Add(mat);
                            Debug.Log($"[Конвертер] Включен GPU Instancing для материала: {mat.name}");
                        }
                    }
                }
            }
        }

        // Создаем родительский контейнер на сцене
        GameObject container = new GameObject("Trees_Container");
        Undo.RegisterCreatedObjectUndo(container, "Convert Terrain Trees");

        int count = 0;
        foreach (TreeInstance tree in data.treeInstances)
        {
            TreePrototype proto = data.treePrototypes[tree.prototypeIndex];
            if (proto.prefab == null) continue;

            Vector3 worldPos = Vector3.Scale(tree.position, data.size) + terrain.transform.position;
            Quaternion worldRot = Quaternion.Euler(0f, tree.rotation * Mathf.Rad2Deg, 0f);
            Vector3 worldScale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(proto.prefab);
            instance.transform.position = worldPos;
            instance.transform.rotation = worldRot;
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, worldScale);
            instance.transform.SetParent(container.transform);

            // Назначаем слой Building рекурсивно на объект и дочерние элементы
            if (buildingLayer != -1)
            {
                SetLayerRecursively(instance, buildingLayer);
            }

            count++;
        }

        // Сохраняем Undo для самого террейна и очищаем его от запеченных копий
        Undo.RegisterCompleteObjectUndo(data, "Clear Terrain Trees");
        data.treeInstances = new TreeInstance[0];
        terrain.Flush();

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Успех", $"Успешно сконвертировано {count} деревьев!\nGPU Instancing применен к материалам.", "OK");
    }

    private static void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}
#endif
