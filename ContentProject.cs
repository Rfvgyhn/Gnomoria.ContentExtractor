using Gnomoria.ContentExtractor.Extensions;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using NLog;
using System.Collections.Generic;
using System.IO;

namespace Gnomoria.ContentExtractor
{
    public class ContentProject
    {
        BuildParameters buildParams;
        Project buildProject;
        ILogger errorLogger;

        public ContentProject(string outputPath, string projectDir = null)
        {
            var rootElement = ProjectRootElement.Create();
            rootElement.AddImport(Path.GetFullPath("lib\\Microsoft.Xna.GameStudio.ContentPipeline.targets"));

            buildProject = new Project(rootElement);
            buildProject.SetProperty("XnaPlatform", "Windows");
            buildProject.SetProperty("XnaProfile", "Reach");
            buildProject.SetProperty("XnaFrameworkVersion", "v4.0");
            buildProject.SetProperty("Configuration", "Release");
            buildProject.SetProperty("OutputPath", outputPath);

            if (!projectDir.IsNullOrEmpty())
                buildProject.SetProperty("BaseIntermediateOutputPath", projectDir.GetRelativePathFrom(Directory.GetCurrentDirectory()));

            errorLogger = new ConfigurableForwardingLogger
            {
                BuildEventRedirector = new NlogEventRedirector()
            };
            buildParams = new BuildParameters(ProjectCollection.GlobalProjectCollection);
            buildParams.Loggers = new ILogger[] { errorLogger };
        }

        public void AddItem(string file, string importer, string link, string name, string processor = null)
        {
            ProjectItem item = buildProject.AddItem("Compile", file)[0];
            item.SetMetadataValue("Link", link);
            item.SetMetadataValue("Name", name);
            item.SetMetadataValue("Importer", importer);
            item.SetMetadataValue("Processor", processor.IsNullOrEmpty() ? "PassThroughProcessor" : processor);
        }

        public void AddReference(string assembly, string hintPath = null)
        {
            if (hintPath.IsNullOrEmpty())
                buildProject.AddItem("Reference", assembly);
            else
                buildProject.AddItem("Reference", assembly, new Dictionary<string, string> { { "HintPath", hintPath } });
        }

        public bool Build()
        {
            BuildManager.DefaultBuildManager.BeginBuild(buildParams);

            BuildRequestData request = new BuildRequestData(buildProject.CreateProjectInstance(), new string[0]);
            BuildSubmission submission = BuildManager.DefaultBuildManager.PendBuildRequest(request);
            var result = submission.Execute();

            BuildManager.DefaultBuildManager.EndBuild();

            return result.OverallResult == BuildResultCode.Success;
        }
    }
}
