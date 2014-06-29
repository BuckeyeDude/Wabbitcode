﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Revsoft.Wabbitcode.Actions;
using Revsoft.Wabbitcode.Properties;
using Revsoft.Wabbitcode.Services.Interfaces;
using Revsoft.Wabbitcode.Services.Parser;
using Revsoft.Wabbitcode.Utils;

namespace Revsoft.Wabbitcode.GUI.Dialogs
{
    public partial class GotoSymbol : Form
    {
        private readonly IParserService _parserService;

        public GotoSymbol()
        {
            _parserService = DependencyFactory.Resolve<IParserService>();
            InitializeComponent();
            IEnumerable<IParserData> parserData = _parserService.GetAllParserData().ToList();
            foreach (IParserData data in parserData)
            {
                inputBox.AutoCompleteCustomSource.Add(data.Name);
            }
        }

        private void inputBox_TextChanged(object sender, EventArgs e)
        {
            ValidateInput();
        }

        private void inputBox_Validating(object sender, CancelEventArgs e)
        {
            ValidateInput();
        }

        private void ValidateInput()
        {
            string symbolName = inputBox.Text;
            IParserData data = _parserService.GetParserData(symbolName, Settings.Default.CaseSensitive).FirstOrDefault();
            SetInputError(data == null ? "Invalid symbol name" : string.Empty);
        }

        /// <summary>
        /// Sets the error string of the error provider. An empty string provided means there is no
        /// error.
        /// </summary>
        /// <param name="errorString">The error string to show</param>
        private void SetInputError(string errorString)
        {
            okButton.Enabled = string.IsNullOrEmpty(errorString);
            errorProvider.SetError(inputBox, errorString);
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            string symbolString = inputBox.Text;
            Close();
            AbstractUiAction.RunCommand(new GotoDefinitionAction(new FilePath(string.Empty), symbolString, 0));
        }
    }
}