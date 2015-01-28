﻿using System.Linq;
using Revsoft.Wabbitcode.Extensions;
using Revsoft.Wabbitcode.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Revsoft.Wabbitcode.Services.Interfaces;
using Revsoft.Wabbitcode.Utils;

namespace Revsoft.Wabbitcode.Services.Project
{
    public class BuildSystem : IBuildSystem
    {
        private readonly List<BuildConfig> _buildConfigs = new List<BuildConfig>();
        private int _currentConfigIndex;
        private string _outputText = string.Empty;
        private readonly IProject _project;
        private volatile bool _isBuilding;

        public BuildSystem(IProject project)
        {
            _project = project;
        }

        public void CreateDefaultConfigs()
        {
            BuildConfig debug = new BuildConfig("Debug");
            BuildConfig release = new BuildConfig("Release");
            _buildConfigs.Add(debug);
            _buildConfigs.Add(release);
        }

        public IAssemblerService AssemblerService { get; set; }

        public IList<BuildConfig> BuildConfigs
        {
            get { return _buildConfigs; }
        }

        public BuildConfig CurrentConfig
        {
            get
            {
                if (_currentConfigIndex > _buildConfigs.Count - 1 || _currentConfigIndex == -1)
                {
                    return null;
                }
                return _buildConfigs[_currentConfigIndex];
            }

            set { _currentConfigIndex = _buildConfigs.IndexOf(value); }
        }

        public int CurrentConfigIndex
        {
            get { return _currentConfigIndex; }
            set { _currentConfigIndex = value; }
        }

        public FilePath MainFile
        {
            get
            {
                if (!CurrentConfig.Steps.Any())
                {
                    return null;
                }

                InternalBuildStep step = GetMainBuildStep();

                return step.InputFile;
            }
        }

        public FilePath MainOutput
        {
            get
            {
                if (!CurrentConfig.Steps.Any())
                {
                    return null;
                }

                InternalBuildStep step = GetMainBuildStep();

                return step.OutputFile;
            }
        }

        public string OutputText
        {
            get { return _outputText; }
        }

        public FilePath LabelOutput { get; set; }

        public FilePath ListOutput { get; set; }

        public FilePath ProjectOutput { get; set; }

        public bool IsBuilding
        {
            get { return _isBuilding; }
        }

        private InternalBuildStep GetMainBuildStep()
        {
            InternalBuildStep step = (InternalBuildStep) CurrentConfig.Steps.FirstOrDefault(s => s is InternalBuildStep);

            if (step == null)
            {
                throw new ArgumentException("Missing main build step");
            }

            return step;
        }

        /// <summary>
        /// Builds the build system according to the BuildConfig
        /// </summary>
        /// <returns></returns>
        public bool Build()
        {
            if (_buildConfigs.Count < 1 || _currentConfigIndex == -1)
            {
                throw new MissingConfigException("Missing config");
            }

            _isBuilding = true;
            BuildConfig config = _buildConfigs[_currentConfigIndex];
            bool succeeded = config.Build(_project);
            _outputText = config.OutputText;
            _isBuilding = false;
            return succeeded;
        }

        public void ReadXML(XmlTextReader reader)
        {
            FilePath root = _project.ProjectDirectory;
            if (reader.Name != "BuildSystem")
            {
                throw new ArgumentException("Invalid XML Format");
            }

            var attribute = reader.GetAttribute("IncludeDirs");
            if (attribute != null)
            {
                string[] includeDirs = attribute.Split(';');
                foreach (string include in includeDirs.Where(include => !string.IsNullOrEmpty(include)))
                {
                    string path = Uri.UnescapeDataString(new Uri(Path.Combine(root, include)).AbsolutePath);
                    _project.IncludeDirs.Add(new FilePath(path));
                }
            }

            BuildConfig configToAdd = null;
            while (reader.MoveToNextElement())
            {
                if (reader.Name.Contains("Step"))
                {
                    if (configToAdd == null)
                    {
                        throw new ArgumentException("Invalid XML Format");
                    }

                    int count = Convert.ToInt32(reader.GetAttribute("StepNum"));
                    string inputFile = reader.GetAttribute("InputFile");
                    switch (reader.Name)
                    {
                        case "ExternalBuildStep":
                            string arguments = reader.GetAttribute("Arguments");
                            if (inputFile != null)
                            {
                                ExternalBuildStep exstep = new ExternalBuildStep(
                                    count,
                                    root.Combine(inputFile),
                                    arguments);
                                configToAdd.AddStep(exstep);
                            }
                            break;
                        case "InternalBuildStep":
                            string outputFile = reader.GetAttribute("OutputFile");
                            BuildStepType type = (BuildStepType) Convert.ToInt16(reader.GetAttribute("StepType"));
                            if (inputFile != null && outputFile != null)
                            {
                                InternalBuildStep instep = new InternalBuildStep(
                                    count,
                                    type,
                                    root.Combine(inputFile),
                                    root.Combine(outputFile));
                                configToAdd.AddStep(instep);
                            }
                            break;
                        default:
                            throw new ArgumentException("Invalid XML Format");
                    }
                }
                else
                {
                    string configName = reader.Name;
                    configToAdd = new BuildConfig(configName);
                    _buildConfigs.Add(configToAdd);
                }
            }
        }

        public void WriteXML(XmlTextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteStartElement("BuildSystem");
            string includes = string.Empty;
            string projFile = _project.ProjectFile;
            foreach (string include in _project.IncludeDirs.Where(include => !string.IsNullOrEmpty(include)))
            {
                if (!Path.IsPathRooted(include))
                {
                    includes += include + ";";
                }
                else
                {
                    includes += FileOperations.GetRelativePath(projFile, include) + ";";
                }
            }

            writer.WriteAttributeString("IncludeDirs", includes);
            foreach (BuildConfig config in _buildConfigs)
            {
                writer.WriteStartElement(config.Name);
                foreach (IBuildStep step in config.Steps)
                {
                    writer.WriteStartElement(step.GetType().Name);
                    writer.WriteAttributeString("StepNum", step.StepNumber.ToString());
                    writer.WriteAttributeString("InputFile", FileOperations.GetRelativePath(projFile, step.InputFile));
                    var externalBuildStep = step as ExternalBuildStep;
                    if (externalBuildStep != null)
                    {
                        writer.WriteAttributeString("Arguments", externalBuildStep.Arguments);
                    }
                    else
                    {
                        var buildStep = step as InternalBuildStep;
                        if (buildStep != null)
                        {
                            var intStep = buildStep;
                            writer.WriteAttributeString("OutputFile", FileOperations.GetRelativePath(projFile, intStep.OutputFile));
                            writer.WriteAttributeString("StepType", Convert.ToInt16(intStep.StepType).ToString());
                        }
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}