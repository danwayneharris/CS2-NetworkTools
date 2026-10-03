namespace NetworkTools.Systems.Tools.Parameters {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using NetworkTools.Systems.Tools.Handles;

    /// <summary>
    ///     Typed parameter. Fires <see cref="ParameterBase.OnChanged" /> on value change (equality-guarded).
    /// </summary>
    public abstract class Parameter<T> : ParameterBase {
        private T m_Value;

        public T Default { get; }

        /// <summary>Optional domain boundary; rejects a write without mutation or events.</summary>
        public Func<T, bool> ValidateValue { get; init; }


        public IHandleSpec<T>[] Handles { get; init; }

        public T Value {
            get => m_Value;
            set => SetValue(value, ChangeOrigin.Code);
        }

        public void SetValue(T value, ChangeOrigin origin) => TrySetValue(value, origin);

        /// <summary>
        ///     Accepts finite values and any explicit domain validator, without implicitly imposing
        ///     UI range metadata on handle/code writes.
        ///     Rejection retains the previous value and never raises OnChanged.
        /// </summary>
        public bool TrySetValue(T value, ChangeOrigin origin) {
            if (!ParameterValueValidation.IsFinite(value)) {
                Log?.Warn($"[Parameter] {Key}: rejected nonfinite value from {origin}; retaining current value.");
                return false;
            }
            if (ValidateValue != null && !ValidateValue(value)) {
                Log?.Warn($"[Parameter] {Key}: rejected out-of-domain value from {origin}; retaining current value.");
                return false;
            }
            if (EqualityComparer<T>.Default.Equals(m_Value, value)) return true;
            var old = m_Value;
            m_Value = value;
            Log?.Debug($"[Parameter] {Key}: {old} → {value}");
            RaiseChanged(origin);
            return true;
        }

        protected Parameter(string key, T @default, int modes = 0, bool bindable = true, string label = null, bool persist = true)
            : base(key, modes, bindable, label, persist) {
            if (!ParameterValueValidation.IsFinite(@default)) {
                throw new ArgumentException("A parameter default must be finite.", nameof(@default));
            }
            Default = @default;
            m_Value = @default;
        }

        /// <inheritdoc />
        public override string SerializeValue() =>
            Convert.ToString(Value, CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public override bool TryDeserializeValue(string raw) {
            T parsed;
            try {
                parsed = (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);
            } catch (Exception error) when (error is FormatException || error is InvalidCastException ||
                                            error is OverflowException || error is ArgumentException) {
                Log?.Warn($"[Parameter] {Key}: persisted value could not be parsed ({error.GetType().Name}); retaining current value.");
                return false;
            }

            // Do not swallow subscriber exceptions after a successful write and misreport
            // them as a failed parse. Nonfinite input is rejected before any state changes.
            return TrySetValue(parsed, ChangeOrigin.Code);
        }

        public override void ResetToDefault() {
            Log?.Debug($"[Parameter] {Key}: reset to default ({Default})");
            Value = Default;
        }
    }
}
