using GXPEngine;
using GXPEngine.Core;
using System.Windows.Forms;

public static class Settings
{
    public static bool EditMode = true;

    private static Setup _setup;
    public static Setup Setup
    {
        get => _setup;
        set
        {
            if(Setup == null)
                _setup = value;
        }
    }

    private static GLContext _glContext;
    public static GLContext GLContext
    {
        get => _glContext;
        set
        {
            if (EditMode)
                _glContext = value;
        }
    }

    private static int _threadCount = -1;
    public static int ThreadCount 
    {
        get => _threadCount;
        set 
        {
            if (_threadCount == -1) 
                _threadCount = value;
        }
    }

    private static System.Drawing.Rectangle _screen;
    public static System.Drawing.Rectangle Screen
    {
        get 
        {
            if (Fullscreen)
                return _screen;
            else
            {
                System.Drawing.Rectangle smallerScreen = _screen;
                smallerScreen.Height = (int)(_screen.Height * 0.8f);
                smallerScreen.Width = (int)(_screen.Width * 0.8f);
                return smallerScreen;
            }       
        }
        set
        {
            if(EditMode)
                _screen = value;
        }
    }

    private static string _applicationPath;
    public static string ApplicationPath
    {
        get => _applicationPath;
        set
        {
            if (EditMode)
                _applicationPath = value;
        }
    }

    private static string _assetsPath;
    public static string AssetsPath
    {
        get => _assetsPath;
        set
        {
            if (EditMode)
                _assetsPath = value;
        }
    }

    private static bool _fullscreen;
    public static bool Fullscreen
    {
        get => _fullscreen;
        set
        {
            if (EditMode)
                _fullscreen = value;
        }
    }

    private static float _volume;
    public static float Volume
    {
        get => _volume;
        set
        {
            if (EditMode)
            {
                if(value <= 1 && value >= 0)
                    _volume = value;
            }
        }
    }

    private static bool _collisionDebug;
    public static EasyDraw ColliderDebug;
    public static EasyDraw EditorColliderDebug;
    public static bool CollisionDebug
    {
        get => _collisionDebug;
        set
        {
            if (EditMode)
                _collisionDebug = value;
            else return;

            if (value)
                Setup.OnBeforeStep += CreateDebugDraw;
        }
    }

    private static int _collisionPrecision;
    public static int CollisionPrecision
    {
        get => _collisionPrecision;
        set
        {
            if (EditMode)
                _collisionPrecision = value;
        }
    }

    private static bool _componentRegistrationBlock;
    public static bool ComponentRegistrationBlock
    {
        get => _componentRegistrationBlock;
        set
        {
            if (EditMode)
                _componentRegistrationBlock = value;
        }
    }

    private static void CreateDebugDraw()
    {
        ColliderDebug = new EasyDraw(Setup.width, Setup.height);
        ColliderDebug.NoFill();
        ColliderDebug.SetOrigin(Setup.width / 2, Setup.height / 2);
        ColliderDebug.SetXY(Setup.width / 2, Setup.height / 2);
        Setup.CollisionDebug.AddChild(ColliderDebug);

        Setup.OnBeforeStep -= CreateDebugDraw;
        Setup.OnBeforeStep += ColliderDebug.ClearTransparent;
    }
    public static void CreateEditorDebugDraw()
    {
        EditorColliderDebug = new EasyDraw(Setup.width, Setup.height);
        EditorColliderDebug.NoFill();
        EditorColliderDebug.SetOrigin(Setup.width / 2, Setup.height / 2);
        EditorColliderDebug.SetXY(Setup.width / 2, Setup.height / 2);
        EditorColliderDebug.Stroke(255, 255, 0);
        EditorColliderDebug.StrokeWeight(2f);
        Setup.EditorCollisionDebug.AddChildAt(EditorColliderDebug,0);

        Setup.OnBeforeStep += EditorColliderDebugAnimation.Animate;
    }
    public static void ReadParameters()
    {
        Screen = System.Windows.Forms.Screen.AllScreens[0].Bounds;
        ThreadCount = System.Environment.ProcessorCount;
    }
    public static void RefreshAssetsPath()
    {
        string path = Application.ExecutablePath;
        ApplicationPath = path + "\\";

        for (int i = 0; i < 3; i++)
        {
            path = path.Substring(0, path.LastIndexOf('\\'));
        }
        AssetsPath = path + "\\Assets\\";
    } 

    private static class EditorColliderDebugAnimation
    {
        private static bool _direction = false;
        public static void Animate()
        {
            byte green = EditorColliderDebug.pen.Color.G;
            
            if(green % 4 == 0)
                EditorColliderDebug.ClearTransparent();

            if (green < 255 && _direction)
                EditorColliderDebug.Stroke(255, (green + 7) > 255 ? 255 : green + 7, green / 5);
            else if (green > 90 && !_direction)
                EditorColliderDebug.Stroke(255, (green - 7) < 90 ? 90 : green - 7, green / 5);

            if (EditorColliderDebug.pen.Color.G == 255 || EditorColliderDebug.pen.Color.G == 90)
                _direction = !_direction;
        }
    }
}
