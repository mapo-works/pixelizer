using Godot;

namespace Pixelizer;

/// <summary>
/// Orthographic follow camera that keeps pixel art stable.
///
///   CameraRig (this, Node3D)   ← rotated by Pitch/Yaw, positioned on the *snapped* focus point
///   └── Camera3D               ← pushed back along the rig's local +Z by Distance
///
/// Why snapping: if the camera moves by less than one low-res pixel, every edge in the scene
/// gets re-rasterised slightly differently and the whole image "shimmers". So we move the
/// camera only in whole low-res pixel steps (measured in camera space), and hand the leftover
/// fraction to <see cref="PixelViewport"/>, which shifts the upscaled image by that amount.
/// Result: rock-solid pixels and smooth scrolling.
///
/// Planets: with <see cref="GravityCenter"/> set, "up" is away from the centre, so the whole rig is tilted to keep
/// looking straight down at the ground under the focus. The tilt follows the focus continuously, in turns too small to
/// see (<see cref="AlignStepDegrees"/>), so no frame re-rasterises the whole picture. A fixed grid can't work under a
/// turning camera, so here the rig moves in whole pixels from where it was last frame: the ground under the focus keeps
/// its pixels, and the turn only slides the picture's far parts, a fraction of a pixel per frame. Turns are parallel
/// transport (shortest rotation from the old up to the new one), so the heading doesn't swing as the camera travels.
/// </summary>
[GlobalClass]
public partial class PixelCameraRig : Node3D
{
	private const float DefaultNear = 0.05f;

	[Export] public Node3D? Target { get; set; }
	[Export] public PixelViewport? PixelViewport { get; set; }

	/// <summary>Planet centre; null on flat maps (world +Y is up, the rig behaves as if this didn't exist).</summary>
	[Export] public Node3D? GravityCenter { get; set; }

	/// <summary>How far (degrees around the planet centre) the focus may travel before the rig re-aligns to the ground. Keep it tiny: a big step is a visible pop.</summary>
	[Export(PropertyHint.Range, "0,5,0.001")] public float AlignStepDegrees { get; set; } = 0.001f;

	/// <summary>Turned with the rig on planets so the ground under the camera is always lit from the same side.</summary>
	[Export] public DirectionalLight3D? Sun { get; set; }

	/// <summary>World units → low-res pixels. Every asset should respect this density.</summary>
	[Export] public float PixelsPerUnit { get; set; } = 16f;

	/// <summary>Camera tilt in degrees (negative = looking down).</summary>
	[Export(PropertyHint.Range, "-90,-5,0.5")] public float Pitch { get; set; } = -30f;

	/// <summary>Starting yaw in degrees. 45° gives the classic isometric-ish view.</summary>
	[Export] public float Yaw { get; set; } = 45f;

	/// <summary>How far the camera sits behind the focus point. Only affects clipping, not size.</summary>
	[Export] public float Distance { get; set; } = 40f;

	/// <summary>Higher = tighter follow. Frame-rate independent.</summary>
	[Export] public float FollowSharpness { get; set; } = 8f;

	/// <summary>Degrees per second when turning to a new yaw (Q/E).</summary>
	[Export] public float RotateSpeed { get; set; } = 360f;

	[Export] public bool Snapping { get; set; } = true;

	/// <summary>
	/// Off: the camera stops following <see cref="Target"/> and stays put until <see cref="Focus"/> is moved
	/// (e.g. edge panning). Turning it back on glides the camera home at <see cref="FollowSharpness"/>.
	/// </summary>
	[Export] public bool FollowTarget { get; set; } = true;

	/// <summary>Cutaway: how far in front of the target (towards the camera) the cut plane sits. Must clear the character's head.</summary>
	[Export] public float CutMargin { get; set; } = 1.25f;

	/// <summary>Cutaway: how many metres in front of its final position the cut plane starts sweeping in.</summary>
	[Export] public float CutSweep { get; set; } = 12f;

	public Camera3D Camera { get; private set; } = null!;

