using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace CustomListView
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ListContainer : VisualElement
    {
        private readonly VisualElement root;

        private IList itemsSource;

        public Func<VisualElement> makeItem;
        public Action<VisualElement, int> itemAdded;
        public Action<int> destroyItem;
        public Action<int> itemRemoved;

        private readonly IntegerField amountIntegerField;
        private readonly ScrollView scroll;

        public IList ItemsSource
        {
            get => itemsSource;
            set
            {
                IList val = value;
                if (itemsSource is null)
                {
                    Func<Task<bool>> cont = async () =>
                        {
                            await Task.Delay(25);
                            for (int index = 0; index < val.Count; index++)
                                CreateNewItem(index, false);
                            return true;
                        };
                    cont.Invoke();
                }

                itemsSource = val;
            }
        }

        public ListContainer(string title = "List", bool useFoldout = true, bool showScroller = true) : this()
        {
            var titleLabel = root.Q<Label>("Title");
            titleLabel.text = title;

            var foldout = root.Q<Foldout>("ShowHide");
            foldout.Q<Toggle>().style.display = useFoldout ? DisplayStyle.Flex : DisplayStyle.None;
            foldout.text = useFoldout ? title : "";

            scroll.verticalScrollerVisibility = showScroller ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
        }

        public ListContainer()
        {
            root = this;

            void CreateVisual()
            {
                string GetCurrentFileName([System.Runtime.CompilerServices.CallerFilePath] string fileName = null) =>
                    fileName;

                string filePath = GetCurrentFileName();
                string directoryPath = Path.GetDirectoryName(filePath);
                string currentDirectory = Directory.GetCurrentDirectory();
                string relativePath = directoryPath?.Replace($"{currentDirectory}\\", string.Empty);

                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>
                    ($"{relativePath}/{nameof(ListContainer)}.uxml").CloneTree(root);
                root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>
                                         ($"{relativePath}/{nameof(ListContainer)}.uss"));
            }

            CreateVisual();

            amountIntegerField = root.Q<IntegerField>("Amount");
            scroll = root.Q<ScrollView>("Scroll");
            ToolbarButton removeBtn = root.Q<ToolbarButton>("Remove");
            ToolbarButton addBtn = root.Q<ToolbarButton>("Add");

            amountIntegerField.RegisterValueChangedCallback(evt =>
                {
                    int val = evt.newValue;
                    int itemCount = ItemsSource.Count;
                    if (val < 0 || val == itemCount)
                    {
                        amountIntegerField.SetValueWithoutNotify(itemCount);
                        return;
                    }

                    int result = val - itemCount;
                    if (result > 0)
                    {
                        for (int index = itemCount; index < val; index++)
                            CreateNewItem(index);
                    }
                    else
                    {
                        result = Mathf.Abs(result);
                        for (int index = result; index > 0; index--)
                            DeleteItem();
                    }
                });

            removeBtn.clicked += DeleteItem;
            addBtn.clicked += () => CreateNewItem(ItemsSource.Count);
        }

        private void DeleteItem()
        {
            if (ItemsSource.Count == 0) return;
            void DestroyItem(int i) => scroll.contentContainer.hierarchy.Children().Last().RemoveFromHierarchy();

            void RemoveItem(int i) => ItemsSource.RemoveAt(i);

            int countIndex = ItemsSource.Count - 1;
            destroyItem += DestroyItem;
            itemRemoved += RemoveItem;
            destroyItem?.Invoke(countIndex);
            itemRemoved?.Invoke(countIndex);
            destroyItem -= DestroyItem;
            itemRemoved -= RemoveItem;
            amountIntegerField.SetValueWithoutNotify(ItemsSource.Count);
        }

        private void CreateNewItem(int index, bool shouldAddElement = true)
        {
            object GetDefaultValue(Type type)
            {
                if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null) return null;
                return Activator.CreateInstance(type);
            }

            if (shouldAddElement)
            {
                if (ItemsSource.Count > 0)
                {
                    object lastItem = ItemsSource[^1];
                    ItemsSource.Add(lastItem);
                }
                else
                {
                    var itemType = ItemsSource.GetType().GetGenericArguments()[0];
                    ItemsSource.Add(GetDefaultValue(itemType));
                }
            }

            var visual = makeItem?.Invoke();
            scroll?.Add(visual);
            itemAdded?.Invoke(visual, index);
            amountIntegerField.SetValueWithoutNotify(ItemsSource.Count);
        }


        public new class UxmlFactory : UxmlFactory<ListContainer> { }
    }
}