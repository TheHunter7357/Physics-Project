using GXPEngine;

public static class InputManager
{
    public delegate void OnInput();
    
    public static event OnInput OnUpButtonPressed;
    public static event OnInput OnDownButtonPressed;
    public static event OnInput OnRightButtonPressed;
    public static event OnInput OnLeftButtonPressed;
    public static event OnInput OnSpaceButtonPressed;
    public static event OnInput OnDeleteButtonPressed;

    public static event OnInput OnLeftMousePressed;
    public static event OnInput OnRightMousePressed;

    public static event OnInput OnAnyButtonPressed;
    public static event OnInput OnMouseMoved;

    public static event OnInput OnSaveCombination;
    public static event OnInput OnAddCombination;

    public static Vec2 MouseDelta => _prevMousePosition - _currentMousePosition;
    private static Vec2 _prevMousePosition;
    private static Vec2 _currentMousePosition;
    public static void ListenToInput()
    {
        if (Input.AnyKey()) OnAnyButtonPressed?.Invoke();

        if (Input.GetKey(Key.S) && Input.GetKeyDown(Key.LEFT_CTRL)) OnSaveCombination?.Invoke();
        if (Input.GetKey(Key.D) && Input.GetKeyDown(Key.LEFT_CTRL)) Debug.Log(Settings.Setup.GetDiagnostics());
        if (Input.GetKey(Key.A) && Input.GetKeyDown(Key.LEFT_CTRL)) OnAddCombination?.Invoke();

        if (Input.GetKey(Key.S)) OnUpButtonPressed?.Invoke();
        if (Input.GetKey(Key.W)) OnDownButtonPressed?.Invoke();
        if (Input.GetKey(Key.A)) OnLeftButtonPressed?.Invoke();
        if (Input.GetKey(Key.D)) OnRightButtonPressed?.Invoke();
        if (Input.GetKeyDown(Key.SPACE)) OnSpaceButtonPressed?.Invoke();
        if (Input.GetKeyDown(Key.DELETE) || Input.GetKeyDown(Key.DELETE2)) OnDeleteButtonPressed?.Invoke();

        if (Input.GetMouseButtonDown(0)) OnRightMousePressed?.Invoke();
        if (Input.GetMouseButtonDown(1)) OnLeftMousePressed?.Invoke();

        if (_prevMousePosition != new Vec2(Input.mouseX, Input.mouseY))
        {
            _prevMousePosition = _currentMousePosition;
            _currentMousePosition = new Vec2(Input.mouseX, Input.mouseY);
            OnMouseMoved?.Invoke();
        }
    }

}