	/// <summary>
	/// Cutaway amount, 0 = off, 1 = everything nearer than the target (plus <see cref="CutMargin"/>) is clipped away.
	/// It moves the camera's near plane, so it cuts every mesh without touching any shader, and an orthographic
	/// camera's picture doesn't change in X/Y when it moves, so pixels stay stable.
	/// </summary>
	public float CutAmount { get; set; }

	/// <summary>True while the near plane is cutting the world, so mouse rays start at the cut surface.</summary>
	public bool IsCutting => CutAmount > 0f;

	/// <summary>Current yaw in radians, around the rig's <see cref="Up"/>.</summary>
	public float YawRadians => Mathf.DegToRad(_currentYaw);

	/// <summary>The "up" the camera is aligned to: world +Y on flat maps, the planet's up under the focus otherwise.</summary>
	public Vector3 Up => _alignedUp;

	/// <summary>Camera right, lying in the ground plane (perpendicular to <see cref="Up"/>). For camera-relative input.</summary>
	public Vector3 GroundRight => HeadingBasis().X;

	/// <summary>Where the camera looks, flattened onto the ground plane. For camera-relative input.</summary>
	public Vector3 GroundForward => -HeadingBasis().Z;

	/// <summary>The point the camera looks at, before pixel snapping. Set it to move a camera that isn't following.</summary>
	public Vector3 Focus
	{
		get => _focus;
		set => _focus = value;
	}

	private Vector3 _focus;
	private Quaternion _align = Quaternion.Identity; // rotates world +Y onto _alignedUp
	private Vector3 _alignedUp = Vector3.Up;
	private Vector3 _rigPosition; // where the last frame put the rig (planets snap from here)
	private Basis _sunBasis;
	private float _currentYaw;
	private float _targetYaw;
	private float _shakePixels;
	private float _shakeSeconds;
	private float _shakeLeft;

	public override void _Ready()
	{
		// We set the transform ourselves in _Process, so the engine must not interpolate it.
		PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;

		Camera = GetNode<Camera3D>("Camera3D");
		Camera.Projection = Camera3D.ProjectionType.Orthogonal;
		Camera.KeepAspect = Camera3D.KeepAspectEnum.Height; // Size = visible height
		Camera.Position = new Vector3(0, 0, Distance);
		Camera.Rotation = Vector3.Zero;
		Camera.Near = DefaultNear;
		Camera.Far = Distance * 3f;
		Camera.Current = true;

		_currentYaw = _targetYaw = Yaw;
		_focus = Target?.GlobalPosition ?? GlobalPosition;
		if (Sun is not null) _sunBasis = Sun.GlobalBasis;
	}

	private Basis HeadingBasis() => new Basis(_align) * Basis.FromEuler(new Vector3(0f, Mathf.DegToRad(_currentYaw), 0f));

	private bool OnPlanet => GravityCenter is not null && IsInstanceValid(GravityCenter);

	/// <summary>Re-aligns the rig to the planet once the focus has moved more than a step from the last alignment.</summary>
	private void UpdateAlignment()
	{
		if (!OnPlanet)
		{
			if (_align == Quaternion.Identity) return;
			_align = Quaternion.Identity;
			_alignedUp = Vector3.Up;
			ApplySun();
			return;
		}

		Vector3 offset = _focus - GravityCenter!.GlobalPosition;
		if (offset.LengthSquared() < 1e-6f) return;
		Vector3 trueUp = offset.Normalized();
		// From where _align really points, not _alignedUp, so thousands of tiny turns can't add up to a tilt.
		Vector3 current = (_align * Vector3.Up).Normalized();
		if (current.AngleTo(trueUp) <= Mathf.DegToRad(AlignStepDegrees)) return;

		// One turn, however far: tiny while scrolling, the whole way after a jump (teleport, load).
		_align = (ShortestArc(current, trueUp) * _align).Normalized();
		_alignedUp = trueUp;
		ApplySun();
	}

