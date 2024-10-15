//using System.Collections;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;

////：
//public class AABB : MonoBehaviour
//{
//    // Start is called before the first frame update
//    void Start()
//    {

//    }

//    // Update is called once per frame
//    void Update()
//    {

//    }

//    //[MenuItem("MyMenu/Do Test")]
//    static void Test()
//    {
//        Transform parent = Selection.activeGameObject.transform;
//        Vector3 postion = parent.position;
//        Quaternion rotation = parent.rotation;
//        Vector3 scale = parent.localScale;
//        parent.position = Vector3.zero;
//        parent.rotation = Quaternion.Euler(Vector3.zero);
//        parent.localScale = Vector3.one;

//        Collider[] colliders = parent.GetComponentsInChildren<Collider>();
//        foreach (Collider child in colliders)
//        {
//            DestroyImmediate(child);
//        }
//        Vector3 center = Vector3.zero;
//        Renderer[] renders = parent.GetComponentsInChildren<Renderer>();
//        foreach (Renderer child in renders)
//        {
//            center += child.bounds.center;
//        }
//        center /= parent.GetComponentsInChildren<Renderer>().Length;
//        Bounds bounds = new Bounds(center, Vector3.zero);
//        foreach (Renderer child in renders)
//        {
//            bounds.Encapsulate(child.bounds);
//        }
//        BoxCollider boxCollider = parent.gameObject.AddComponent<BoxCollider>();
//        boxCollider.center = bounds.center - parent.position;
//        boxCollider.size = bounds.size;

//        parent.position = postion;
//        parent.rotation = rotation;
//        parent.localScale = scale;
//    }
//}
