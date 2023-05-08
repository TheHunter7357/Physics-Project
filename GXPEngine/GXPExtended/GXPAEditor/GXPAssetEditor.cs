using GXPEngine;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public static class Selection
{
    public const int UNSELECTED = 0x757575;
    public const int SELECTED = 0xFFFFFF;

    public static string Filename;
    public static GameObject DocumentObject;

    private static GameObject _selectedGameObject;
    public static GameObject SelectedGameObject
    {
        get => _selectedGameObject;
        set
        {
            _selectedGameObject = value;
            ClearComponents();

            if (value is null)
                return;

            if (!_displayersSet)
            {
                InitSelectionBox();
                _displayersSet = true;
            }
            _selectedName.Text(value.name.Length > 11 ? value.name.Substring(0, 9) + ".." : value.name, clear: true);
            _selectedLayer.Text(value.LayerMask.Length > 11 ? value.LayerMask.Substring(0, 9) + ".." : value.LayerMask, clear: true);
            RefreshComponents();
        }
    }
    private static EasyDraw _selectedName, _selectedLayer;
    private static VerticalList _componentList;
    private static bool _displayersSet = false;

    public static void Transform(Vec2 position, Vec2 scaleDelta, float rotationDelta = 0f)
    {
        if (SelectedGameObject is null)
            return;

        SelectedGameObject.SetScaleXY(SelectedGameObject.scaleX + scaleDelta.x, SelectedGameObject.scaleY + scaleDelta.y);
        SelectedGameObject.SetXY(SelectedGameObject.x - position.x, SelectedGameObject.y - position.y);
        SelectedGameObject.rotation += rotationDelta;
    }

    public static void InitSelectionBox()
    {
        Setup.SelectionBox.AddChild(_selectedName
        = new EasyDraw(100, 50, layerMask: "GUI")
        {
            x = 60,
            y = 14,
            color = 0xffbb00,
            font = GXPAssetEditor.EditorFont,
        });
        Setup.SelectionBox.AddChild(_selectedLayer
        = new EasyDraw(100, 50, layerMask: "GUI")
        {
            x = 60,
            y = 30,
            color = 0xffbb00,
            font = GXPAssetEditor.EditorFont
        });
        Setup.SelectionBox.AddChild(
        new GUIButton("plus_icon",
        action: () => GXPAssetEditor.OpenAddComponent(SelectedGameObject),
        layerMask: "GUI")
        {
            x = 144,
            y = 380,
        });
        Setup.SelectionBox.AddChild(_componentList =
        new VerticalList(21, layerMask: "GUI")
        {
            x = 86,
            y = 128,
        });
    }
    private static void ClearComponents()
    {
        if (_componentList is null)
            return;

        _componentList.Clear();
    }
    public static void RefreshComponents()
    {
        if (SelectedGameObject is null)
            return;

        _componentList.Clear();
        Component[] components = SelectedGameObject.GetAllComponents();
        GUIButton[] elements = new GUIButton[components.Length];
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            string componentName = component.GetType().Name;
            elements[i] = new GUIButton("element_base", action: () => GXPAssetEditor.OpenComponentBox(component), layerMask: "GUI");
            elements[i].AddChild(new ConstValueDisplayer<string>(componentName.Length > 14 ? componentName.Substring(0, 12) + ".." : componentName, 130, 18)
            {
                x = -66,
                y = -8,
                color = 0xffbb00,
                font = GXPAssetEditor.EditorFont,
                HorizontalShapeAlign = CenterMode.Center,
                VerticalShapeAlign = CenterMode.Center
            });
            elements[i].AddChild(
            new GUIButton("solid_minus_icon",
            action: () => showEditorDebug(component),
            layerMask: "GUI")
            {
                x = 83,
                y = 0,
                scale = new Vec2(0.8f, 0.8f)
            });
        }
        _componentList.Add(elements);
        void showEditorDebug(Component component)
        {
            if (component is Collider)
            {
                Settings.Setup.OnBeforeStep -= SelectedGameObject.Collider.ShowEditorDebug;

                foreach (GameObject child in SelectedGameObject.GetChildren())
                    Settings.Setup.OnBeforeStep -= child.Collider.ShowEditorDebug;
            }
            SelectedGameObject.RemoveComponent(component);
            RefreshComponents();
        }
    }
    public static void SelectionBoxSetVisible(bool visible)
    {
        Setup.SelectionBox.visible = visible;
    }
}
public static class WaitForPropertyInput
{
    private static int _instanceCount = 0;
    public static string Text;

