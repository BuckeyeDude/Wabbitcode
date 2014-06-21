﻿using System;
using System.Windows.Forms;
using Revsoft.Wabbitcode.Services.Interfaces;
using Revsoft.Wabbitcode.Services.Project;
using Revsoft.Wabbitcode.Utils;

namespace Revsoft.Wabbitcode.GUI.Dialogs
{
    public partial class BuildSteps : Form
    {
        private BuildConfig _currentConfig;
        private int _currentIndex;
        private bool _needsSave;
        private readonly IProject _project;

        public BuildSteps()
        {
            _currentIndex = 0;
            IProjectService projectService = DependencyFactory.Resolve<IProjectService>();
            _project = projectService.Project;

            InitializeComponent();

            GetBuildConfigs();
            configBox.SelectedItem = _project.BuildSystem.CurrentConfig;
        }

        private void actionBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            InternalBuildStep step = (InternalBuildStep) buildSeqList.SelectedItem;
            _currentConfig.RemoveStep(step);

            step.StepType = (BuildStepType) actionBox.SelectedIndex;
            buildSeqList.Items[buildSeqList.SelectedIndex] = step;
            _currentConfig.AddStep(step);

            UpdateStepOptions();
            _needsSave = true;
        }

        private void addDirButton_Click(object sender, EventArgs e)
        {
            int count = buildSeqList.Items.Count;
            FilePath fileName = new FilePath(_project.ProjectName + ".asm");
            IBuildStep stepToAdd = new InternalBuildStep(count, BuildStepType.Assemble,
                fileName, fileName.ChangeExtension("8xk"));
            _currentConfig.AddStep(stepToAdd);
            buildSeqList.Items.Insert(count, stepToAdd);
            _needsSave = true;
        }

        private void browseInput_Click(object sender, EventArgs e)
        {
            inputBox.Text = DoOpenFileDialog();
        }

        private void browseOutput_Click(object sender, EventArgs e)
        {
            outputBox.Text = DoOpenFileDialog();
        }

