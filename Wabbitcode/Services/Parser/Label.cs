﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Revsoft.Wabbitcode.Services.Parser
{
	class Label : ILabel
	{
		public Label(int offset, string labelName, bool isEquate, string description, ParserInformation parent)
		{
			LabelName = labelName;
			Offset = offset;
			IsEquate = isEquate;
			Description = description;
			Parent = parent;
		}

		public string Name
		{
			get { return LabelName; }
		}

		public string LabelName
		{
			get;
			set;
		}

		public bool IsReusable
		{
			get
			{
				return LabelName == "_";
			}
		}

		public bool IsEquate
		{
			get;
			set;
		}

		public int Offset
		{
			get;
			set;
		}

		public string Description
		{
			get;
			set;
		}

		public ParserInformation Parent
		{
			get;
			set;
		}

		public override string ToString()
		{
			return LabelName;
		}

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

		public override bool Equals(object obj)
		{
			if (obj.GetType() != typeof(Label))
				return false;
			Label label2 = obj as Label;
			return Offset == label2.Offset && LabelName == label2.LabelName;
		}

		public static bool operator ==(Label label1, Label label2)
		{
			if ((object)label1 == null || (object)label2 == null)
				if ((object)label1 == null && (object)label2 == null)
					return true;
				else 
					return false;
			return label1.Offset == label2.Offset && label1.LabelName == label2.LabelName;
		}

		public static bool operator !=(Label label1, Label label2)
		{
			if ((object)label1 == null || (object)label2 == null)
				if ((object)label1 != null && (object)label2 != null)
					return false;
				else 
					return true;
			return label1.Offset != label2.Offset || label1.LabelName != label2.LabelName;
		}
    }
}
