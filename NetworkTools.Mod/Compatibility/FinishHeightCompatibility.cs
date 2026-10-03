#if IS_DEBUG
namespace NetworkTools.Compatibility {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;
    using Colossal.Logging;
    using Game.Net;
    using HarmonyLib;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;

    // Explicit experimental launch opt-in. Covers ALL GeometrySystem rebuilds,
    // including loading; never silently tied to the currently selected NT tool.
    internal static class FinishHeightCompatibility {
        public const string LaunchFlag = "--nt-experimental-finish-height-preparation";
        private const string Owner = "NetworkTools.Experimental.FinishHeightPreparation";
        private const string JobName = "Game.Net.GeometrySystem+FinishEdgeGeometryJob";
        private static Harmony m_Harmony;
        private static ILog m_Log;
        public static string Status { get; private set; } = "disabled";
        public static long ScheduledPasses { get; private set; }

        internal static bool FingerprintsMatch(string game, string math, string burst) =>
            game == "AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A" &&
            math == "F8C037B71D2BB4496DDAB30B79D8840F5A32F0738A1547BA5534E5B43254C9B8" &&
            burst == "C907D1A8E74368513756FE860853DAF0B032D59DC5E30A12EA236A6469CDC2EB";

        private static string Hash(string path) {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }

        public static void Install(ILog log) {
            m_Log = log;
            if (!Environment.GetCommandLineArgs().Contains(LaunchFlag)) return;
            InstallEnabled(log);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void InstallEnabled(ILog log) {
            try {
                if (!FingerprintsMatch(Hash(typeof(GeometrySystem).Assembly.Location), Hash(typeof(int2).Assembly.Location),
                    Hash(Path.Combine(UnityEngine.Application.dataPath, "Plugins", "x86_64", "lib_burst_generated.dll"))))
                    throw new InvalidOperationException("Unsupported native binary fingerprints");
                var target = AccessTools.Method(typeof(GeometrySystem), "OnUpdate");
                if (Harmony.GetPatchInfo(target)?.Transpilers.Count > 0)
                    throw new InvalidOperationException("Existing GeometrySystem transpiler; combined patch ordering is not qualified");
                var harmony = new Harmony(Owner);
                try {
                    harmony.Patch(target, transpiler: new HarmonyMethod(typeof(FinishHeightCompatibility), nameof(Transpile)));
                    m_Harmony = harmony;
                } catch { harmony.UnpatchAll(Owner); throw; }
                Status = "enabled-experimental-all-native-rebuilds";
                log.Info("[FinishHeightCompatibility] " + Status);
            } catch (Exception error) {
                Status = "refused: " + error.Message;
                log.Warn("[FinishHeightCompatibility] " + Status);
            }
        }

        public static void Dispose() {
            m_Harmony?.UnpatchAll(Owner);
            m_Harmony = null;
            Status = "disabled";
        }

        internal static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) {
            var result = instructions.Select(i => new CodeInstruction(i)).ToList();
            int count = 0;
            foreach (var instruction in result) {
                if (!(instruction.operand is MethodInfo method) || !method.IsGenericMethod) continue;
                var arguments = method.GetGenericArguments();
                if (method.DeclaringType != typeof(IJobParallelForDeferExtensions) || method.Name != "Schedule" ||
                    arguments.Length != 2 || arguments[0].FullName != JobName || arguments[1] != typeof(Entity)) continue;
                var parameters = method.GetParameters();
                if (parameters.Length != 4 || parameters[1].ParameterType != typeof(NativeList<Entity>) ||
                    parameters[2].ParameterType != typeof(int) || parameters[3].ParameterType != typeof(JobHandle) ||
                    method.ReturnType != typeof(JobHandle)) throw new InvalidOperationException("Finishing scheduler signature changed");
                // Resolve exact field types before any replacement can be installed.
                RequireField(arguments[0], "m_EdgeGeometryData", typeof(ComponentLookup<EdgeGeometry>));
                RequireField(arguments[0], "m_EdgeHeightMap", typeof(NativeParallelHashMap<int2, float4>));
                instruction.operand = typeof(FinishHeightCompatibility).GetMethod(nameof(Schedule)).MakeGenericMethod(arguments);
                count++;
            }
            if (count != 1) throw new InvalidOperationException("Expected exactly one native finishing scheduler, got " + count);
            return result;
        }

        private static FieldInfo RequireField(Type type, string name, Type expected) {
            var field = type.GetField(name);
            if (field == null || field.FieldType != expected) throw new InvalidOperationException("Finishing field contract changed: " + name);
            return field;
        }

        private static class Fields<T> {
            public static readonly FieldInfo Geometry = RequireField(typeof(T), "m_EdgeGeometryData", typeof(ComponentLookup<EdgeGeometry>));
            public static readonly FieldInfo Heights = RequireField(typeof(T), "m_EdgeHeightMap", typeof(NativeParallelHashMap<int2, float4>));
        }

        public static JobHandle Schedule<T, U>(T job, NativeList<U> list, int batch, JobHandle dependency)
            where T : struct, IJobParallelForDefer where U : unmanaged {
            // Validated generic signature is Entity; no dereference of deferred data
            // or synchronous Complete here. Original scheduler owns container lifetime.
            var entities = (NativeList<Entity>)(object)list;
            object boxed = job;
            var prepare = new PrepareHeightsJob {
                Entities = entities.AsDeferredJobArray(),
                Geometry = (ComponentLookup<EdgeGeometry>)Fields<T>.Geometry.GetValue(boxed),
                Heights = (NativeParallelHashMap<int2, float4>)Fields<T>.Heights.GetValue(boxed)
            };
            var prepared = IJobParallelForDeferExtensions.Schedule(prepare, entities, batch, dependency);
            ScheduledPasses++;
            if (ScheduledPasses == 1) m_Log?.Info("[FinishHeightCompatibility] first preparation scheduled; not completion evidence");
            return IJobParallelForDeferExtensions.Schedule(job, list, batch, prepared);
        }

        // Deliberately no BurstCompile: producer lookup uses the verified managed hash.
        // No allocation, reflection or logs on the worker thread. Each entity is unique
        // in the original native deferred list; the original job uses the same restriction.
        private struct PrepareHeightsJob : IJobParallelForDefer {
            [ReadOnly] public NativeArray<Entity> Entities;
            [ReadOnly] public NativeParallelHashMap<int2, float4> Heights;
            [NativeDisableParallelForRestriction] public ComponentLookup<EdgeGeometry> Geometry;
            public void Execute(int index) {
                var entity = Entities[index];
                var geometry = Geometry[entity];
                bool changed = false;
                for (int endpoint = 0; endpoint < 2; endpoint++) {
                    if (!Heights.TryGetValue(new int2(entity.Index, endpoint), out var heights)) continue;
                    FinishHeightPreparation.Apply(ref geometry, endpoint, heights);
                    changed = true;
                }
                if (changed) Geometry[entity] = geometry;
            }
        }
    }
}
#endif
