using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NiftyEditorMenu
{

    public abstract class MenuItemBase
    {
        public abstract void AddToMenu(string path, GenericMenu menu);
        public abstract string Text {get;}
        
        public abstract int Priority { get; }
    }

    public class TextItem : MenuItemBase
    {
        private readonly string _text;
        private readonly bool _isEnabled = true;
        private readonly bool _isCheckmarked;
        private readonly GenericMenu.MenuFunction _func;
        private readonly GenericMenu.MenuFunction2 _func2;
        private readonly object _data;
        public override string Text => _text;
        
        public override int Priority { get; }

        public TextItem(string text, GenericMenu.MenuFunction func, bool isCheckmarked = false, bool isIsEnabled = true)
        {
            _text = text;
            _isCheckmarked = isCheckmarked;
            _isEnabled = isIsEnabled;
            _func = func;
        }
        public TextItem(string text, GenericMenu.MenuFunction2 func2, object aData, bool aChecked = false, bool aIsEnabled = true)
        {
            _text = text;
            _isCheckmarked = aChecked;
            _isEnabled = aIsEnabled;
            _func2 = func2;
            _data = aData;
        }

        public override void AddToMenu(string path, GenericMenu menu)
        {
            if (!_isEnabled)
                menu.AddDisabledItem(new GUIContent(path + _text));
            else if (_func != null)
                menu.AddItem(new GUIContent(path + _text), _isCheckmarked, _func);
            else if (_func2 != null)
                menu.AddItem(new GUIContent(path + _text), _isCheckmarked, _func2, _data);
        }
    }
    
    public abstract class TypeItemBase<TData> : MenuItemBase
    {
        protected readonly TData _itemData;
        private readonly Action<TData> _onSelected;
        protected readonly GenericMenu.MenuFunction2 _menuSelectHandler;

        protected TypeItemBase(TData itemData, Action<TData> onSelected)
        {
            _itemData = itemData;
            _onSelected = onSelected;
            _menuSelectHandler = OnMenuSelect;
        }

        private void OnMenuSelect(object menuData)
        {
            _onSelected.Invoke(_itemData);
        }
    }
    
    public class TypeItem<TData> : TypeItemBase<TData>
    {
        private readonly string _text;
        private int _priority;
        private readonly Predicate<TData> _predicateIsChecked;

        public TypeItem(TData itemData, Action<TData> onSelected) : base(itemData, onSelected)
        {
            _text = itemData.ToString();
        }
        
        public TypeItem(TData itemData, Action<TData> onSelected, Predicate<TData> predicateIsChecked) : base(itemData, onSelected)
        {
            _predicateIsChecked = predicateIsChecked;
            _text = itemData.ToString();
        }

        public TypeItem(TData itemData, Action<TData> onSelected, Func<TData, string> providerMenuName) : base(itemData, onSelected)
        {
            _text = providerMenuName.Invoke(itemData);
        }

        public override void AddToMenu(string path, GenericMenu menu)
        {
            if (_predicateIsChecked != null)
            {
                menu.AddItem(new GUIContent(path + _itemData),_predicateIsChecked(_itemData), _menuSelectHandler, _itemData);
            }
            else
            {
                menu.AddItem(new GUIContent(path + _itemData),false, _menuSelectHandler, _itemData);
            }
        }

        public override string Text => _text;

        public override int Priority => _priority;
    }
    
    public class MenuSection
    {
        public readonly List<MenuItemBase> Items = new List<MenuItemBase>();
        public void Sort(Comparison<MenuItemBase> comparison, bool isRecursive)
        {
            if (comparison == null)
            {
                comparison = StringOrdinalComparison;
            }
            
            Items.Sort(comparison);
            if (!isRecursive)
            {
                return;
            }
            foreach (var item in Items)
            {
                var menu = item as SortableMenu;
                if (menu != null)
                {
                    menu.Sort(comparison, isRecursive);
                }
            }
        }
        private static int StringOrdinalComparison(MenuItemBase item1, MenuItemBase item2)
        {
            return string.Compare(item1.Text, item2.Text, StringComparison.Ordinal);
        }
    }

    public class SortableMenu : MenuItemBase
    {
        private readonly string _text;
        public override string Text => _text;
        private readonly List<MenuSection> _items = new List<MenuSection>();
        public override int Priority { get; }

        public SortableMenu()
        {
            _items.Add(new MenuSection());
        }
        
        public SortableMenu(string text) : this()
        {
            _text = text;
        }
        
        public SortableMenu(string text, int priority) : this(text)
        {
            Priority = priority;
        }

        public void Clear()
        {
            foreach (var item in _items)
            {
                item.Items.Clear();
            }
        }

        public T AddItem<T>(T item) where T : MenuItemBase
        {
            _items[_items.Count - 1].Items.Add(item);
            return item;
        }
        
        public TextItem AddItem(string text, GenericMenu.MenuFunction func, bool isCheckmarked = false, bool isEnabled = true)
        {
            return AddItem(new TextItem(text, func, isCheckmarked, isEnabled));
        }
        
        public TextItem AddItem(string text, GenericMenu.MenuFunction2 func, object aData, bool isCheckmarked = false, bool isEnabled = true)
        {
            return AddItem(new TextItem(text, func, aData, isCheckmarked, isEnabled));
        }
        
        public MenuSection AddSeparator()
        {
            _items.Add(new MenuSection());
            return _items[_items.Count - 2];
        }
        
        public SortableMenu AddSubMenu(SortableMenu menu)
        {
            return AddItem(menu);
        }
        
        public SortableMenu AddSubMenu(string text)
        {
            return AddItem(new SortableMenu(text));
        }

        public override void AddToMenu(string path, GenericMenu menu)
        {
            string fullPath = string.IsNullOrEmpty(_text)?"": path + _text + "/";
            bool separator = false;
            foreach(var section in _items)
            {
                if (separator)
                {
                    menu.AddSeparator(fullPath);
                }
                foreach (var item in section.Items)
                {
                    item.AddToMenu(fullPath, menu);
                }
                separator = true;
            }
        }

        public GenericMenu CreateMenu()
        {
            var menu = new GenericMenu();
            AddToMenu("", menu);
            return menu;
        }
        public void Sort(Comparison<MenuItemBase> comparison, bool isRecursive = true)
        {
            foreach (var section in _items)
            {
                section.Sort(comparison, isRecursive);
            }
        }
        
    }
}