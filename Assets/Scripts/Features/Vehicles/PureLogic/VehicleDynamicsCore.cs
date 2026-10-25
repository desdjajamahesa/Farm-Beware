using System;

namespace FarmBeware.Features.Vehicles.PureLogic
{
    /// <summary>
    /// Pure C# POCO class computing vehicle kinematics, throttle curves, Ackermann steering,
    /// lateral grip, drift slip, suspension pitch/roll, and gear state.
    /// Zero Unity lifecycle dependencies, Zero-GC allocations per frame.
    /// </summary>
    public class VehicleDynamicsCore
    {
        // Vehicle Physical Dimensions
        public float Wheelbase { get; set; } = 3.0f;           // meters between front & rear axles
        public float TrackWidth { get; set; } = 1.9f;          // meters between left & right wheels

        // Speed & Acceleration Limits
        public float MaxForwardSpeed { get; set; } = 20.0f;     // ~72 km/h
        public float MaxReverseSpeed { get; set; } = 7.0f;       // ~25 km/h
        public float AccelerationRate { get; set; } = 14.0f;     // m/s^2
        public float BrakingRate { get; set; } = 26.0f;          // m/s^2
        public float RollingResistance { get; set; } = 3.5f;     // m/s^2
        public float DownforceFactor { get; set; } = 0.8f;

        // Steering Limits & Ackermann Geometry
        public float BaseSteerAngle { get; set; } = 35.0f;        // degrees at low speed
        public float HighSpeedSteerAngle { get; set; } = 18.0f;   // degrees at max speed
        public float SteerResponseSpeed { get; set; } = 10.0f;    // rate of steering angle change
        public float SpeedSensitiveThreshold { get; set; } = 12.0f; // speed at which steer angle reduces

        // Tire Friction & Drift
        public float NormalTireGrip { get; set; } = 0.92f;        // High lateral adhesion
        public float DriftTireGrip { get; set; } = 0.48f;         // Controlled slip during handbrake
        public float HandbrakeBrakingRate { get; set; } = 30.0f;  // Rapid stop under handbrake

        // Suspension Visuals
        public float PitchPerAccel { get; set; } = 0.14f;         // Degrees squat/dip per m/s^2
        public float MaxPitchAngle { get; set; } = 3.5f;          // Max visual pitch
        public float RollPerLateralAccel { get; set; } = 0.16f;   // Degrees lean per m/s^2
        public float MaxRollAngle { get; set; } = 4.5f;           // Max visual roll

        // Gear Enums
        public enum GearState { Park, Reverse, Drive }

        /// <summary>
        /// Calculates target forward velocity based on throttle/brake input and current speed.
        /// Implements realistic 'Brake-Before-Reverse' transmission behavior:
        /// Moving forward + [S] = Foot Brake (slows to stop).
        /// Stopped + [S] = Reverse gear.
        /// </summary>
        public float CalculateForwardSpeed(float throttleInput, float currentSpeed, bool handbrake, float dt)
        {
            if (dt <= 0.0001f) return currentSpeed;

            // Handbrake overrides throttle to bring vehicle to a rapid stop
            if (handbrake)
            {
                if (currentSpeed > 0f)
                {
                    return Math.Max(0f, currentSpeed - HandbrakeBrakingRate * dt);
                }
                else if (currentSpeed < 0f)
                {
                    return Math.Min(0f, currentSpeed + HandbrakeBrakingRate * dt);
                }
                return 0f;
            }

            // Case A: Vehicle is currently moving FORWARD (speed > 0.2 m/s)
            if (currentSpeed > 0.2f)
            {
                if (throttleInput < -0.05f)
                {
                    // [S] is Foot Brake: decelerate towards 0, do not reverse in the same tick
                    float absInput = Math.Abs(throttleInput);
                    return Math.Max(0f, currentSpeed - BrakingRate * absInput * dt);
                }
                if (throttleInput > 0.05f)
                {
                    // [W] is Forward Throttle
                    float targetSpeed = MaxForwardSpeed * throttleInput;
                    return Math.Min(targetSpeed, currentSpeed + AccelerationRate * throttleInput * dt);
                }
                // Coasting: natural rolling resistance towards 0
                return Math.Max(0f, currentSpeed - RollingResistance * dt);
            }

            // Case B: Vehicle is currently REVERSING (speed < -0.2 m/s)
            if (currentSpeed < -0.2f)
            {
                if (throttleInput > 0.05f)
                {
                    // [W] is Foot Brake while in reverse: decelerate towards 0
                    return Math.Min(0f, currentSpeed + BrakingRate * throttleInput * dt);
                }
                if (throttleInput < -0.05f)
                {
                    // [S] is Reverse Throttle
                    float absInput = Math.Abs(throttleInput);
                    float targetSpeed = -MaxReverseSpeed * absInput;
                    return Math.Max(targetSpeed, currentSpeed - (AccelerationRate * 0.65f) * absInput * dt);
                }
                // Coasting in reverse: rolling resistance towards 0
                return Math.Min(0f, currentSpeed + RollingResistance * dt);
            }

            // Case C: Vehicle is virtually STOPPED (|speed| <= 0.2 m/s)
            if (throttleInput > 0.05f)
            {
                // Shift into Drive, accelerate forward
                return Math.Min(MaxForwardSpeed * throttleInput, currentSpeed + AccelerationRate * throttleInput * dt);
            }
            if (throttleInput < -0.05f)
            {
                // Shift into Reverse, accelerate backward
                float absInput = Math.Abs(throttleInput);
                return Math.Max(-MaxReverseSpeed * absInput, currentSpeed - (AccelerationRate * 0.65f) * absInput * dt);
            }

            // Parked / Resting
            return 0f;
        }

