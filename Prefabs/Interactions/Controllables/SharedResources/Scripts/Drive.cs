﻿namespace VRTK.Prefabs.Interactions.Controllables
{
    using UnityEngine;
    using Zinnia.Data.Type;
    using Zinnia.Process;
    using Zinnia.Extension;
    using Zinnia.Data.Attribute;

    /// <summary>
    /// The basis for a mechanism to drive motion on a control.
    /// </summary>
    /// <typeparam name="TFacade">The <see cref="DriveFacade{TDrive, TSelf}"/> to be used with the drive.</typeparam>
    /// <typeparam name="TSelf">The actual concrete impemetation of the drive being used.</typeparam>
    public abstract class Drive<TFacade, TSelf> : MonoBehaviour, IProcessable
         where TFacade : DriveFacade<TSelf, TFacade> where TSelf : Drive<TFacade, TSelf>
    {
        #region Facade Settings
        /// <summary>
        /// The public interface facade.
        /// </summary>
        [Header("Facade Settings"), Tooltip("The public interface facade."), InternalSetting, SerializeField]
        protected TFacade facade;
        #endregion

        #region Threshold Settings
        /// <summary>
        /// The threshold that the current normalized value of the control can be within to consider the target value has been reached.
        /// </summary>
        [Header("Threshold Settings"), Tooltip("The threshold that the current normalized value of the control can be within to consider the target value has been reached.")]
        public float targetValueReachedThreshold = 0.025f;
        #endregion

        /// <summary>
        /// The current raw value for the drive control.
        /// </summary>
        public float Value => CalculateValue(facade.DriveAxis, DriveLimits);
        /// <summary>
        /// The current normalized value for the drive control between the set limits.
        /// </summary>
        public float NormalizedValue => Mathf.InverseLerp(DriveLimits.minimum, DriveLimits.maximum, Value);
        /// <summary>
        /// The current step value for the drive control.
        /// </summary>
        public float StepValue => CalculateStepValue(facade);
        /// <summary>
        /// The current normalized step value for the drive control between the set step range.
        /// </summary>
        public float NormalizedStepValue => Mathf.InverseLerp(facade.stepRange.minimum, facade.stepRange.maximum, StepValue);

        /// <summary>
        /// The calculated direction for the drive axis.
        /// </summary>
        public Vector3 AxisDirection
        {
            get;
            protected set;
        }

        /// <summary>
        /// The calculated limits for the drive.
        /// </summary>
        public FloatRange DriveLimits
        {
            get;
            protected set;
        }

        /// <summary>
        /// The previous state of <see cref="Value"/>.
        /// </summary>
        protected float previousValue = float.MaxValue;
        /// <summary>
        /// The previous state of <see cref="StepValue"/>.
        /// </summary>
        protected float previousStepValue = float.MaxValue;
        /// <summary>
        /// The previous state of whether the target value has been reached.
        /// </summary>
        protected bool previousTargetValueReached;

        /// <summary>
        /// Sets the target value of the drive to the given normalized value.
        /// </summary>
        /// <param name="newValue">The normalized value to set the Target Value to.</param>
        public abstract void SetTargetValue(float newValue);
        /// <summary>
        /// Processes the speed in which the drive can affect the control.
        /// </summary>
        /// <param name="driveSpeed">The speed to drive the control at.</param>
        /// <param name="moveToTargetValue">Whether to allow the drive to automatically move the control to the desired target value.</param>
        public abstract void ProcessDriveSpeed(float driveSpeed, bool moveToTargetValue);

        /// <summary>
        /// Sets up the drive mechanism.
        /// </summary>
        public virtual void SetUp()
        {
            SetUpInternals();
            AxisDirection = CalculateDriveAxis(facade.DriveAxis);
            DriveLimits = CalculateDriveLimits(facade);
            ProcessDriveSpeed(facade.DriveSpeed, facade.MoveToTargetValue);
            SetTargetValue(facade.TargetValue);
        }

        /// <summary>
        /// Processes the value changes and emits the appropriate events.
        /// </summary>
        public virtual void Process()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (!Value.ApproxEquals(previousValue))
            {
                previousValue = Value;
                EmitValueChanged();
                EmitNormalizedValueChanged();
            }

            if (!StepValue.ApproxEquals(previousStepValue))
            {
                previousStepValue = StepValue;
                EmitStepValueChanged();
            }

            float targetValue = GetTargetValue();
            bool targetValueReached = NormalizedValue.ApproxEquals(targetValue, targetValueReachedThreshold);
            bool shouldEmitEvent = !previousTargetValueReached && targetValueReached;
            previousTargetValueReached = targetValueReached;

            if (CanMoveToTargetValue() && shouldEmitEvent)
            {
                EmitTargetValueReached();
            }
        }

        /// <summary>
        /// Calculates the axis to drive the control on.
        /// </summary>
        /// <param name="driveAxis">The desired world axis.</param>
        /// <returns>The direction of the drive axis.</returns>
        public virtual Vector3 CalculateDriveAxis(DriveAxis.Axis driveAxis)
        {
            return driveAxis.GetAxisDirection(true);
        }

        /// <summary>
        /// Processes the drive's ability to automatically drive the control.
        /// </summary>
        /// <param name="autoDrive">Whether the drive can automatically drive the control.</param>
        public virtual void ProcessAutoDrive(bool autoDrive)
        {
        }

        /// <summary>
        /// Performs any required internal setup.
        /// </summary>
        protected abstract void SetUpInternals();
        /// <summary>
        /// Calculates the current value of the control.
        /// </summary>
        /// <param name="axis">The axis the drive is operating on.</param>
        /// <param name="limits">The limits of the drive.</param>
        /// <returns>The calculated value.</returns>
        protected abstract float CalculateValue(DriveAxis.Axis axis, FloatRange limits);
        /// <summary>
        /// Calculates the limits of the drive.
        /// </summary>
        /// <param name="facade">The facade containing the data for the calculation.</param>
        /// <returns>The minimum and maximum local space limit the drive can reach.</returns>
        protected abstract FloatRange CalculateDriveLimits(TFacade facade);

        protected virtual void OnEnable()
        {
            SetUp();
        }

        /// <summary>
        /// Gets the drive control target value.
        /// </summary>
        /// <returns>The target value specified in the facade.</returns>
        protected virtual float GetTargetValue()
        {
            return facade.TargetValue;
        }

        /// <summary>
        /// Determines whether the drive can move the control to the target value.
        /// </summary>
        /// <returns>Whether the drive can automatically move to the target value specified in the facade.</returns>
        protected virtual bool CanMoveToTargetValue()
        {
            return facade.MoveToTargetValue;
        }

        /// <summary>
        /// Calculates the current step value of the control.
        /// </summary>
        /// <param name="facade">The facade containing the data for the calculation.</param>
        /// <returns>The calculated step value.</returns>
        protected virtual float CalculateStepValue(TFacade facade)
        {
            return Mathf.Round(Mathf.Lerp(facade.stepRange.minimum / facade.stepIncrement, facade.stepRange.maximum / facade.stepIncrement, NormalizedValue));
        }

        /// <summary>
        /// Emits the ValueChanged event.
        /// </summary>
        protected virtual void EmitValueChanged()
        {
            facade.ValueChanged?.Invoke(Value);
        }

        /// <summary>
        /// Emits the NormalizedValueChanged event.
        /// </summary>
        protected virtual void EmitNormalizedValueChanged()
        {
            facade.NormalizedValueChanged?.Invoke(NormalizedValue);
        }

        /// <summary>
        /// Emits the StepValueChanged event.
        /// </summary>
        protected virtual void EmitStepValueChanged()
        {
            facade.StepValueChanged?.Invoke(StepValue);
        }

        /// <summary>
        /// Emits the TargetValueReached event.
        /// </summary>
        protected virtual void EmitTargetValueReached()
        {
            facade.TargetValueReached?.Invoke(NormalizedValue);
        }
    }
}