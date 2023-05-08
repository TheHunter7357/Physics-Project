using GXPEngine.Core;
using GXPEngine.Managers;
using GXPEngine.PhysicsCore;
using System;

namespace GXPEngine
{
	/// <summary>
	/// The Game class represents the Game window.
	/// Only a single instance of this class is allowed.
	/// </summary>
	[Serializable] public class Game : GameObject
	{
		internal static Game main = null;

		private GLContext _glContext;



		public UpdateManager UpdateManager { get; private set; }

		/// <summary>
		/// Step delegate defines the signature of a method used for step callbacks, see OnBeforeStep, OnAfterStep.
		/// </summary>
		public delegate void StepDelegate();
		/// <summary>
		/// Occurs before the engine starts the new update loop. This allows you to define general manager classes that can update itself on/after each frame.
		/// </summary>
		public event StepDelegate OnBeforeStep;
		/// <summary>
		/// Occurs after the engine has finished its last update loop. This allows you to define general manager classes that can update itself on/after each frame.
		/// </summary>
		public event StepDelegate OnAfterStep;

		public delegate void RenderDelegate (GLContext glContext);
		public event RenderDelegate OnAfterRender;

		/// <summary>
		/// Sprites will be rendered if and only if they overlap with this rectangle. 
		/// Default value: (0,0,game.width,game.height). 
		/// You only need to change this when rendering to subwindows (e.g. split screen).
		/// </summary>
		/// <value>The render range.</value>
		public Rectangle RenderRange {
			get {
				return _renderRange;
			}
			set {
				_renderRange = value;
			}
		}

		/// <summary>
		/// Set this to false if you *only* want to render using your own cameras.
		/// (Don't say we didn't warn you if you end up with a black screen...)
		/// </summary>
		public bool RenderMain = true;
		public readonly bool PixelArt;

		private Rectangle _renderRange;
		
		//------------------------------------------------------------------------------------------------------------------------
		//														Game()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="GXPEngine.Game"/> class.
		/// This class represents a game window, containing an openGL view.
		/// </summary>
		/// <param name='width'>
		/// Width of the window in pixels. (As used by all the logic, e.g. the coordinate system)
		/// </param>
		/// <param name='height'>
		/// Height of the window in pixels. (As used by all the logic, e.g. the coordinate system)
		/// </param>
		/// <param name='fullScreen'>
		/// If set to <c>true</c> the application will run in fullscreen mode.
		/// </param>
		/// <param name='vSync'>
		/// If <c>true</c>, the frame rate will sync to the screen refresh rate, so setting targetFps has no effect. 
		/// (It's best to give this the same value as the 'fullScreen' parameter.)
		/// </param>
		/// <param name='realWidth'>
		/// The actual window width. By default (if passing a negative value), this is equal to the 'width' parameter, but using
		/// this parameter you can easily change the window width without having to change any other code.
		/// </param>
		/// <param name='realHeight'>
		/// The actual window height. By default (if passing a negative value), this is equal to the 'height' parameter, but using
		/// this parameter you can easily change the window height without having to change any other code.
		/// </param>
		/// <param name='pixelArt'>
		/// If <c>true</c>, textures will not be interpolated ('blurred'). This way, you can get a typical pixel art look.
		/// </param>
		public Game (int pWidth, int pHeight, bool pFullScreen, bool pVSync = true, int pRealWidth=-1, int pRealHeight=-1, bool pPixelArt=false) : base()
		{
			if (pRealWidth <= 0) {
				pRealWidth = pWidth;
			}
			if (pRealHeight <= 0) {
				pRealHeight = pHeight;
			}
			PixelArt = pPixelArt;

			if (PixelArt) {
				x = 0.001f;
				y = 0.001f;
			}		

			main = this;
			UpdateManager = new UpdateManager ();

			if (Settings.GLContext == null)
			{
				_glContext = new GLContext(this);
				_glContext.CreateWindow(pWidth, pHeight, pFullScreen, pVSync, pRealWidth, pRealHeight);
				Settings.GLContext = _glContext;
			}
			else
            {
				_glContext = Settings.GLContext;
				_glContext.ChangeOwner(this);
			}

			_renderRange = new Rectangle (0, 0, pWidth, pHeight);
			OnBeforeStep += AwakeStep;
			Add (this);
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														SetViewPort()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the rendering output view port. All rendering will be done within the given rectangle.
		/// The default setting is {0, 0, game.width, game.height}.
		/// </summary>
		/// <param name='x'>
		/// The x coordinate.
		/// </param>
		/// <param name='y'>
		/// The y coordinate.
		/// </param>
		/// <param name='width'>
		/// The new width of the viewport.
		/// </param>
		/// <param name='height'>
		/// The new height of the viewport.
		/// </param>
		public void SetViewport(int x, int y, int width, int height, bool setRenderRange=true) {
			// Translate from GXPEngine coordinates (origin top left) to OpenGL coordinates (origin bottom left):
			//Console.WriteLine ("Setting viewport to {0},{1},{2},{3}",x,y,width,height);
			_glContext.SetScissor(x, game.height - height - y, width, height);
			if (setRenderRange) {
				_renderRange = new Rectangle(x, y, width, height);
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														ShowMouse()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Shows or hides the mouse cursor.
		/// </summary>
		/// <param name='enable'>
		/// Set this to 'true' to enable the cursor.
		/// Else, set this to 'false'.
		/// </param>
		public void ShowMouse (bool enable) => _glContext.ShowCursor(enable);	
		
		//------------------------------------------------------------------------------------------------------------------------
		//														Start()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Start the game loop. Call this once at the start of your game.
		/// </summary>
		public void Start() => _glContext.Run();

		//------------------------------------------------------------------------------------------------------------------------
		//														Step()
		//------------------------------------------------------------------------------------------------------------------------
		internal void AwakeStep()
        {
			UpdateManager.AwakeStep();
			OnBeforeStep -= AwakeStep;
        }

		internal void Step()
		{
            OnBeforeStep?.Invoke();
            UpdateManager.Step();
			Physics.Step();
            OnAfterStep?.Invoke();
        }

		bool recurse = true;

		public override void Render(GLContext glContext) {
			if (RenderMain || !recurse) 
				base.Render (glContext);
			if (OnAfterRender != null && recurse)
			{
				recurse = false;
				OnAfterRender(glContext);
				recurse = true;
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														RenderSelf()
		//------------------------------------------------------------------------------------------------------------------------
		override protected void RenderSelf(GLContext glContext) {
			//empty
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														Add()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Should only be called from the GameObject class!
		/// </summary>
		internal void Add(GameObject gameObject)
		{
			UpdateManager.Add(gameObject);
		}
		//------------------------------------------------------------------------------------------------------------------------
		//														Remove()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Should only be called from the GameObject class!
		/// </summary>
		internal void Remove (GameObject gameObject)
		{
			UpdateManager.Remove (gameObject);
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														Clear()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Should only be called from the GameObject class!
		/// </summary>
		internal void Clear()
		{
			UpdateManager.Clear();
		}
		//------------------------------------------------------------------------------------------------------------------------
		//														width
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the width of the window.
		/// </summary>
		public int width => _glContext.Width; 
		
		
		//------------------------------------------------------------------------------------------------------------------------
		//														height
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the height of the window.
		/// </summary>
		public int height => _glContext.Height;

		//------------------------------------------------------------------------------------------------------------------------
		//														Destroy()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Destroys the game, only as an object.
		/// </summary>
		override public void Destroy ()
		{
			base.Destroy();
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														Exit()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Destroys the game, and closes the game window.
		/// </summary>
		public void Exit()
		{
			Physics.Stop();

			base.Destroy();
			_glContext.Close();
			_glContext.Exit();
		}

		public int currentFps {
			get {
				return _glContext.CurrentFps;
			}
		}

		public int targetFps {
			get { 
				return _glContext.TargetFps; 
			}
			set {
				_glContext.TargetFps = value;
			}
		}

		public void SetVSync(bool enableVSync) {
			_glContext.SetVSync(enableVSync);
		}

		int CountSubtreeSize(GameObject subtreeRoot) {
			int counter=1; // for the root
			foreach (GameObject child in subtreeRoot.GetChildren()) {
				counter += CountSubtreeSize (child);
			}
			return counter;
		}

		/// <summary>
		/// Returns a string with some internal engine information. Use this for debugging, especially when the game slows down.
		/// </summary>
		/// <returns>Internal engine information.</returns>
		public string GetDiagnostics() {
			string output = "";
			output += "Number of objects in hierarchy: " + CountSubtreeSize (this)+'\n';
			output += "OnBeforeStep delegates: "+(OnBeforeStep==null?0:OnBeforeStep.GetInvocationList().Length)+'\n';
			output += "OnAfterStep delegates: "+(OnAfterStep==null?0:OnAfterStep.GetInvocationList().Length)+'\n';
			output += "OnAfterRender delegates: "+(OnAfterRender==null?0:OnAfterRender.GetInvocationList().Length)+'\n';
			output += Texture2D.GetDiagnostics ();
			output += UpdateManager.GetDiagnostics (); 
			return output;
		}

        public override object Clone()
        {
			return MemberwiseClone();
        }
    }
}

