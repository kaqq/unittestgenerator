namespace SentryOne.UnitTestGenerator.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using EnvDTE;
    using Microsoft.VisualStudio.Shell;
    using SentryOne.UnitTestGenerator.Core.Helpers;
    using SentryOne.UnitTestGenerator.Core.Options;
    using SentryOne.UnitTestGenerator.Properties;

    public class ProjectItemModel
    {
        public ProjectItemModel(ProjectItem projectItem, IGenerationOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            Item = projectItem ?? throw new ArgumentNullException(nameof(projectItem));
            FilePath = projectItem.FileNames[1];
            TargetProjectName = options.GetTargetProjectName(Item.ContainingProject.Name);
            try
            {
                TargetProjectName = string.Format(CultureInfo.CurrentCulture, options.TestProjectNaming, Item.ContainingProject.Name);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException(Strings.ProjectItemModel_ProjectItemModel_Cannot_not_derive_target_project_name__please_check_the_test_project_naming_setting_);
            }
        }

        public string FilePath { get; }

        public ProjectItem Item { get; }

        public Project Project
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return Item.ContainingProject;
            }
        }

        public Project TargetProject
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return FindProject(TargetProjectName, Project.DTE.Solution.Projects.OfType<Project>());
            }
        }

#pragma warning disable VSTHRD010
        private Project FindProject(string targetProjectName, IEnumerable<Project> currentProjects)
        {
            foreach (var project in currentProjects)
            {
                if (string.Equals(project.Name, targetProjectName, StringComparison.OrdinalIgnoreCase))
                {
                    return project;
                }
                else if (project.Kind == VsProjectKindSolutionFolder)
                {
                    var subProjects = project
                            .ProjectItems
                            .OfType<ProjectItem>()
                            .Where(item => item.SubProject != null)
                            .Select(item => item.SubProject);
                    var candidate = FindProject(targetProjectName, subProjects);
                    if (candidate != null)
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

#pragma warning restore VSTHRD010
        public string TargetProjectName { get; }

        private const string VsProjectKindSolutionFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";

        public static bool IsSupported(ProjectItemModel item)
        {
            return item?.FilePath != null && item.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
        }
    }
}