    private static object _obj;
    private static PropertyInfo _property;
    private static Sprite _base, _back;
    private static ValueDisplayer<string> _foreground;

    private static bool _isX;
    private static bool _isVector2;

    public static void Try(PropertyInfo property, object obj, bool isVector2 = false, bool isX = true)
    {
        if (_instanceCount > 0)
            return;

        Text = !isVector2 ? property.GetValue(obj).ToString()
            : (isX ? ((Vec2)property.GetValue(obj)).x.ToString()
            : ((Vec2)property.GetValue(obj)).y.ToString());
        _obj = obj;
        _property = property;
        _isX = isX;
        _isVector2 = isVector2;

        Setup.GUI.AddChild(
        _back = new Sprite("block")
        {
            scaleX = Settings.Setup.width,
            scaleY = Settings.Setup.height,
            color = 0x000000,
            alpha = 0.7f
        });

        Setup.GUI.AddChild(
        _base = new Sprite("value_entry")
        {
            x = 540 + _instanceCount * 30,
            y = 320 + _instanceCount * 30
        });

        _base.AddChild(
        _foreground = new ValueDisplayer<string>(() => Text, 285, 30)
        {
            x = 22,
            y = 45,
            color = 0xffbb00,
            font = GXPAssetEditor.EditorFont,
            HorizontalTextAlign = CenterMode.Center
        });

        _base.AddChild(
        new ConstValueDisplayer<string>("<press 'ENTER' to save value>", 285, 30)
        {
            x = 22,
            y = 70,
            color = 0xff0000,
            font = GXPAssetEditor.EditorFont,
            HorizontalTextAlign = CenterMode.Center
        });
        _base.AddChild(
        new GUIButton("minus_icon", action: () => Finish(false), layerMask: "GUI")
        {
            x = 308,
            y = 21,
        });

        Settings.Setup.OnBeforeStep += ListenInput;
        _instanceCount++;
    }
    private static void ListenInput()
    {
        if (Input.GetKeyDown(Key.BACKSPACE))
            Backspace();
        else if (Input.GetKeyDown(Key.ENTER) || Input.GetKeyDown(Key.NUMPAD_ENTER))
            Finish(true);
        else if (Input.GetKeyDown(Key.ESCAPE))
            Finish(false);
        else if (Input.AnyKeyDown())
            Add(Input.GetKeys());
    }
    public static void Add(int charId)
    {
        switch (charId)
        {
            case Key.CAPS_LOCK:
            case Key.TAB:
            case Key.LEFT_SHIFT:
            case Key.RIGHT_SHIFT:
            case Key.LEFT_CTRL:
            case Key.RIGHT_CTRL:
                return;
        }
        Text += char.ConvertFromUtf32(charId);
    }
    public static void Add(int[] chars)
    {
        foreach (int charId in chars)
            Add(charId);
    }
    public static void Backspace()
    {
        Text = Text.Length > 0 ? Text.Substring(0, Text.Length - 1) : Text;
    }
    public static void Finish(bool result)
    {
        Settings.Setup.OnBeforeStep -= ListenInput;
        if (result)
            try
            {
                _property.SetValue
                (
                    _obj,
                    !_isVector2 ?
                    Convert.ChangeType(Text, _property.PropertyType) :
                    (
                        _isX ?
                        new Vec2(float.Parse(Text), ((Vec2)_property.GetValue(_obj)).y) :
                        new Vec2(((Vec2)_property.GetValue(_obj)).x, float.Parse(Text))
                    )
                );
            }
            catch
            {
                Debug.Log(">> Property wasn't changed due to the invalid given value");
            }

        _instanceCount--;
        if (_instanceCount == 0 && !Setup.ComponentBox.visible)
            GXPAssetEditor.SubscribeEditor();
        _base.Destroy();
        _back.Destroy();
    }
}
public static class GXPAssetEditor
{
    [NonSerialized] public static Font EditorFont = Utils.LoadFont(Settings.AssetsPath + "DOS.ttf", 12);
    [NonSerialized] private static Sprite _darkening;

