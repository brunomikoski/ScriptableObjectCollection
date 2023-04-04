using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class CollectionPreprocessBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
        {
            CollectionsRegistry.Instance.PostBuildProcess();
        }

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            CollectionsRegistry.Instance.PreBuildProcess();
        }
    }
}
