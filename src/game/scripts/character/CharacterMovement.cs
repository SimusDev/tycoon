using Godot;
using System;
using System.Text.RegularExpressions;

[GlobalClass]
public partial class CharacterMovement : Node
{
    [Export] public StateMachine StateMachine { get; set; }

    [ExportGroup("Settings")]
    [Export] public float JumpForce { get; set; } = 7.0f;
    [Export] public float SpeedMultiplier { get; set; } = 1.0f;
    [Export] public float CrouchedSpeed { get; set; } = 2.5f;
    [Export] public float WalkSpeed { get; set; } = 4.5f;
    [Export] public float SprintSpeed { get; set; } = 9.0f;
    [Export] public float Acceleration { get; set; } = 25.0f;
    [Export] public float Friction { get; set; } = 25.0f;

    [ExportSubgroup("Keys", "key_")]
    [Export] public StringName KeyForward { get; set; } = "move_forward";
    [Export] public StringName KeyBackward { get; set; } = "move_backward";
    [Export] public StringName KeyLeft { get; set; } = "move_left";
    [Export] public StringName KeyRight { get; set; } = "move_right";
    [Export] public StringName KeyCrouch { get; set; } = "crouch";
    [Export] public StringName KeySprint { get; set; } = "sprint";

    [ExportGroup("Custom", "custom_")]
    [Export] public CharacterBody3D CustomCharacter
    {
        get => _customCharacter;
        set
        {
            _customCharacter = value;
            BindCharacter();
        }
    }

    

    private CharacterBody3D _customCharacter;
    private CharacterBody3D _character;
    private bool _inputEnabled = true;

    public override void _Ready()
    {
        BindCharacter();

        bool enabled = IsMultiplayerAuthority() && !Engine.IsEditorHint();
        SetProcess(enabled);
        SetPhysicsProcess(enabled);
        SetProcessInput(enabled);

        // SimusDev.ui.interface_opened_or_closed.connect(_update_input_status)
        //InterfaceStack.OnActiveChanged += UpdateInputStatus;
        //UpdateInputStatus();
    }

    private void UpdateInputStatus()
    {
        //_inputEnabled = !InterfaceStack.HasActive();
    }

    private void BindCharacter()
    {
        if (_customCharacter == null)
        {
            var target = GetParent();
            if (target is not CharacterBody3D)
            {
                GD.PushError("[BaseCharacterMovement] '_bind_character' target is not CharacterBody3D");
                return;
            }

            _character = GetParent<CharacterBody3D>();
        }
        else
        {
            _character = _customCharacter;
        }

        UpdateConfigurationWarnings();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_character == null || StateMachine == null) return;

        string currentState = StateMachine.CurrentState();
        if (currentState == null) return;

        float deltaF = (float)delta;
        double gravity = (double)ProjectSettings.GetSetting("physics/3d/default_gravity");

        if (!_character.IsOnFloor())
        {
            _character.Velocity = new Vector3(
                _character.Velocity.X,
                _character.Velocity.Y - (float)ProjectSettings.GetSetting("physics/3d/default_gravity") * 2.24f * deltaF,
                _character.Velocity.Z
            );
        }
        else if (Input.IsActionJustPressed("jump") && _inputEnabled)
        {
            _character.Velocity = new Vector3(
                _character.Velocity.X,
                JumpForce,
                _character.Velocity.Z
            );
        }

        Vector2 inputDir = Input.GetVector(KeyLeft, KeyRight, KeyForward, KeyBackward);
        Vector3 direction;

        if (_inputEnabled)
        {
            direction = (_character.GlobalTransform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        }
        else
        {
            direction = Vector3.Zero;
        }

        float speed;
        switch (currentState)
        {
            case "CrouchedWalking":
                speed = CrouchedSpeed;
                break;
            case "Walking":
                speed = WalkSpeed;
                break;
            case "Running":
                speed = SprintSpeed;
                break;
            default:
                speed = 0.0f;
                break;

        }

        if (_character.IsOnFloor())
        {
            Vector3 targetVel = direction * speed * SpeedMultiplier;
            float weight = direction.Length() > 0 ? Acceleration : Friction;

            _character.Velocity = new Vector3(
                Mathf.Lerp(_character.Velocity.X, targetVel.X, weight * deltaF),
                _character.Velocity.Y,
                Mathf.Lerp(_character.Velocity.Z, targetVel.Z, weight * deltaF)
            );
        }
        else
        {
            if (direction.Length() > 0)
            {
                float airAccel = Acceleration * 0.15f;
                Vector3 accelForce = direction * airAccel * deltaF;

                Vector2 horizontalVel = new Vector2(_character.Velocity.X, _character.Velocity.Z);
                if (horizontalVel.Dot(new Vector2(direction.X, direction.Z)) < speed)
                {
                    _character.Velocity = new Vector3(
                        _character.Velocity.X + accelForce.X,
                        _character.Velocity.Y,
                        _character.Velocity.Z + accelForce.Z
                    );
                }
            }

            float airResistance = 0.998f;
            _character.Velocity = new Vector3(
                _character.Velocity.X * airResistance,
                _character.Velocity.Y,
                _character.Velocity.Z * airResistance
            );
        }

        _character.MoveAndSlide();
    }

    public override void _Process(double delta)
    {
        HandleStateTransitions();
    }

    private void HandleStateTransitions()
    {
        Vector2 inputDir = Input.GetVector(KeyLeft, KeyRight, KeyForward, KeyBackward);
        bool isMoving = inputDir.Length() > 0.1f;
        bool wantsSprint = Input.IsActionPressed(KeySprint) &&
                          -(_character.Velocity.Normalized() * _character.Transform.Basis).Z >= -0.1f;
        bool wantsCrouch = Input.IsActionPressed(KeyCrouch);

        StringName targetStateName = "Idle";

        if (!_character.IsOnFloor())
        {
            targetStateName = "Floating";
        }
        else if (wantsCrouch)
        {
            targetStateName = isMoving ? "CrouchedWalking" : "Crouched";
        }
        else if (isMoving)
        {
            targetStateName = wantsSprint ? "Running" : "Walking";
        }
        else
        {
            targetStateName = "Idle";
        }

        if (StateMachine != null)
        {
            StateMachine.SwitchState(targetStateName);
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        var target = _customCharacter != null ? (Node)_customCharacter : GetParent();

        if (target is not CharacterBody3D)
        {
            warnings.Add("Parent must be a 'CharacterBody3D'");
        }

        return warnings.ToArray();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationParented || what == NotificationUnparented)
        {
            UpdateConfigurationWarnings();
        }
    }
}