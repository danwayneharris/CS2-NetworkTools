namespace NetworkTools.Systems.Tools.Parameters {
    using Unity.Mathematics;

    /// <summary>Finite-number boundary for the scalar and vector parameter types in this mod.</summary>
    internal static class ParameterValueValidation {
        internal static bool IsFinite<T>(T value) {
            if (value is float scalar) return Finite(scalar);
            if (value is double precise) return !double.IsNaN(precise) && !double.IsInfinity(precise);
            if (value is float3 vector) return Finite(vector.x) && Finite(vector.y) && Finite(vector.z);
            if (value is quaternion rotation) {
                return Finite(rotation.value.x) && Finite(rotation.value.y) &&
                       Finite(rotation.value.z) && Finite(rotation.value.w);
            }
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