        private void buildSeqList_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateStepOptions();
        }

        /// <summary>
        /// This is a hack to remember the config so we can save it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void configBox_DropDown(object sender, EventArgs e)
        {
        }

        private void configBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_currentConfig != null && !_currentConfig.Equals(_project.BuildSystem.BuildConfigs[_currentIndex]))
            {
                if (MessageBox.Show(this,
                    "Would you like to save the current configuration?",
                    "Save?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.None) == DialogResult.Yes)
                {
                    _project.BuildSystem.BuildConfigs[_currentIndex] = _currentConfig;
                }
            }

            _currentIndex = configBox.SelectedIndex;
            _currentConfig = (BuildConfig) _project.BuildSystem.BuildConfigs[_currentIndex].Clone();
            PopulateListBox();
            UpdateStepOptions();
        }

        private void deleteDirButton_Click(object sender, EventArgs e)
        {
            IBuildStep selectedItem = (IBuildStep) buildSeqList.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            _currentConfig.RemoveStep(selectedItem);
            buildSeqList.Items.Remove(selectedItem);
            _needsSave = true;
        }

        private string DoOpenFileDialog()
        {
            bool isExternal = buildSeqList.SelectedItem is ExternalBuildStep;
            string defExt = isExternal ? "*.bat" : "*.asm";
            string filter = isExternal ? "All Know File Types | *.bat; *.exe; |Executable (*.exe)|*.exe|Batch Files (*.bat)|*.bat|All Files (*.*)|*.*" :
                "All Know File Types | *.asm; *.z80; *.inc; |Assembly Files (*.asm)|*.asm|*.z80" +
                " Assembly Files (*.z80)|*.z80|Include Files (*.inc)|*.inc|All Files (*.*)|*.*";
            string title = isExternal ? "Program to run" : "Add existing file";
            string filePath = null;
            using (OpenFileDialog openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                DefaultExt = defExt,
                Filter = filter,
                FilterIndex = 0,
                Multiselect = false,
                RestoreDirectory = true,
                Title = title,
            })
            {
                DialogResult result = openFileDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;
                }
            }

            return filePath;
        }

        private void editButton_Click(object sender, EventArgs e)
        {
            _needsSave = true;
        }

        private void GetBuildConfigs()
        {
            foreach (BuildConfig config in _project.BuildSystem.BuildConfigs)
            {
                configBox.Items.Add(config);
            }

            if (configBox.Items.Count != 0)
            {
                return;
            }

            _project.BuildSystem.BuildConfigs.Add(new BuildConfig("Debug"));
            _project.BuildSystem.BuildConfigs.Add(new BuildConfig("Release"));
        }

        private void inputBox_TextChanged(object sender, EventArgs e)
        {
            IBuildStep step = (IBuildStep) buildSeqList.SelectedItem;
            if (step is InternalBuildStep)
            {
                FilePath newPath = new FilePath(inputBox.Text);
                step.InputFile = _project.ProjectDirectory.Combine(newPath).NormalizePath();
            }
            else if (step is ExternalBuildStep)
            {
                step.InputFile = new FilePath(inputBox.Text);
            }

            int index = buildSeqList.SelectedIndex;
            int selectIndex = inputBox.SelectionStart;

            // HACK: forces update of item text
            buildSeqList.Items.Remove(step);
            buildSeqList.Items.Insert(index, step);
            buildSeqList.SelectedIndex = index;
            inputBox.Focus();
            inputBox.SelectionStart = selectIndex;

            _needsSave = true;
        }

        private void moveDown_Click(object sender, EventArgs e)
        {
            int index = buildSeqList.SelectedIndex;
            if (index == buildSeqList.Items.Count - 1)
            {
                return;
            }

            _currentConfig.Steps[index].StepNumber += 2;
            foreach (IBuildStep t in _currentConfig.Steps)
            {
                t.StepNumber--;
            }

            index++;
            _needsSave = true;
            IBuildStep selectedItem = (IBuildStep) buildSeqList.SelectedItem;
            buildSeqList.Items.Remove(selectedItem);
            buildSeqList.Items.Insert(index, selectedItem);
            buildSeqList.SelectedIndex = index;
        }

        private void moveUp_Click(object sender, EventArgs e)
        {
            int index = buildSeqList.SelectedIndex;
            if (index == 0)
            {
                return;
            }

            _currentConfig.Steps[index].StepNumber -= 2;
            foreach (IBuildStep step in _currentConfig.Steps)
            {
                step.StepNumber++;
            }

            index--;
            _needsSave = true;
            IBuildStep selectedItem = (IBuildStep) buildSeqList.SelectedItem;
            buildSeqList.Items.Remove(selectedItem);
            buildSeqList.Items.Insert(index, selectedItem);
            buildSeqList.SelectedIndex = index;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if ((_currentConfig == null || _currentConfig.Equals(_project.BuildSystem.BuildConfigs[_currentIndex])) && !_needsSave)
            {
                return;
            }
            if (MessageBox.Show(
                "Would you like to save the current configuration?",
                "Save?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.None) == DialogResult.Yes)
            {
                _project.BuildSystem.BuildConfigs[_currentIndex] = _currentConfig;
            }
        }

        private void outputBox_TextChanged(object sender, EventArgs e)
        {
            InternalBuildStep step = (InternalBuildStep) buildSeqList.SelectedItem;
            FilePath newPath = new FilePath(outputBox.Text);
            step.OutputFile = _project.ProjectDirectory.Combine(newPath).NormalizePath();
            _needsSave = true;
        }

        private void PopulateListBox()
        {
            buildSeqList.Items.Clear();
            foreach (IBuildStep step in _currentConfig.Steps)
            {
                buildSeqList.Items.Add(step);
            }

            buildSeqList.SelectedIndex = _currentConfig.Steps.Count - 1;
        }

        private void stepTypeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            IBuildStep step = null;
            _currentConfig.RemoveStep((IBuildStep) buildSeqList.SelectedItem);

            int stepNum = ((IBuildStep) buildSeqList.SelectedItem).StepNumber;
            if (buildSeqList.SelectedItem is InternalBuildStep && stepTypeBox.SelectedIndex != 0)
            {
                step = new ExternalBuildStep(buildSeqList.SelectedIndex, new FilePath("cmd.exe"), string.Empty)
                {
                    StepNumber = stepNum
                };
            }
            else if (buildSeqList.SelectedItem is ExternalBuildStep && stepTypeBox.SelectedIndex != 1)
            {
                step = new InternalBuildStep(
                    buildSeqList.SelectedIndex,
                    BuildStepType.Assemble,
                    _project.ProjectFile.ChangeExtension(".asm"),
                    _project.ProjectFile.ChangeExtension(".8xk"))
                {
                    StepNumber = stepNum
                };
            }

            if (step == null)
            {
                return;
            }

            _currentConfig.AddStep(step);
            buildSeqList.Items[buildSeqList.SelectedIndex] = step;
            UpdateStepOptions();
            _needsSave = true;
        }

        private void UpdateStepOptions()
        {
            if (buildSeqList.SelectedIndex == -1)
            {
                stepOptionsBox.Enabled = false;
                return;
            }

            stepOptionsBox.Enabled = true;

            if (buildSeqList.SelectedItem is InternalBuildStep)
            {
                stepTypeBox.SelectedIndex = 0;
            }
            else if (buildSeqList.SelectedItem is ExternalBuildStep)
            {
                stepTypeBox.SelectedIndex = 1;
            }

            IBuildStep step = (IBuildStep) buildSeqList.SelectedItem;
            switch (stepTypeBox.SelectedIndex)
            {
                case 0:
                    InternalBuildStep intStep = (InternalBuildStep) step;
                    inputLabel.Text = "Input File:";
                    outputLabel.Enabled = true;
                    outputBox.Enabled = true;
                    actionBox.Visible = true;
                    actionLabel.Visible = true;

                    inputBox.Text = intStep.InputFile;
                    outputBox.Text = intStep.OutputFile;
                    actionBox.SelectedIndex = (int) intStep.StepType;
                    break;
                case 1:
                    inputLabel.Text = "Command:";
                    outputLabel.Enabled = false;
                    outputBox.Enabled = false;
                    actionBox.Visible = false;
                    actionLabel.Visible = false;

                    inputBox.Text = step.InputFile;
                    break;
            }
        }

        private void configManagerButton_Click(object sender, EventArgs e)
        {
            // TODO:
        }
    }
}