using Il2CppMonomiPark.SlimeRancher.UI.IntroSequence;
namespace SR2E.Patches.InGame
{
    [HarmonyPatch(typeof(IntroSequenceUIRoot), nameof(IntroSequenceUIRoot.Start))]
    public class SkipIntroPatch
    {
        public static void Postfix(IntroSequenceUIRoot __instance)
        {
            if (SR2EEntryPoint.saveSkipIntro)
            {
                __instance.EndSequence();
                Object.Destroy(__instance.gameObject);
            }
        }
    }
}