	/// <summary>
	/// The shortest rotation between two unit vectors. <c>new Quaternion(from, to)</c> won't do: it returns no rotation
	/// at all below about 0.03°, which is more than the camera turns in a frame.
	/// </summary>
	private static Quaternion ShortestArc(Vector3 from, Vector3 to)
	{
		Vector3 axis = from.Cross(to);
		float sin = axis.Length();
		float cos = from.Dot(to);
		if (sin > 1e-6f) return new Quaternion(axis / sin, Mathf.Atan2(sin, cos));
		if (cos > 0f) return Quaternion.Identity;
		// Exactly opposite: half a turn round any axis across them.
		Vector3 across = from.Cross(Mathf.Abs(from.X) < 0.9f ? Vector3.Right : Vector3.Forward).Normalized();
		return new Quaternion(across, Mathf.Pi);
	}

	/// <summary>
	/// Raised with the sun's new world rotation whenever the rig turns <see cref="Sun"/> (on planets, as the focus
	/// travels; and when <see cref="SunBasis"/> is set). For shaders that need the sun's direction themselves.
	/// </summary>
	public event System.Action<Basis>? SunTurned;

	/// <summary>The sun's rotation before the rig turns it with the planet (read from <see cref="Sun"/> at start).</summary>
	public Basis SunBasis
	{
		get => _sunBasis;
		set
		{
			_sunBasis = value;
			ApplySun();
		}
	}

	private void ApplySun()
	{
		if (Sun is null || !IsInstanceValid(Sun)) return;
		// The rotation as worked out, not read back: a sun not in the tree yet reads as unturned.
		Basis turned = new Basis(_align) * _sunBasis;
		SetGlobalBasis(Sun, turned);
		SunTurned?.Invoke(turned);
	}

	/// <summary>
	/// Sets <paramref name="node"/>'s world rotation, also before it is in the tree (a loader that lights the
	/// scene in its own <c>_EnterTree</c>, before the sun after it has entered): then worked out from the parents' own
	/// rotations, as <c>GlobalBasis</c> would once it is in.
	/// </summary>
	public static void SetGlobalBasis(Node3D node, Basis basis)
	{
		if (node.IsInsideTree())
		{
			node.GlobalBasis = basis;
			return;
		}
		Basis parents = Basis.Identity;
		if (!node.TopLevel)
			for (Node3D? p = node.GetParent() as Node3D; p is not null; p = p.TopLevel ? null : p.GetParent() as Node3D)
				parents = p.Basis * parents;
		node.Basis = parents.Inverse() * basis;
	}

	/// <summary>
	/// World-space ray through the mouse cursor. The game is drawn into a small SubViewport and shown upscaled, so the
	/// window position is first mapped to SubViewport pixels. False when the cursor is off the game image.
	/// </summary>
	public bool TryGetMouseRay(out Vector3 origin, out Vector3 direction)
	{
		origin = direction = default;
		if (PixelViewport is null || !PixelViewport.TryGetMouseViewportPosition(out Vector2 pixel)) return false;
		origin = Camera.ProjectRayOrigin(pixel);
		direction = Camera.ProjectRayNormal(pixel);
		return true;
	}

	/// <summary>Turn the camera by a number of 90° steps (e.g. -1 / +1).</summary>
	public void RotateSteps(int steps) => _targetYaw += 90f * steps;

	/// <summary>The yaw the camera is turning to (or holds), degrees.</summary>
	public float TargetYawDegrees => _targetYaw;

	/// <summary>
	/// Turns to <paramref name="degrees"/>: at once (orbiting with the mouse, restoring a saved view), else at
	/// <see cref="RotateSpeed"/> the shorter way round. Any angle works; pixels are stable again once it stops.
	/// </summary>
	public void SetYaw(float degrees, bool instant = false)
	{
		if (instant)
		{
			_currentYaw = _targetYaw = degrees;
			return;
		}
		_targetYaw = _currentYaw + Mathf.Wrap(degrees - _currentYaw, -180f, 180f);
	}

	/// <summary>Shakes the picture by up to <paramref name="pixels"/> low-res pixels, fading out. A weaker shake never cuts a stronger one short.</summary>
	public void Shake(float pixels, float seconds)
	{
		if (seconds <= 0f || (_shakeLeft > 0f && pixels < ShakeReach()))
			return;
		_shakePixels = pixels;
		_shakeSeconds = seconds;
		_shakeLeft = seconds;
	}

