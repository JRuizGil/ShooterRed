using Fusion;
using UnityEngine;

/// <summary>
/// Estructura que encapsula el input del jugador
/// Se sincroniza a través de Fusion
/// </summary>
public struct PlayerNetworkInput : INetworkInput
{
    public Vector2 MoveDirection;
    public Vector2 LookDirection;
    public NetworkButtons Buttons;

    /// <summary>
    /// Recopila el input del jugador desde el teclado/ratón
    /// </summary>
    public static PlayerNetworkInput GetInput()
    {
        var input = new PlayerNetworkInput();

        // Movimiento (WASD)
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W)) moveY += 1f;
        if (Input.GetKey(KeyCode.S)) moveY -= 1f;
        if (Input.GetKey(KeyCode.A)) moveX -= 1f;
        if (Input.GetKey(KeyCode.D)) moveX += 1f;

        input.MoveDirection = new Vector2(moveX, moveY).normalized;

        // Mirada (Ratón)
        input.LookDirection = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        // Botones
        if (Input.GetKey(KeyCode.Space))
            input.Buttons.Set((int)PlayerButtons.Jump, true);

        if (Input.GetMouseButton(0))
            input.Buttons.Set((int)PlayerButtons.Fire, true);

        if (Input.GetMouseButton(1))
            input.Buttons.Set((int)PlayerButtons.Aim, true);

        if (Input.GetKeyDown(KeyCode.R))
            input.Buttons.Set((int)PlayerButtons.Reload, true);

        return input;
    }
}

/// <summary>
/// Definición de botones que puede presionar el jugador
/// </summary>
public enum PlayerButtons
{
    Jump = 0,
    Fire = 1,
    Aim = 2,
    Reload = 3,
    Ability1 = 4,
    Ability2 = 5
}

/// <summary>
/// Extensiones para NetworkButtons usando la API correcta de Fusion
/// </summary>
public static class PlayerButtonsExtensions
{
    public static void Set(this ref NetworkButtons buttons, PlayerButtons button, bool value)
    {
        buttons.Set((int)button, value);
    }

    public static bool IsPressed(this NetworkButtons buttons, PlayerButtons button)
    {
        return buttons.IsSet((int)button);
    }

    public static bool WasPressed(this NetworkButtons buttons, NetworkButtons previousButtons, PlayerButtons button)
    {
        return buttons.WasPressed(previousButtons, (int)button);
    }
}