    public static void Start(string filename)
    {
        Settings.CreateEditorDebugDraw();
        GameObject documentObject = AssetManager.LoadAsset(filename, fullname: true);
        Setup.MainLayer.AddChild(documentObject);
        Selection.Filename = filename; 
        Selection.DocumentObject = documentObject;
        TryOutliningGameObject(documentObject, false, true);
    }
    public static void SubscribeEditor()
    {
        InputManager.OnRightMousePressed += ChangeSelection;
        InputManager.OnSaveCombination += Save;
        InputManager.OnAddCombination += Add;
        InputManager.OnMouseMoved += Transform;
        InputManager.OnDeleteButtonPressed += Delete;
    }
    public static void UnsubscribeEditor()
    {
        InputManager.OnRightMousePressed -= ChangeSelection;
        InputManager.OnSaveCombination -= Save;
        InputManager.OnAddCombination -= Add;
        InputManager.OnMouseMoved -= Transform;
        InputManager.OnDeleteButtonPressed -= Delete;
    }
    private static void OnSelected()
    {
        if (Selection.SelectedGameObject is null)
            return;

        Debug.Log(">> Selected : " + Selection.SelectedGameObject.name);

        TryOutliningGameObject(Selection.SelectedGameObject, true);
        Selection.SelectionBoxSetVisible(true);
    }
    private static void OnUnselected()
    {
        if (Selection.SelectedGameObject is null)
            return;

        Debug.Log(">> Deselected : " + Selection.SelectedGameObject.name);

        TryOutliningGameObject(Selection.SelectedGameObject, false);
        Selection.SelectionBoxSetVisible(false);
    }
    private static void Add()
    {
        Debug.Log(">> Started GXP Asset import");

        using (OpenFileDialog FileDialog = new OpenFileDialog())
        {
            FileDialog.InitialDirectory = Settings.AssetsPath;
            FileDialog.Filter = "GXP Assets (*.gxpa)|*.gxpa";
            FileDialog.RestoreDirectory = true;

            if (FileDialog.ShowDialog() == DialogResult.OK)
            {
                GameObject loadedAsset = AssetManager.LoadAsset(FileDialog.FileName, true);
                loadedAsset.SetXY(0, 0);
                loadedAsset.SetScaleXY(1, 1);
                loadedAsset.rotation = 0;

                Selection.DocumentObject.AddChild(loadedAsset);
                TryOutliningGameObject(loadedAsset, false);

                Debug.Log(">> Imported GXP Asset import : " + FileDialog.FileName);
            }
        }

        ResetCamera();
    }
    private static void Save()
    {
        string shortenedFileName = Selection.Filename
            .Substring(0, Selection.Filename.LastIndexOf('.'))
            .Substring(Selection.Filename.LastIndexOf('\\') + 1);

        AssetManager.DeleteAsset(shortenedFileName);
        AssetManager.CreateAsset(shortenedFileName, Selection.DocumentObject);
        Debug.Log(">> Saved file");
    }
    private static void Delete()
    {
        if (Selection.SelectedGameObject is null)
            return;

        Debug.Log(">> Deleted gameObject : " + Selection.SelectedGameObject.name);

        if(Selection.SelectedGameObject == Selection.DocumentObject)
        {
            if (Selection.DocumentObject is Sprite sprite)
                sprite.ResetParameters("Empty");
            return;
        }

        Selection.SelectedGameObject.Destroy();
        Selection.SelectedGameObject = null;
    }
    private static void Transform()
    {
        if (Selection.SelectedGameObject is null)
            return;

        Selection.Transform
        (
            Input.GetKey(Key.LEFT_SHIFT) && !Input.GetKey(Key.LEFT_CTRL) && !Input.GetKey(Key.LEFT_ALT) ? InputManager.MouseDelta * 0.8f : Vec2.Zero,
            Input.GetKey(Key.LEFT_CTRL) ?
            (
                Input.GetKey(Key.LEFT_SHIFT)?
                new Vec2(InputManager.MouseDelta.y, InputManager.MouseDelta.y) * 0.01f :
                new Vec2(-InputManager.MouseDelta.x, InputManager.MouseDelta.y) * 0.01f
            ) 
            : Vec2.Zero,
            Input.GetKey(Key.LEFT_ALT) && !Input.GetKey(Key.LEFT_CTRL) ? InputManager.MouseDelta.y * 0.4f : 0
        );
    }
    private static void ChangeSelection()
    {
        Vec2 mousePosition = new Vec2(Input.mouseX, Input.mouseY);
        if (Input.mouseX < 192) return;
        foreach (GameObject gameObject in Setup.GUI.GetChildren())
            if (gameObject is Sprite sprite && sprite.visible && sprite.HitTest(mousePosition))
                return;

        GameObject currentGameObject = null, prevGameObject = Selection.SelectedGameObject;
        foreach (GameObject gameObject in Selection.DocumentObject.GetChildren())
        {
            if (gameObject != Selection.SelectedGameObject && gameObject is Sprite sprite && sprite.HitTest(mousePosition))
                currentGameObject = gameObject;
        }
        if (currentGameObject == null)
        {
            prevGameObject = null;
            currentGameObject = Selection.DocumentObject;
        }
        OnUnselected();
        Selection.SelectedGameObject = currentGameObject;
        if (prevGameObject != Selection.SelectedGameObject)
            OnSelected();
    }
    private static void TryOutliningGameObject(GameObject gameObject, bool selected, bool withChildren = false)
    {
        if (gameObject is Sprite sprite)
            sprite.color = (uint)(selected ? Selection.SELECTED : Selection.UNSELECTED);

        editorDebug(gameObject);

        if (!withChildren)
            return;

        foreach (GameObject child in gameObject.GetChildren())
        {
            if (child is Sprite childSprite)
                childSprite.color = (uint)(selected ? Selection.SELECTED : Selection.UNSELECTED);

            editorDebug(child);
        }
        void editorDebug(GameObject debugObject)
        {
            if (debugObject.Collider is null)
                return;

            if (selected)
                Settings.Setup.OnBeforeStep += debugObject.Collider.ShowEditorDebug;
            else
                Settings.Setup.OnBeforeStep -= debugObject.Collider.ShowEditorDebug;
        }
    }
    private static void ResetCamera()
    {
        Camera.ClearFocuses();
        Camera.AddFocus(Setup.DocumentPointer);
        Camera.SetLevel(Setup.MainLayer);
    }
    public static void OpenComponentBox(Component component)
    {
        if (component is null)
            return;
        CloseComponentBox();
        UnsubscribeEditor();
        VerticalList fieldNameList, fieldChangeList, fieldChangeList2, fieldValueList, fieldValueList2;

        Setup.ComponentBox.visible = true;
        Setup.GUI.AddChild(
        _darkening = new Sprite("block")
        {
            scaleX = Settings.Setup.width,
            scaleY = Settings.Setup.height,
            color = 0x000000,
            alpha = 0.7f
        });
        Setup.GUI.SetChildIndex(_darkening, Setup.ComponentBox.Index);
        Setup.ComponentBox.AddChild(
        new ConstValueDisplayer<string>(component.GetType().Name, 380, 50)
        {
            x = 158,
            y = 38,
            color = 0xffbb00,
            font = EditorFont,
            VerticalTextAlign = CenterMode.Center
        });
        Setup.ComponentBox.AddChild(
        new ConstValueDisplayer<string>(component.Owner.name, 380, 50)
        {
            x = 125,
            y = 59,
            color = 0xffbb00,
            font = EditorFont,
            VerticalTextAlign = CenterMode.Center
        });
        Setup.ComponentBox.AddChild(
        new GUIButton("minus_icon", action: CloseComponentBox, layerMask: "GUI")
        {
            x = 382,
            y = 22,
        });
        Setup.ComponentBox.AddChild(
        fieldNameList = new VerticalList(21, layerMask: "GUI")
        {
            x = 27,
            y = 100,
        });
        Setup.ComponentBox.AddChild(
        fieldChangeList = new VerticalList(21, layerMask: "GUI")
        {
            x = 310,
            y = 140,
        });
        Setup.ComponentBox.AddChild(
        fieldChangeList2 = new VerticalList(21, layerMask: "GUI")
        {
            x = 380,
            y = 140,
        });
        Setup.ComponentBox.AddChild(
        fieldValueList = new VerticalList(21, layerMask: "GUI")
        {
            x = 242,
            y = 100,
        });
        Setup.ComponentBox.AddChild(
        fieldValueList2 = new VerticalList(21, layerMask: "GUI")
        {
            x = 310,
            y = 100,
        });
        PropertyInfo[] properties = component.GetType().GetProperties();
        foreach (PropertyInfo property in properties)
        {
            if (property.PropertyType.IsGenericType 
                && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>) 
                || typeof(IEnumerable).IsAssignableFrom(property.PropertyType) 
                || property.PropertyType.GetInterfaces().Contains(typeof(ICollection)))
                continue;

            bool oneColumn = true;
            if (property.PropertyType == typeof(Vec2))
                oneColumn = false;

            fieldNameList.Add(new ConstValueDisplayer<string>(property.Name.Length > 22 ? property.Name.Substring(0, 20) + ".." : property.Name, 320, 50)
            {
                color = 0xff0000,
                font = EditorFont,
            });

            if (oneColumn)
            {
                fieldChangeList.Add(new GUIButton("element_base", () => WaitForPropertyInput.Try(property, component), layerMask: "GUI"));
                fieldChangeList2.Add(new Sprite("Empty"));
                fieldValueList.Add(new ValueDisplayer<string>(() => property.GetValue(component).ToString(), 320, 50)
                {
                    color = 0xffbb00,
                    font = EditorFont,
                });
                fieldValueList2.Add(new Sprite("Empty"));
            }
            else
            {
                Sprite column1, column2;
                fieldChangeList.Add(column1 = new GUIButton("element_base0,5", () => WaitForPropertyInput.Try(property, component, true, true), layerMask: "GUI"));
                fieldChangeList2.Add(column2 = new GUIButton("element_base0,5", () => WaitForPropertyInput.Try(property, component, true, false),layerMask: "GUI"));
                fieldValueList.Add(new ValueDisplayer<string>(() => "x:" + ((Vec2)property.GetValue(component)).x.ToString(), 320, 50)
                {
                    color = 0xffbb00,
                    font = EditorFont,
                });
                fieldValueList2.Add(new ValueDisplayer<string>(() => "y:" + ((Vec2)property.GetValue(component)).y.ToString(), 320, 50)
                {
                    color = 0xffbb00,
                    font = EditorFont,
                });
                column1.SetOrigin(68, column1.GetOrigin().y);
                column2.SetOrigin(69, column2.GetOrigin().y);
            }
        }
    }
    private static void CloseComponentBox()
    {
        if (Setup.ComponentBox.visible)
        {
            SubscribeEditor();
            _darkening.Destroy();
            Setup.ComponentBox.visible = false;
            Setup.ComponentBox.DestroyChildren();
        }
    }
    public static void OpenAddComponent(GameObject gameObject)
    {
        if (gameObject is null)
            return;
        CloseAddComponent();
        UnsubscribeEditor();
        VerticalList componentList;
        Setup.GUI.AddChild(
        _darkening = new Sprite("block")
        {
            scaleX = Settings.Setup.width,
            scaleY = Settings.Setup.height,
            color = 0x000000,
            alpha = 0.7f
        });
        Setup.GUI.SetChildIndex(_darkening, Setup.ComponentList.Index);
        Setup.ComponentList.visible = true;
        Setup.ComponentList.AddChild(
        new GUIButton("minus_icon", action: CloseAddComponent, layerMask: "GUI")
        {
            x = 296,
            y = 22,
        });
        Setup.ComponentList.AddChild(
        componentList = new VerticalList(21, layerMask: "GUI")
        {
            x = 20,
            y = 48,
        });
        Type[] componentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => typeof(Component).IsAssignableFrom(type)).ToArray();
        foreach (Type componentType in componentTypes)
        {
            gameObject.TryGetComponentsAssignableFrom(componentType, out Component[] components);
            if (componentType == typeof(Component) || components != null && components.Length > 0 || (typeof(Collider).IsAssignableFrom(componentType) && gameObject.Collider != null)) continue;
            GUIButton button = new GUIButton("component_base", action:() => createComponent(componentType), layerMask: "GUI");
            button.SetOrigin(0, 0);
            button.AddChild(new ConstValueDisplayer<string>(componentType.Name, 282, 32) 
            {
                y = -12,
                color = 0xffbb00,
                font = EditorFont,
                HorizontalTextAlign = CenterMode.Center
            });
            componentList.Add(button);
        }
        void createComponent(Type type)
        {
            gameObject.AddComponent(type);
            Selection.RefreshComponents();

            if (typeof(Collider).IsAssignableFrom(type) && gameObject == Selection.SelectedGameObject)
            {
                Settings.Setup.OnBeforeStep += gameObject.Collider.ShowEditorDebug;

                foreach (GameObject child in gameObject.GetChildren())
                    Settings.Setup.OnBeforeStep += child.Collider.ShowEditorDebug;
            }
            CloseAddComponent();
            OpenAddComponent(gameObject);
        }
    }
    private static void CloseAddComponent()
    {
        if (Setup.ComponentList.visible)
        {
            SubscribeEditor();
            _darkening.Destroy();
            Setup.ComponentList.visible = false;
            Setup.ComponentList.DestroyChildren();
        }
    }
}
