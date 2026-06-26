using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;

namespace Kesomannen.RepoProjectPatcher.Editor {
    public struct UpdatePlayModeStep : IPatcherStep {
        public UniTask<StepResult> Run() {
            if (!EditorSettings.enterPlayModeOptionsEnabled || (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableSceneReload) != 0) {
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions &= ~EnterPlayModeOptions.DisableSceneReload;
                AssetDatabase.SaveAssets();
            }
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}