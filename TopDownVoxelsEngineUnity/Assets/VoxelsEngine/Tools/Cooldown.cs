using UnityEngine;

namespace VoxelsEngine.Tools {
    public class Cooldown {
        // Cooldown duration in seconds
        public float CooldownDuration;

        // Last action time
        private double _lastActionTime;

        public Cooldown(float cooldownDuration) {
            CooldownDuration = cooldownDuration;
        }

        // Attempt to perform action
        public bool TryPerform() {
            // Get the current time
            double currentTime = Time.timeAsDouble;

            // Check if enough time has passed since the last action
            if (currentTime - _lastActionTime >= CooldownDuration) {
                // If yes, update the last action time and return true
                _lastActionTime = currentTime;
                return true;
            }

            // If not enough time has passed, return false
            return false;
        }
    }
}