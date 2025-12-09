using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using UnityEngine;

namespace EasySpeedup
{
    [StaticConstructorOnStartup]
    public static class PatchConstructor
    {
        static PatchConstructor()
        {
            var harmony = new Harmony("EasySpeedup");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            ((Texture2D[])typeof(Thing).Assembly.GetType("Verse.TexButton").GetField(nameof(TexButton.SpeedButtonTextures)).GetValue(null))[4] =
                ContentFinder<Texture2D>.Get("UI/TimeControls/TimeSpeedButton_Ultrafast", true);
        }
    }

    [HarmonyPatch(typeof(TimeControls), nameof(TimeControls.DoTimeControlsGUI))]
    public static class TimeControlsPatch
    {
        private static readonly MethodInfo devGetter = AccessTools.Property(typeof(Prefs), nameof(Prefs.DevMode)).GetGetMethod();
        private static readonly MethodInfo drawLine = AccessTools.Method(typeof(Widgets), nameof(Widgets.DrawLineHorizontal), new System.Type[] { typeof(float), typeof(float), typeof(float) });

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);
            var buttonDrawn = false;
            var lineWidthFixed = false;
            var devModeEnabled = false;

            for (var i = 0; i < list.Count; i++)
            {
                var code = list[i];

                if (!buttonDrawn && code.opcode == OpCodes.Ldloc_3)
                {
                    i += 3;
                    buttonDrawn = true;
                    if (i < list.Count)
                    {
                        yield return new CodeInstruction(list[i]);
                    }
                    continue;
                }

                if (!lineWidthFixed && code.LoadsConstant(2d) && (i + 2) < list.Count)
                {
                    var codeAfterNext = list[i + 2];
                    if (codeAfterNext.Calls(drawLine))
                    {
                        code.operand = 3f;
                        lineWidthFixed = true;
                        yield return code;
                        continue;
                    }
                }

                if (!devModeEnabled && code.Calls(devGetter))
                {
                    code.opcode = OpCodes.Ldc_I4_1;
                    code.operand = null;
                    devModeEnabled = true;
                    yield return code;
                    continue;
                }

                yield return code;
            }
        }

        public static void Prefix(ref Rect timerRect)
        {
            timerRect.x -= 35f;
            timerRect.width += 35f;
        }
    }
}