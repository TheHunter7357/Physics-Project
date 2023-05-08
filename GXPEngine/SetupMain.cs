using GXPEngine;
using System;

[Serializable] public class Setup : Game
{
    //-------------------------------- GRAPHICAL - LAYERS ----------------------------------//
    public static Sprite MainLayer, CollisionDebug, PostProcessing, EditorCollisionDebug, GUI;

    //--------------------------------- GXPA - EDITOR --------------------------------//
    public static GameObject DocumentPointer, SelectionBox, ComponentBox, ComponentList;

    [STAThread] private static void Main() => new Setup();
    public Setup() : base(1280, 720, false, pPixelArt: false, pRealWidth: Settings.Screen.Width, pRealHeight: Settings.Screen.Height)
    {
        void settings()
        {
            Settings.Setup = this;
            Settings.RefreshAssetsPath();
            Settings.ReadParameters();
            Settings.Volume = 0.8f;
            Settings.Fullscreen = false;
            Settings.CollisionDebug = false;
            Settings.CollisionPrecision = 0;
            Settings.ComponentRegistrationBlock = false;
        }
        void subscriptions()
        {
            SoundManager.Init();
            OnBeforeStep += InputManager.ListenToInput;
            OnBeforeStep += Camera.Interpolate;
            AssetManager.UpdateRegistry();
        }
        void layers()
        {
            Physics.AddLayers(new string[]
            { 
                "Default",
                "Bullets",
                "Creatures",
                "Walls",
                "GUI",
            });
        }
        void unitTests()
        {
            Debug.Log("\n-----Unit-tests-----\n");
            Vec2 myVec = new Vec2(2, 3);
            Vec2 result = myVec * 3;
            Debug.Log("Scalar multiplication right ok ?: " +
             (result.x == 6 && result.y == 9 && myVec.x == 2 && myVec.y == 3));
            myVec = new Vec2(2, 3);
            result = 4 * myVec;
            Debug.Log("Scalar multiplication left ok ?: " +
             (result.x == 8 && result.y == 12 && myVec.x == 2 && myVec.y == 3));
            myVec = new Vec2(2, 3);
            Vec2 other = new Vec2(2, 3);
            result = other * myVec;
            Debug.Log("Vector multiplication ok ?: " +
             (result.x == 4 && result.y == 9 && myVec.x == 2 && myVec.y == 3));

            myVec = new Vec2(2, 3);
            other = new Vec2(1, -1);
            result = myVec + other;
            Debug.Log("Vector addition ok ?: " +
             (result.x == 3 && result.y == 2 && myVec.x == 2 && myVec.y == 3));
            myVec = new Vec2(2, 3);
            result = myVec + 4;
            Debug.Log("Addition right ok ?: " +
             (result.x == 6 && result.y == 7 && myVec.x == 2 && myVec.y == 3));
            myVec = new Vec2(2, 3);
            result = 4 + myVec;
            Debug.Log("Addition left ok ?: " +
             (result.x == 6 && result.y == 7 && myVec.x == 2 && myVec.y == 3));

            myVec = new Vec2(2, 3);
            other = new Vec2(1, -1);
            result = myVec - other;
            Debug.Log("Vector subtraction ok ?: " +
             (result.x == 1 && result.y == 4 && myVec.x == 2 && myVec.y == 3));
            myVec = new Vec2(2, 3);
            result = myVec - 4;
            Debug.Log("Subtraction right ok ?: " +
             (result.x == -2 && result.y == -1 && myVec.x == 2 && myVec.y == 3));
            myVec = new Vec2(2, 3);
            result = 4 - myVec;
            Debug.Log("Subtraction left ok ?: " +
             (result.x == 2 && result.y == 1 && myVec.x == 2 && myVec.y == 3));

            myVec = new Vec2(2, 3);
            other = new Vec2(2, 2);
            result = myVec ^ other;
            Debug.Log("Vector power ok ?: " +
             (result.x == 4 && result.y == 9 && myVec.x == 2 && myVec.y == 3));
            myVec = new Vec2(2, 3);
            result = myVec ^ 2;
            Debug.Log("Power right ok ?: " +
             (result.x == 4 && result.y == 9 && myVec.x == 2 && myVec.y == 3));
            myVec = new Vec2(2, 3);
            result = 2 ^ myVec;
            Debug.Log("Power left ok ?: " +
             (result.x == 4 && result.y == 8 && myVec.x == 2 && myVec.y == 3));

            myVec = new Vec2(2, 3);
            other = new Vec2(-5, 2);
            float floatResult = Vec2.Dot(myVec, other);
            Debug.Log("Dot product ok ?: " +
                (floatResult == -4 && myVec.x == 2 && myVec.y == 3 && other.x == -5 && other.y == 2));

            myVec = new Vec2(2, 3);
            other = new Vec2(1, 1);
            result = Vec2.Lerp(myVec, other, 0.5f);
            Debug.Log("Lerp ok?: " +
                (result.x == 1.5 && result.y == 2 && myVec.x == 2 && myVec.y == 3));

            myVec = new Vec2(3, 4);
            floatResult = myVec.length;
            Debug.Log("Lenght ok?: " +
                (floatResult == 5 && myVec.x == 3 && myVec.y == 4));

        }

        settings();
        subscriptions();
        layers();
        unitTests();
        Start();
    }
    private void Awake()
    {
        Physics.Start();
        Debug.Log("\n-----Awake-----\n");

        #region LayerInit
        AddChild(MainLayer = new Sprite("Empty"));
        AddChild(CollisionDebug = new Sprite("Empty"));
        AddChild(PostProcessing = new Sprite("Empty"));
        AddChild(EditorCollisionDebug = new Sprite("Empty"));
        AddChild(GUI = new Sprite("Empty"));
        #endregion

        //#region Post-processing
        //PostProcessing.AddChildren(new GameObject[]
        //{
        //    AssetManager.LoadAsset("screenEffect_MULT"),
        //    AssetManager.LoadAsset("screenEffect_Add")
        //});
        //#endregion

        #region GXP Asset Editor
        string[] args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            EditorCollisionDebug.AddChild(AssetManager.LoadAsset("editor"));
            MainLayer.AddChild(DocumentPointer = AssetManager.LoadAsset("pointer"));
            GUI.AddChild(SelectionBox = new Sprite("selection_box", layerMask: "GUI")
            {
                x = 7,
                y = 240,
                visible = false,
            });
            GUI.AddChild(ComponentBox = new Sprite("component_box", layerMask: "GUI")
            {
                x = 500,
                y = 100,
                visible = false,
            });
            GUI.AddChild(ComponentList = new Sprite("component_list", layerMask: "GUI")
            {
                x = 600,
                y = 100,
                visible = false,
            });

            Camera.SetLevel(MainLayer);
            Camera.AddFocus(DocumentPointer);
            Camera.SetFactor(0.1f);
            Settings.ComponentRegistrationBlock = true;

            GXPAssetEditor.Start(args[1]);
            GXPAssetEditor.SubscribeEditor();
            Debug.Log("\n-----Start-----\n");
            return;
        }
        #endregion

        #region Setup level
        Sprite level = new Sprite("Empty");
        MainLayer.AddChild(level);

        level.AddChild(AssetManager.LoadAsset("physicsTestLevel"));

        Sprite player = AssetManager.LoadAsset("player") as Sprite;
        level.AddChild(player);
        #endregion

        Camera.SetLevel(level);
        Camera.AddFocus(player);

        Debug.Log("\n-----Start-----\n");
    }
}