using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Popup
{
    public class PopupList<T> : PopupWindowContent where T : IPopupListItem
    {
        private const float MINIMUM_WIDTH = 150f;
        private const float DEFAULT_WINDOW_HEIGHT = 250f;
        private const float SCROLLBAR_WIDTH = 15f;

        private static Type implementationType = Type.GetType("UnityEditor.PopupList, UnityEditor");

        private static PropertyInfo editorWindowProperty = implementationType.GetProperty("editorWindow",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public delegate void OnItemSelectedDelegate(T item, bool selected);

        public event OnItemSelectedDelegate OnItemSelectedEvent
        {
            add => inputData.onItemSelectedEvent += value;
            remove => inputData.onItemSelectedEvent -= value;
        }

        public event Action OnClosedEvent;

        public int Count { get { return inputData.Count; } }

        private bool isOpen;
        public bool IsOpen { get { return isOpen; } }

        private PopupWindowContent implementation;
        private InputData inputData;

        private Vector2 scrollPosition;

        public class InputData
        {
            private static Type implementationType = Type.GetType("UnityEditor.PopupList+InputData, UnityEditor");

            private static FieldInfo elementsField = implementationType.GetField("m_ListElements",
                BindingFlags.Public | BindingFlags.Instance);

            private static FieldInfo allowCustomField = implementationType.GetField("m_AllowCustom",
                BindingFlags.Public | BindingFlags.Instance);

            private static FieldInfo callbackField = implementationType.GetField("m_OnSelectCallback",
                BindingFlags.Public | BindingFlags.Instance);

            private static Type elementType = Type.GetType("UnityEditor.PopupList+ListElement, UnityEditor");

            private static Type callbackType = Type.GetType("UnityEditor.PopupList+OnSelectCallback, UnityEditor");

            private static PropertyInfo selectedProperty = elementType.GetProperty("selected",
                BindingFlags.Public | BindingFlags.Instance);

            private static FieldInfo contentField = elementType.GetField("m_Content",
                BindingFlags.Public | BindingFlags.Instance);

            private static MethodInfo deselectAllMethod = implementationType.GetMethod("DeselectAll",
                BindingFlags.Public | BindingFlags.Instance);

            public OnItemSelectedDelegate onItemSelectedEvent;

            private object implementation;
            public object Implementation { get { return implementation; } }
            public int Count { get { return items.Count; } }

            private IList elements;
            private List<T> items = new List<T>();

            public delegate void OnSelectCallback(object element);

            public float ContentWidth { get; private set; } = float.MinValue;

            private void OnElementSelect(object element)
            {
                string name = ((GUIContent)contentField.GetValue(element)).text;

                int index = -1;
                bool found = false;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].Name == name)
                    {
                        index = i;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    elements.Remove(element);
                    return;
                }

                bool currentValue = (bool)selectedProperty.GetValue(element, null);
                selectedProperty.SetValue(element, !currentValue, null);

                if (onItemSelectedEvent != null)
                    onItemSelectedEvent(items[index], !currentValue);
            }

            public void SetSelected(T item, bool selected)
            {
                selectedProperty.SetValue(elements[items.IndexOf(item)], selected, null);
            }

            public void SetSelected(int index, bool selected)
            {
                selectedProperty.SetValue(elements[index], selected, null);
            }

            public InputData()
            {
                implementation = Activator.CreateInstance(implementationType);
                elements = (IList)elementsField.GetValue(implementation);
                callbackField.SetValue(implementation,
                    DelegateUtility.CastToType((OnSelectCallback)OnElementSelect, callbackType));
                allowCustomField.SetValue(implementation, true);
            }

            public void AddItem(T item, bool selected)
            {
                object element = Activator.CreateInstance(elementType, item.Name, selected, 0);

                IPopupListItem popupListItem = item;
                GUIStyle style = "MenuItem";
                Vector2 size = style.CalcSize(new GUIContent(popupListItem.Name));
                ContentWidth = Mathf.Max(ContentWidth, size.x);

                elements.Add(element);
                items.Add(item);
            }

            public void Clear()
            {
                elements.Clear();
                items.Clear();
            }

            public bool GetSelected(T item)
            {
                return (bool)selectedProperty.GetValue(elements[items.IndexOf(item)], null);
            }

            public bool GetSelected(int index)
            {
                return (bool)selectedProperty.GetValue(elements[index], null);
            }

            public void DeselectAll()
            {
                deselectAllMethod.Invoke(implementation, null);
            }
        }

        public PopupList()
        {
            inputData = new InputData();
            implementation = (PopupWindowContent)Activator.CreateInstance(implementationType, inputData.Implementation);
        }

        public void AddItem(T item, bool selected)
        {
            inputData.AddItem(item, selected);
        }

        public void SetSelected(T item, bool selected)
        {
            inputData.SetSelected(item, selected);
        }

        public void SetSelected(int index, bool selected)
        {
            inputData.SetSelected(index, selected);
        }

        public bool GetSelected(T item)
        {
            return inputData.GetSelected(item);
        }

        public bool GetSelected(int index)
        {
            return inputData.GetSelected(index);
        }

        public override void OnGUI(Rect rect)
        {
            editorWindowProperty.SetValue(implementation, editorWindow, null);

            Rect contentRect = new Rect(0, 0, rect.width, implementation.GetWindowSize().y);

            if (implementation.GetWindowSize().y > rect.height) // Will draw scroll bar
                contentRect.width -= SCROLLBAR_WIDTH;

            scrollPosition = GUI.BeginScrollView(rect, scrollPosition, contentRect);
            implementation.OnGUI(contentRect);
            GUI.EndScrollView();
        }

        public override Vector2 GetWindowSize()
        {
            float windowWidth = GetWindowWidth();
            float windowHeight = implementation.GetWindowSize().y;

            if (windowHeight > DEFAULT_WINDOW_HEIGHT)
            {
                windowHeight = DEFAULT_WINDOW_HEIGHT;
                windowWidth += SCROLLBAR_WIDTH;
            }

            return new Vector2(windowWidth, windowHeight);
        }

        private float GetWindowWidth()
        {
            return Mathf.Max(MINIMUM_WIDTH, inputData.ContentWidth);
        }

        public override void OnOpen()
        {
            isOpen = true;
            editorWindowProperty.SetValue(implementation, editorWindow, null);
            implementation.OnOpen();
        }

        public override void OnClose()
        {
            isOpen = false;
            editorWindowProperty.SetValue(implementation, editorWindow, null);
            implementation.OnClose();

            inputData.onItemSelectedEvent = null;

            OnClosedEvent?.Invoke();
            OnClosedEvent = null;
        }

        public void DeselectAll()
        {
            inputData.DeselectAll();
        }

        public void Clear()
        {
            inputData.Clear();
        }
    }
}
