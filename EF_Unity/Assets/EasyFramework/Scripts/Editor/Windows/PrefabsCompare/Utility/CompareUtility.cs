using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace PrefabsCompare
    {
        public class CompareUtility
        {
            /// <summary>
            /// ID计数器
            /// </summary>
            static int idCounter;

            /// <summary>
            /// 组件数组（重复利用）
            /// </summary>
            static readonly List<Component> _leftComponentList = new List<Component>();

            /// <summary>
            /// 组件数组（重复利用）
            /// </summary>
            static readonly List<Component> _rightComponentList = new List<Component>();

            /// <summary>
            /// 对比两个Prefab
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <returns></returns>
            public static GameObjectCompareInfo ComparePrefab(GameObject left, GameObject right)
            {
                if (left == null || right == null)
                {
                    return new GameObjectCompareInfo("", 0, 0);
                }

                return CompareGameObject(left, right, "Root", 0, MissType.allExist);
            }

            /// <summary>
            /// 对比两个GameObject
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <param name="name"></param>
            /// <param name="depth"></param>
            /// <param name="missType">（左右对象）丢失状态</param>
            /// <param name="fileID"></param>
            /// <returns></returns>
            private static GameObjectCompareInfo CompareGameObject(GameObject left, GameObject right, string name, int depth, MissType missType)
            {
                GameObjectCompareInfo info = new GameObjectCompareInfo(name, depth, ++idCounter)
                {
                    MissType = missType,
                    LeftGameObject = left,
                    RightGameObject = right,
                };

                if (missType == MissType.allExist)
                {
                    CompareGameObject(left, right, ref info);
                }

                return info;
            }

            /// <summary>
            /// 对比两个GameObject
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <param name="info"></param>
            private static void CompareGameObject(GameObject left, GameObject right, ref GameObjectCompareInfo info)
            {
                if (left.activeSelf == right.activeSelf)
                {
                    info.GameObjectCompare |= GameObjectCompareType.activeEqual;
                }

                if (left.CompareTag(right.tag))
                {
                    info.GameObjectCompare |= GameObjectCompareType.tagEqual;
                }

                if (left.layer == right.layer)
                {
                    info.GameObjectCompare |= GameObjectCompareType.layerEqual;
                }

                CompareChild(left, right, info);

                if (info.MissType == MissType.allExist)
                {
                    CompareComponent(left, right, ref info);
                }
            }

            /// <summary>
            /// 对比子对象
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <param name="info"></param>
            private static void CompareChild(GameObject left, GameObject right, GameObjectCompareInfo info)
            {
                var leftChildCount = left.transform.childCount;
                var rightChildCount = right.transform.childCount;

                int leftIndex = 0;
                int rightIndex = 0;

                bool childCountEqual = true;
                bool childContentEqual = true;

                if (leftChildCount != rightChildCount)
                {
                    childCountEqual = false;
                }

                while (leftIndex < leftChildCount || rightIndex < rightChildCount)
                {
                    if (leftIndex >= leftChildCount)
                    {
                        for (int i = rightIndex; i < rightChildCount; i++)
                        {
                            var rightChild = right.transform.GetChild(i);

                            var childInfo = AddChildInfo(info, null, rightChild.gameObject, rightChild.name, MissType.missLeft);

                            if (!childInfo.AllEqual())
                            {
                                childContentEqual = false;
                            }
                        }

                        break;
                    }
                    else if (rightIndex >= rightChildCount)
                    {
                        for (int i = leftIndex; i < leftChildCount; i++)
                        {
                            var leftChild = left.transform.GetChild(i);

                            var childInfo = AddChildInfo(info, leftChild.gameObject, null, leftChild.name, MissType.missRight);

                            if (!childInfo.AllEqual())
                            {
                                childContentEqual = false;
                            }
                        }

                        break;
                    }
                    else
                    {
                        var leftChild = left.transform.GetChild(leftIndex);

                        var index = -1;

                        for (int i = rightIndex; i < rightChildCount; i++)
                        {
                            var rightChild = right.transform.GetChild(i);

                            if (leftChild.name == rightChild.name)
                            {
                                index = i;
                                break;
                            }
                        }

                        if (index == -1)
                        {
                            var childInfo = AddChildInfo(info, leftChild.gameObject, null, leftChild.name, MissType.missRight);

                            if (!childInfo.AllEqual())
                            {
                                childContentEqual = false;
                            }

                            leftIndex++;
                        }
                        else
                        {
                            Transform rightChild = null;
                            GameObjectCompareInfo childInfo = null;

                            for (int i = rightIndex; i < index; i++)
                            {
                                rightChild = right.transform.GetChild(i);

                                childInfo = AddChildInfo(info, null, rightChild.gameObject, rightChild.name, MissType.missLeft);

                                if (!childInfo.AllEqual())
                                {
                                    childContentEqual = false;
                                }
                            }

                            rightChild = right.transform.GetChild(index);

                            childInfo = AddChildInfo(info, leftChild.gameObject, rightChild.gameObject, leftChild.name, MissType.allExist);

                            if (!childInfo.AllEqual())
                            {
                                childContentEqual = false;
                            }

                            leftIndex++;
                            rightIndex = index + 1;
                        }
                    }
                }

                if (childCountEqual)
                {
                    info.GameObjectCompare |= GameObjectCompareType.childCountEqual;
                }

                if (childContentEqual)
                {
                    info.GameObjectCompare |= GameObjectCompareType.childContentEqual;
                }
            }

            /// <summary>
            /// 添加子对象信息
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <param name="name"></param>
            /// <param name="missType"></param>
            /// <returns></returns>
            private static GameObjectCompareInfo AddChildInfo(GameObjectCompareInfo parent, GameObject left, GameObject right, string name, MissType missType)
            {
                var childInfo = CompareGameObject(left, right, name, parent.Depth + 1, missType);

                childInfo.Parent = parent;

                parent.Children.Add(childInfo);

                return childInfo;
            }

            /// <summary>
            /// 对比组件
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <param name="info"></param>
            private static void CompareComponent(GameObject left, GameObject right, ref GameObjectCompareInfo info)
            {
                left.GetComponents(_leftComponentList);
                right.GetComponents(_rightComponentList);

                var leftCount = _leftComponentList.Count;
                var rightCount = _rightComponentList.Count;

                int leftIndex = 0;
                int rightIndex = 0;

                bool componentCountEqual = true;
                bool componentContentEqual = true;

                if (leftCount != rightCount)
                {
                    componentCountEqual = false;
                }

                while (leftIndex < leftCount || rightIndex < rightCount)
                {
                    if (leftIndex >= leftCount)
                    {
                        for (int i = rightIndex; i < rightCount; i++)
                        {
                            var rightComponent = _rightComponentList[i];

                            var childInfo = AddComponentInfo(info, null, rightComponent, rightComponent.GetType().FullName, MissType.missLeft);

                            if (!childInfo.AllEqual())
                            {
                                componentContentEqual = false;
                            }
                        }

                        break;
                    }
                    else if (rightIndex >= rightCount)
                    {
                        for (int i = leftIndex; i < leftCount; i++)
                        {
                            var leftComponent = _leftComponentList[i];

                            var childInfo = AddComponentInfo(info, leftComponent, null, leftComponent.GetType().FullName, MissType.missRight);

                            if (!childInfo.AllEqual())
                            {
                                componentContentEqual = false;
                            }
                        }

                        break;
                    }
                    else
                    {
                        var leftComponent = _leftComponentList[leftIndex];

                        var index = -1;

                        for (int i = rightIndex; i < rightCount; i++)
                        {
                            var rightComponent = _rightComponentList[i];

                            if (leftComponent.GetType() == rightComponent.GetType())
                            {
                                index = i;
                                break;
                            }
                        }

                        if (index == -1)
                        {
                            var childInfo = AddComponentInfo(info, leftComponent, null, leftComponent.GetType().FullName, MissType.missRight);

                            if (!childInfo.AllEqual())
                            {
                                componentContentEqual = false;
                            }

                            leftIndex++;
                        }
                        else
                        {
                            Component rightComponent = null;
                            ComponentCompareInfo childInfo = null;

                            for (int i = rightIndex; i < index; i++)
                            {
                                rightComponent = _rightComponentList[rightIndex];

                                childInfo = AddComponentInfo(info, null, rightComponent, rightComponent.GetType().FullName, MissType.missLeft);

                                if (!childInfo.AllEqual())
                                {
                                    componentContentEqual = false;
                                }
                            }

                            rightComponent = _rightComponentList[index];

                            childInfo = AddComponentInfo(info, leftComponent, rightComponent, leftComponent.GetType().FullName, MissType.allExist);

                            if (!childInfo.AllEqual())
                            {
                                componentContentEqual = false;
                            }

                            leftIndex++;
                            rightIndex = index + 1;
                        }
                    }

                }

                if (componentCountEqual)
                {
                    info.GameObjectCompare |= GameObjectCompareType.componentCountEqual;
                }

                if (componentContentEqual)
                {
                    info.GameObjectCompare |= GameObjectCompareType.componentContentEqual;
                }
            }

            /// <summary>
            /// 添加组件信息
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <param name="name"></param>
            /// <param name="missType"></param>
            /// <returns></returns>
            private static ComponentCompareInfo AddComponentInfo(GameObjectCompareInfo parent, Component left, Component right, string name, MissType missType)
            {
                var componentInfo = CompareComponent(left, right, name, parent.Depth, missType);

                componentInfo.Parent = parent;

                parent.Components.Add(componentInfo);

                return componentInfo;
            }

            /// <summary>
            /// 对比组件
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <param name="name"></param>
            /// <param name="depth"></param>
            /// <param name="missType"></param>
            /// <returns></returns>
            private static ComponentCompareInfo CompareComponent(Component left, Component right, string name, int depth, MissType missType)
            {
                ComponentCompareInfo info = new ComponentCompareInfo(name, depth, ++idCounter);

                info.LeftComponent = left;
                info.RightComponent = right;

                info.MissType = missType;

                bool contentEqual = true;

                if (missType == MissType.allExist)
                {
                    SerializedObject leftSO = new SerializedObject(left);
                    SerializedObject rightSO = new SerializedObject(right);

                    var property = leftSO.GetIterator();

                    bool enterChildren = true;

                    if (property.Next(true)) //跳过base
                    {
                        do
                        {
                            enterChildren = true;

                            var path = property.propertyPath;

                            if (string.IsNullOrWhiteSpace(path))
                            {
                                continue;
                            }

                            if (property.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                enterChildren = false;
                            }
                        }
                        while (property.Next(enterChildren));
                    }

                    leftSO.Dispose();
                    rightSO.Dispose();
                }

                if (contentEqual)
                {
                    info.ComponentCompare |= ComponentCompareType.contentEqual;
                }

                return info;
            }
        }
    }
}