        /// <summary>
        /// Computes vehicle yaw rate in degrees per second using the Ackermann bicycle model.
        /// Returns strictly 0.0 when longitudinal speed is near zero (|speed| <= 0.08 m/s),
        /// completely preventing stationary pivot/tank spinning.
        /// </summary>
        public float CalculateAckermannYawRate(float currentSpeed, float steerAngleDeg, bool handbrake, float lateralSpeed, float dt)
        {
            // 1. Strict stationary guard: A stopped car cannot turn its body in place
            if (Math.Abs(currentSpeed) < 0.08f || Math.Abs(steerAngleDeg) < 0.05f)
            {
                return 0f;
            }

            float wb = Math.Max(0.5f, Wheelbase);
            // Convert steer angle to radians
            double steerRad = (steerAngleDeg * Math.PI) / 180.0;
            double tanSteer = Math.Tan(steerRad);

            // Kinematic bicycle curvature: kappa = tan(delta) / L
            // Yaw rate omega = v * kappa (radians/sec)
            double yawRateRad = (currentSpeed / wb) * tanSteer;
            float yawRateDeg = (float)(yawRateRad * (180.0 / Math.PI));

            // 2. Handbrake dynamic oversteer drift boost at speed
            if (handbrake && Math.Abs(currentSpeed) > 2.5f)
            {
                float driftMultiplier = 1.0f + Math.Min(1.2f, Math.Abs(lateralSpeed) * 0.25f);
                yawRateDeg *= driftMultiplier;
            }

            return yawRateDeg;
        }

        /// <summary>
        /// Calculates speed-sensitive steering angle with smooth interpolation and caster return.
        /// Front wheels can turn up to BaseSteerAngle even when stopped (dry steering feedback),
        /// but maximum angle is tightened at higher speeds to prevent rollovers.
        /// </summary>
        public float CalculateSteeringAngle(float steerInput, float currentSteerAngle, float currentSpeed, float dt)
        {
            float absSpeed = Math.Abs(currentSpeed);
            float speedRatio = Math.Min(1.0f, absSpeed / SpeedSensitiveThreshold);
            float allowedMaxAngle = BaseSteerAngle - (BaseSteerAngle - HighSpeedSteerAngle) * speedRatio;

            float targetAngle = steerInput * allowedMaxAngle;

            // Caster self-centering effect: returns to 0 faster when moving at speed
            float responseMultiplier = 1.0f;
            if (Math.Abs(steerInput) < 0.05f)
            {
                responseMultiplier = 1.0f + Math.Min(1.5f, absSpeed / 8.0f);
            }

            float step = SteerResponseSpeed * responseMultiplier * dt * BaseSteerAngle;

            if (currentSteerAngle < targetAngle)
            {
                return Math.Min(targetAngle, currentSteerAngle + step);
            }
            if (currentSteerAngle > targetAngle)
            {
                return Math.Max(targetAngle, currentSteerAngle - step);
            }

            return targetAngle;
        }

        /// <summary>
        /// Calculates lateral friction grip multiplier.
        /// Returns lower grip when handbrake is engaged or during extreme power-slides.
        /// </summary>
        public float CalculateLateralGrip(bool handbrake, float lateralSpeed)
        {
            if (handbrake) return DriftTireGrip;

            float absLatSpeed = Math.Abs(lateralSpeed);
            if (absLatSpeed > 6.0f)
            {
                // Dynamic slip under high lateral velocity
                float slipRatio = Math.Min(1.0f, (absLatSpeed - 6.0f) / 8.0f);
                return NormalTireGrip - (NormalTireGrip - DriftTireGrip) * slipRatio * 0.6f;
            }

            return NormalTireGrip;
        }

        /// <summary>
        /// Calculates visual suspension pitch (squat under accel, dip under braking).
        /// </summary>
        public float CalculateSuspensionPitch(float accelMps2, float currentPitch, float dt)
        {
            float targetPitch = Math.Max(-MaxPitchAngle, Math.Min(MaxPitchAngle, -accelMps2 * PitchPerAccel));
            float smooth = Math.Min(1.0f, 8.0f * dt);
            return currentPitch + (targetPitch - currentPitch) * smooth;
        }

        /// <summary>
        /// Calculates visual suspension roll (body lean into / out of turns).
        /// </summary>
        public float CalculateSuspensionRoll(float lateralAccelMps2, float currentRoll, float dt)
        {
            float targetRoll = Math.Max(-MaxRollAngle, Math.Min(MaxRollAngle, -lateralAccelMps2 * RollPerLateralAccel));
            float smooth = Math.Min(1.0f, 8.0f * dt);
            return currentRoll + (targetRoll - currentRoll) * smooth;
        }

        /// <summary>
        /// Computes cumulative wheel spin angle (in degrees) based on linear travel distance.
        /// </summary>
        public float CalculateWheelSpinAngle(float speed, float wheelRadius, float currentAngle, float dt)
        {
            if (wheelRadius <= 0.05f) wheelRadius = 0.36f;
            float distance = speed * dt;
            float degrees = (distance / (2f * (float)Math.PI * wheelRadius)) * 360f;
            return (currentAngle + degrees) % 360f;
        }

        /// <summary>
        /// Determines current gear state for HUD readout.
        /// </summary>
        public GearState DetermineGear(float speed, float throttleInput)
        {
            if (speed < -0.2f || (speed <= 0.2f && throttleInput < -0.1f))
            {
                return GearState.Reverse;
            }
            if (Math.Abs(speed) < 0.2f && Math.Abs(throttleInput) < 0.05f)
            {
                return GearState.Park;
            }
            return GearState.Drive;
        }
    }
}
