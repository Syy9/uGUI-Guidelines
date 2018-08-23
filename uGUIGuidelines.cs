using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Syy.uGUIGuidelines
{
    public class uGUIGuidelines : EditorWindow
    {
        [MenuItem("Window/uGUI-Guidelines")]
        public static void Open()
        {
            GetWindow<uGUIGuidelines>("Guidelines");
        }
        static Dictionary<int,Status> _drawInfo = new Dictionary<int, Status>();
        static uGUIGuidelineComponent guidlineDrawer;
        static Color guidlineColor = new Color(80/255f, 255/ 255f, 22/ 255f);
        static uGUIGuidelines window;

        const int WIDTH = 16;
        const int HEIGHT = 16;
        void OnEnable()
        {
            window = this;
            DestoryGuidlineComponent();
            CreateGuidlineComponent();
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        void OnDisable()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            DestoryGuidlineComponent();
            window = null;
        }

        void CreateGuidlineComponent()
        {
            var obj = new GameObject("uGUI-Guideline-Drawer");
            guidlineDrawer = obj.AddComponent<uGUIGuidelineComponent>();
            obj.hideFlags = HideFlags.DontSaveInEditor;
        }

        void DestoryGuidlineComponent()
        {
            if(guidlineDrawer != null)
            {
                GameObject.DestroyImmediate(guidlineDrawer.gameObject);
            }
        }

        void OnGUI()
        {
            guidlineColor = EditorGUILayout.ColorField("Color",guidlineColor);
            var targets = _drawInfo.Where(data => data.Value.Target != null && data.Value.IsDraw);
            EditorGUILayout.LabelField("Draw Targets");

            using(new EditorGUILayout.VerticalScope("box"))
            {
                if(targets.Count() == 0)
                {
                    EditorGUILayout.LabelField("Check RectTransform's HierarhcyGUI toggle box or click \"c\" button");
                } else {
                    foreach (var item in targets)
                    {
                        EditorGUILayout.LabelField(item.Value.Target.name);
                    }
                }
            }
        }

        static void OnHierarchyGUI(int instanceId, Rect selectionRect)
        {
            var target = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (target == null)
            {
                return;
            }

            if(!(target.transform is RectTransform))
            {
                return;
            }

            Status status;
            if(_drawInfo.ContainsKey(instanceId))
            {
                status = _drawInfo[instanceId];
                if(status.Target == null)
                {
                    _drawInfo.Remove(instanceId);
                    return;
                }
            } else {
                status = new Status();
                status.IsDraw = false;
                status.Target = (RectTransform)target.transform;
                _drawInfo[instanceId] = status;
            }

            //switch children draw
            var childSwitchRect = selectionRect;
            childSwitchRect.x = childSwitchRect.xMax - WIDTH;
            childSwitchRect.width = WIDTH;
            childSwitchRect.height = HEIGHT;
            if(GUI.Button(childSwitchRect, "c"))
            {
                List<RectTransform> childList = new List<RectTransform>();
                GetChildren(status.Target, ref childList);
                var drawChildTargets = childList.Where(data => {
                    int id = data.gameObject.GetInstanceID();
                    return _drawInfo.ContainsKey(id) && _drawInfo[id].IsDraw;
                });
                if(drawChildTargets.Count() == 0)
                {
                    //draw all child
                    foreach (var child in childList)
                    {
                        int id = child.gameObject.GetInstanceID();
                        var newStatus = new Status();
                        newStatus.IsDraw = true;
                        newStatus.Target = child;
                        _drawInfo[id] = newStatus;
                    }

                } else {
                    //remove all draw target child
                    foreach (var child in drawChildTargets)
                    {
                        int id = child.gameObject.GetInstanceID();
                        _drawInfo[id].IsDraw = false;                        
                    }
                }
                ForceRepaint();
            }

            //switch own draw
            var ownSwitchRect = selectionRect;
            ownSwitchRect.x = ownSwitchRect.xMax - WIDTH * 2 - 3;
            ownSwitchRect.width = WIDTH;
            ownSwitchRect.height = HEIGHT;
            EditorGUI.BeginChangeCheck();
            status.IsDraw = EditorGUI.Toggle(ownSwitchRect, status.IsDraw);
            if(EditorGUI.EndChangeCheck())
            {
                ForceRepaint();
            }
            
        }

        static void GetChildren(RectTransform obj, ref List<RectTransform> allChildren)
        {
            var children = obj.GetComponentInChildren<RectTransform>();
            if (children.childCount == 0)
            {
                return;
            }
            foreach (RectTransform child in children)
            {
                allChildren.Add(child);
                GetChildren(child, ref allChildren);
            }
        }

        static void ForceRepaint()
        {
            SceneView.RepaintAll();
            window?.Repaint();
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Active)]
        static void DrawGizmo(uGUIGuidelineComponent drawer, GizmoType gizmoType)
        {
            var targets = _drawInfo.Where(data => data.Value.Target != null && data.Value.IsDraw);
            foreach (var target in targets)
            {
                if (target.Value.IsDraw)
                {
                    var t = target.Value.Target;
                    var position = t.position;
                    var localPosition = t.localPosition;
                    var sizeDelta = t.sizeDelta;
                    var pivot = t.pivot;
                    var lossyScale = t.lossyScale;

                    var xOffset = position.x - localPosition.x;
                    var yOffset = position.y - localPosition.y;

                    var x1 = (localPosition.x - (sizeDelta.x * pivot.x) * lossyScale.x) + xOffset;
                    var x2 = (localPosition.x + (sizeDelta.x * (1f - pivot.x) * lossyScale.x)) + xOffset;
                    var y1 = (localPosition.y - (sizeDelta.y * pivot.y) * lossyScale.y) + yOffset;
                    var y2 = (localPosition.y + (sizeDelta.y * (1f - pivot.y) * lossyScale.y)) + yOffset;

                    var max = 100000f;
                    var min = -100000f;

                    Gizmos.color = guidlineColor;
                    Gizmos.DrawLine(new Vector3(x1, min, 0f), new Vector3(x1, max, 0f));
                    Gizmos.DrawLine(new Vector3(x2, min, 0f), new Vector3(x2, max, 0f));
                    Gizmos.DrawLine(new Vector3(min, y1, 0f), new Vector3(max, y1, 0f));
                    Gizmos.DrawLine(new Vector3(min, y2, 0f), new Vector3(max, y2, 0f));
                }
            }
        }

        public class Status
        {
            public RectTransform Target;
            public bool IsDraw;
        }

        [AddComponentMenu("")] //HACK! Disable addition from Add Component Button in Inspector
        public class uGUIGuidelineComponent : MonoBehaviour
        {
            void Awake()
            {
            }
        }
    }
}
#endif
