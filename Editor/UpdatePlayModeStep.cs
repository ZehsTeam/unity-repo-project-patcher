using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;

namespace Kesomannen.RepoProjectPatcher.Editor {
    public struct UpdatePlayModeStep : IPatcherStep {
        public UniTask<StepResult> Run() {
            const EnterPlayModeOptions skipFlags = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

            if (EditorSettings.enterPlayModeOptionsEnabled && (EditorSettings.enterPlayModeOptions & skipFlags) != 0) {
                EditorSettings.enterPlayModeOptions &= ~skipFlags;
                AssetDatabase.SaveAssets();
            }
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}