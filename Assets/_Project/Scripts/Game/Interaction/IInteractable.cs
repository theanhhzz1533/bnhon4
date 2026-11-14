using Bnhon.Game.Player;

namespace Bnhon.Game.Interaction
{
    /// <summary>Anything the player can use with the E key. Needs a Collider2D on the same GameObject.</summary>
    public interface IInteractable
    {
        /// <summary>Text shown on the HUD while the player is in range.</summary>
        string Prompt { get; }

        void Interact(PlayerController player);
    }
}
