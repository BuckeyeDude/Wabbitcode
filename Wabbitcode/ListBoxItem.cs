﻿namespace Revsoft.Wabbitcode
{

    public class ListBoxItem
    {
        public string Ext
        {
            get;
            set;
        }

        public string File
        {
            get;
            set;
        }

        public string Text
        {
            get;
            set;
        }

        public override string ToString()
        {
            return this.Text;
        }
    }
}