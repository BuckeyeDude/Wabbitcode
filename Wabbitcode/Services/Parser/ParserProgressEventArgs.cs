﻿using Revsoft.Wabbitcode.Utils;

namespace Revsoft.Wabbitcode.Services.Parser
{
    public class ParserProgressEventArgs : ParserEventArgs
    {
        public int PercentComplete { get; private set; }

        public ParserProgressEventArgs(FilePath fileName, int percentComplete) : base(fileName)
        {
            PercentComplete = percentComplete;
        }
    }
}