using Nomnom.UnityProjectPatcher.Editor;
using Nomnom.UnityProjectPatcher.Editor.Steps;

namespace Kesomannen.RepoProjectPatcher.Editor {
    [UPPatcher("com.kesomannen.unity-repo-project-patcher")]
    public static class RepoWrapper {
        public static void GetSteps(StepPipeline stepPipeline) {
            stepPipeline.SetInputSystem(InputSystemType.Both);
            stepPipeline.SetGameViewResolution("16:9");
            stepPipeline.OpenSceneAtEnd("Main");
            
            stepPipeline.InsertLast(new FixES3Step());
            stepPipeline.InsertLast(new GeneratePhotonAssembliesStep());
            //stepPipeline.InsertLast(new ReplaceShadersStep());
        }
    }
}