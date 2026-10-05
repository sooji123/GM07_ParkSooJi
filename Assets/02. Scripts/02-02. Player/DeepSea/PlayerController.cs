using System;
using UnityEngine;

public enum PlayerState { Idle, Move, Aim, Attack }

[RequireComponent(typeof(PlayerInputReader), typeof(PlayerMotor), typeof(PlayerAim))]
[RequireComponent(typeof(HarpoonController))]
public sealed class PlayerController : MonoBehaviour
{
    public PlayerState State { get; private set; } = PlayerState.Idle;
    public event Action<PlayerState> StateChanged;
    private PlayerInputReader input;
    private PlayerMotor motor;
    private PlayerAim aim;
    private HarpoonController harpoon;
    private int idleEnteredFrame = -1;

    private void Awake()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int fishLayer = LayerMask.NameToLayer("Fish");
        if (playerLayer >= 0)
            gameObject.layer = playerLayer;
        if (playerLayer >= 0 && fishLayer >= 0)
            Physics2D.IgnoreLayerCollision(playerLayer, fishLayer, true);

        input = GetComponent<PlayerInputReader>();
        motor = GetComponent<PlayerMotor>();
        aim = GetComponent<PlayerAim>();
        harpoon = GetComponent<HarpoonController>();
    }

    private void OnEnable() => harpoon.AttackFinished += OnAttackFinished;

    private void Update()
    {
        input.ReadInput();
        // 공격 중과 공격 완료 프레임의 입력을 무시하며, 재조준 입력을 예약하지 않습니다.
        if (State == PlayerState.Attack || idleEnteredFrame == Time.frameCount) return;

        if (State == PlayerState.Aim)
        {
            if (!input.AimHeld)
            {
                ChangeState(PlayerState.Idle);
                return;
            }
            bool validAim = aim.UpdateDirection(input.MousePosition);
            if (validAim && input.FirePressed)
            {
                ChangeState(PlayerState.Attack);
                if (!harpoon.Fire(aim.Origin, aim.Direction)) ChangeState(PlayerState.Idle);
            }
            return;
        }

        if (input.AimPressed && input.AimHeld)
        {
            ChangeState(PlayerState.Aim);
            bool validAim = aim.UpdateDirection(input.MousePosition);
            // 같은 입력 프레임에 두 마우스 버튼을 누르는 경우도 처리합니다.
            if (validAim && input.FirePressed)
            {
                ChangeState(PlayerState.Attack);
                if (!harpoon.Fire(aim.Origin, aim.Direction)) ChangeState(PlayerState.Idle);
            }
            return;
        }
        ChangeState(input.Move.sqrMagnitude > 0f ? PlayerState.Move : PlayerState.Idle);
    }

    private void FixedUpdate()
    {
        if (State == PlayerState.Move && Application.isFocused) motor.Move(input.Move, input.SprintHeld);
        else motor.Stop();
    }

    private void ChangeState(PlayerState next)
    {
        if (State == next) return;
        State = next;
        if (next != PlayerState.Move) motor.Stop();
        aim.SetVisible(next == PlayerState.Aim, next == PlayerState.Attack);
        if (next == PlayerState.Idle) idleEnteredFrame = Time.frameCount;
        StateChanged?.Invoke(next);
    }

    private void OnAttackFinished() => ChangeState(PlayerState.Idle);

    private void OnDisable()
    {
        harpoon.AttackFinished -= OnAttackFinished;
        harpoon.Cancel();
        motor.Stop();
        aim.SetVisible(false);
        ChangeState(PlayerState.Idle);
    }
}