	private float ShakeReach()
	{
		float strength = _shakeLeft / _shakeSeconds;
		return _shakePixels * strength * strength;
	}

	private void UpdateShake(float dt, float texel)
	{
		if (_shakeLeft <= 0f) return;
		_shakeLeft -= dt;
		if (_shakeLeft <= 0f)
		{
			Camera.HOffset = 0f;
			Camera.VOffset = 0f;
			return;
		}

		// Whole low-res pixels only. Anything less re-rasterises every edge and the picture shimmers, which is
		// the very thing snapping exists to prevent.
		float reach = ShakeReach();
		Camera.HOffset = Mathf.Round((float)GD.RandRange(-reach, reach)) * texel;
		Camera.VOffset = Mathf.Round((float)GD.RandRange(-reach, reach)) * texel;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// 1. Smoothly follow the target. GetGlobalTransformInterpolated() returns the position
		//    the player is *drawn* at when physics interpolation is on (not the raw physics one).
		if (Target is not null && FollowTarget)
		{
			Vector3 targetPos = Target.GetGlobalTransformInterpolated().Origin;
			float t = 1f - Mathf.Exp(-FollowSharpness * dt);
			_focus = _focus.Lerp(targetPos, t);
		}

		_currentYaw = Mathf.MoveToward(_currentYaw, _targetYaw, RotateSpeed * dt);
		UpdateAlignment();
		var basis = new Basis(_align) * Basis.FromEuler(new Vector3(Mathf.DegToRad(Pitch), Mathf.DegToRad(_currentYaw), 0f));

		// 2. Size the orthographic camera so 1 world unit = PixelsPerUnit low-res pixels.
		int renderHeight = PixelViewport is not null
			? PixelViewport.SubViewport.Size.Y
			: (int)GetViewport().GetVisibleRect().Size.Y;
		float resolutionFactor = PixelViewport?.ResolutionFactor ?? 1f;
		Camera.Size = renderHeight / (PixelsPerUnit * resolutionFactor);
		float texel = Camera.Size / renderHeight; // world units covered by one rendered pixel

		// 3. Snap the focus point to the pixel grid *in camera space*.
		//    basis is orthonormal, so its transpose is its inverse (world → camera space).
		//    Flat maps: a grid fixed to the world origin. Planets: the basis turns all the time, which would swing such
		//    a grid across the ground, so the grid hangs on the rig's last position instead: every move is whole pixels.
		Vector3 anchor = OnPlanet ? _rigPosition : Vector3.Zero;
		Vector3 local = basis.Transposed() * (_focus - anchor);
		Vector3 snapped = local;
		if (Snapping)
		{
			snapped.X = Mathf.Snapped(local.X, texel);
			snapped.Y = Mathf.Snapped(local.Y, texel);
		}

		_rigPosition = anchor + basis * snapped;
		GlobalTransform = new Transform3D(basis, _rigPosition);

		// 4. Report the leftover as a fraction of a low-res pixel. Screen Y points down,
		//    camera-space Y points up, hence the sign flip.
		Vector2 subPixel = new((local.X - snapped.X) / texel, -(local.Y - snapped.Y) / texel);
		PixelViewport?.SetSubPixelOffset(Snapping ? subPixel : Vector2.Zero);

		UpdateCut(basis);
		UpdateShake(dt, texel);
	}

	private void UpdateCut(Basis basis)
	{
		float near = DefaultNear;
		if (CutAmount > 0f && Target is not null)
		{
			// The camera sits Distance behind the rig along camera-space +Z and looks down -Z.
			float depth = (basis.Transposed() * (_rigPosition - Target.GetGlobalTransformInterpolated().Origin)).Z + Distance;
			near = Mathf.Max(DefaultNear, depth - CutMargin - CutSweep * (1f - CutAmount));
		}

		if (!Mathf.IsEqualApprox(Camera.Near, near))
			Camera.Near = near;
	}
}
