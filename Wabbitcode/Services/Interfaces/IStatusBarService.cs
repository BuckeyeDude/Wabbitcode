﻿using Revsoft.Wabbitcode.Services.Assembler;

namespace Revsoft.Wabbitcode.Services.Interfaces
{
    public interface IStatusBarService : IService
    {
        /// <summary>
        /// Updates the caret position in the status bar
        /// </summary>
        /// <param name="line">Line of the editor. -1 specifies to hide this field</param>
        /// <param name="column">Column of the editor. -1 specifies to hide this field</param>
        void SetCaretPosition(int line, int column);
        /// <summary>
        /// Sets the text of the largest section of the status bar
        /// </summary>
        /// <param name="text">Text to display in the status bar</param>
        void SetText(string text);
        /// <summary>
        /// Updates the code info with the latest size, min, and max string in the status bar.
        /// </summary>
        /// <param name="info">Code count information containing min runtime, max runtime and size. If null the field will be hidden</param>
        void SetCodeCountInfo(CodeCountInfo info);

        void ShowStatusBar();
        void HideStatusBar();
    }
}