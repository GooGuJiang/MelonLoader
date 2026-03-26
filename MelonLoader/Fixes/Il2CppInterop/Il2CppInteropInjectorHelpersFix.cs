#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace MelonLoader.Fixes.Il2CppInterop
{
    internal class Il2CppInteropInjectorHelpersFix
    {
        internal static void Install()
        {
            try
            {
                Type thisType = typeof(Il2CppInteropInjectorHelpersFix);
                Type classInjectorType = typeof(ClassInjector);

                Type injectorHelpersType = classInjectorType.Assembly.GetType("Il2CppInterop.Runtime.Injection.InjectorHelpers");
                if (injectorHelpersType == null)
                    throw new Exception("Failed to get InjectorHelpers");

                MelonDebug.Msg("Patching Il2CppInterop InjectorHelpers.Setup...");
                Core.HarmonyInstance.Patch(AccessTools.Method(injectorHelpersType, "Setup"),
                    null, null,
                    AccessTools.Method(thisType, nameof(InjectorHelpersSetup_Transpiler)).ToNewHarmonyMethod());
            }
            catch (Exception e)
            {
                MelonLogger.Error(e);
            }
        }

        // Some games ship player binaries where Il2CppInterop's GetTypeInfoFromTypeDefinitionIndex hook
        // resolves an incompatible function body (for example due to inlining / thunking differences).
        // When that happens class injection crashes very early with an AccessViolation in the generated hook.
        // Disabling that optional hook is safer than crashing the whole game startup.
        private static IEnumerable<CodeInstruction> InjectorHelpersSetup_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var injectorHelpersType = typeof(ClassInjector).Assembly.GetType("Il2CppInterop.Runtime.Injection.InjectorHelpers");
            var hookField = AccessTools.Field(injectorHelpersType, "GetTypeInfoFromTypeDefinitionIndexHook");
            if (hookField == null)
                return instructions;

            var codeMatcher = new CodeMatcher(instructions);
            codeMatcher.MatchStartForward([
                    new(i => i.LoadsField(hookField))
                ]);

            if (codeMatcher.IsValid)
                codeMatcher.RemoveInstructions(2);

            return codeMatcher.Instructions();
        }
    }
}
#endif
