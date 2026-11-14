using UnityEngine;

namespace Bnhon.Game.Player
{
    /// <summary>Follows the player and leans toward where they aim.</summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private float smoothing = 8f;
        [SerializeField] private float aimLead = 1.5f;

        public PlayerController Target { get; set; }

        private void LateUpdate()
        {
            if (Target == null) return;

            Vector2 focus = (Vector2)Target.transform.position + Target.AimDirection * aimLead;
            Vector3 goal = new Vector3(focus.x, focus.y, transform.position.z);
            float t = 1f - Mathf.Exp(-smoothing * UnityEngine.Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, goal, t);
        }

        public void SnapToTarget()
        {
            if (Target == null) return;
            Vector3 p = Target.transform.position;
            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }
    }
}
