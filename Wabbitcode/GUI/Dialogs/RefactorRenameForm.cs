﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Revsoft.TextEditor;
using Revsoft.TextEditor.Document;
using Revsoft.Wabbitcode.Services.Interfaces;
using Revsoft.Wabbitcode.Services.Parser;
using Revsoft.Wabbitcode.TextEditor;
using Revsoft.Wabbitcode.TextEditor.Interfaces;
using Revsoft.Wabbitcode.Utils;

namespace Revsoft.Wabbitcode.GUI.Dialogs
{
    public sealed partial class RefactorRenameForm : Form
    {
        private const int PreviewHeight = 400;
        private readonly ITextEditor _editor;
        private readonly IProjectService _projectService;
        private readonly IFileService _fileService;
        private readonly Dictionary<string, TextEditorControl> _editors = new Dictionary<string, TextEditorControl>();
        private readonly Timer _updateTimer = new Timer {Interval = 2000};

        private string _selectedText;
        private List<List<Reference>> _references;
        private bool _hasBeenInited;
        private int _lastLength;

        public RefactorRenameForm(ITextEditor editor, IFileService fileService, IProjectService projectService)
        {
            _fileService = fileService;
            _projectService = projectService;
            _editor = editor;
            _updateTimer.Tick += updateTimer_Tick;
        }

        /// <summary>
        /// Sets up rename form based on inputs
        /// </summary>
        /// <returns>True if word at caret is valid for rename, false otherwise</returns>
        public bool Initialize()
        {
            _selectedText = _editor.GetWordAtCaret();

            _references = _projectService.FindAllReferences(_selectedText).ToList();

            InitializeComponent();
            Text = string.Format("Rename '{0}'", _selectedText);
            nameBox.Text = _selectedText;
            _lastLength = _selectedText.Length;
            SetupPreview();
            UpdateEditorReferences();

            return true;
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            _updateTimer.Stop();
            Close();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            _updateTimer.Stop();
            var refs = _projectService.FindAllReferences(_selectedText);
            foreach (var file in refs)
            {
                FilePath fileName = file.First().File;
                IDocument document = _fileService.GetOpenDocument(fileName);
                document.UndoStack.StartUndoGroup();

                foreach (var reference in file)
                {
                    int offset = document.GetOffsetForLineNumber(reference.Line) + reference.Col;
                    int len = reference.ReferenceString.Length;
                    document.Replace(offset, len, nameBox.Text);
                }

                document.UndoStack.EndUndoGroup();
            }

            foreach (var editor in _editors)
            {
                editor.Value.Dispose();
            }

            _editors.Clear();
        }

        private void SetupPreview()
        {
            tabControl.Visible = true;
            tabControl.TabPages.Clear();
            Height += PreviewHeight;
            prevRefButton.Visible = true;
            nextRefButton.Visible = true;

            foreach (var file in _references)
            {
                string fileName = file.First().File;
                if (_editors.ContainsKey(fileName))
                {
                    continue;
                }

                var tab = new TabPage(Path.GetFileName(fileName)) {Tag = fileName};
                tabControl.TabPages.Add(tab);
                var editor = new WabbitcodeTextEditor
                {
                    Dock = DockStyle.Fill,
                    IsIconBarVisible = false,
                    UseTextHighlighting = false,
                };
                editor.LoadFile(fileName);

                tab.Controls.Add(editor);
                _editors.Add(fileName, editor);

                editor.Document.ReadOnly = true;
            }

            _hasBeenInited = true;
        }

        private void UpdateEditorReferences()
        {
            foreach (var file in _references)
            {
                TextEditorControl editor = _editors[file.First().File];
                editor.IsReadOnly = false;
                foreach (var reference in file)
                {
                    int offset = editor.Document.GetOffsetForLineNumber(reference.Line) + reference.Col;
                    editor.Document.Replace(offset, _lastLength, nameBox.Text);
                    editor.Document.MarkerStrategy.AddMarker(new TextMarker(offset, nameBox.Text.Length, TextMarkerType.SolidBlock, Color.LightGreen));
                    editor.Document.BookmarkManager.AddMark(new Bookmark(editor.Document, new TextLocation(0, reference.Line)));
                }

                editor.IsReadOnly = true;
            }
        }

        private void prevRefButton_Click(object sender, EventArgs e)
        {
            TextEditorControl editor = _editors[tabControl.SelectedTab.Tag.ToString()];
            TextArea textArea = editor.ActiveTextAreaControl.TextArea;
            Bookmark mark = textArea.Document.BookmarkManager.GetPrevMark(textArea.Caret.Line);
            if (mark == null)
            {
                return;
            }

            if (textArea.Caret.Position <= mark.Location)
            {
                tabControl.SelectedIndex--;
                if (tabControl.SelectedIndex < 0)
                {
                    tabControl.SelectedIndex = tabControl.TabCount - 1;
                }

                editor = _editors[tabControl.SelectedTab.Tag.ToString()];
                textArea = editor.ActiveTextAreaControl.TextArea;
                mark = textArea.Document.BookmarkManager.GetLastMark(b => true);
            }

            textArea.Caret.Position = mark.Location;
            textArea.SelectionManager.ClearSelection();
            textArea.SetDesiredColumn();
        }

        private void nextRefButton_Click(object sender, EventArgs e)
        {
            TextEditorControl editor = _editors[tabControl.SelectedTab.Tag.ToString()];
            TextArea textArea = editor.ActiveTextAreaControl.TextArea;
            Bookmark mark = textArea.Document.BookmarkManager.GetNextMark(textArea.Caret.Line);
            if (mark == null)
            {
                return;
            }

            if (textArea.Caret.Position >= mark.Location)
            {
                if (tabControl.SelectedIndex + 1 == tabControl.TabCount)
                {
                    tabControl.SelectedIndex = 0;
                }
                else
                {
                    tabControl.SelectedIndex++;
                }

                editor = _editors[tabControl.SelectedTab.Tag.ToString()];
                textArea = editor.ActiveTextAreaControl.TextArea;
                mark = textArea.Document.BookmarkManager.GetFirstMark(b => true);
            }

            textArea.Caret.Position = mark.Location;
            textArea.SelectionManager.ClearSelection();
            textArea.SetDesiredColumn();
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            UpdateEditorReferences();
            _lastLength = nameBox.TextLength;
        }

        private void nameBox_TextChanged(object sender, EventArgs e)
        {
            if (!_hasBeenInited)
            {
                return;
            }

            if (_updateTimer.Enabled)
            {
                _updateTimer.Stop();
            }

            _updateTimer.Start();
        }
    }
}