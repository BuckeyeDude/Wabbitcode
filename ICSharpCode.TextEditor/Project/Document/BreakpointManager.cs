﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@Revsoft.net"/>
//     <version>$Revision: 3272 $</version>
// </file>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text.RegularExpressions;
using Revsoft.TextEditor.Util;

namespace Revsoft.TextEditor.Document
{
	public interface IBreakpointFactory
	{
		Breakpoint CreateBreakpoint(IDocument document, TextLocation location);
	}
	
	/// <summary>
	/// This class handles the breakpoints for a buffer
	/// </summary>
	public class BreakpointManager
	{
		IDocument      document;
		#if DEBUG
		IList<Breakpoint> breakpoint = new CheckedList<Breakpoint>();
		#else
		List<Breakpoint> breakpoint = new List<Breakpoint>();
		#endif
		
		/// <value>
		/// Contains all breakpoints
		/// </value>
		public ReadOnlyCollection<Breakpoint> Marks {
			get {
				return new ReadOnlyCollection<Breakpoint>(breakpoint);
			}
		}
		
		public IDocument Document {
			get {
				return document;
			}
		}

		public Regex HighlightRegex
		{
			get;
			set;
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="BreakpointManager"/>
		/// </summary>
		internal BreakpointManager(IDocument document)
		{
			this.document = document;
			HighlightRegex = new Regex(".+");
		}
		
		/// <summary>
		/// Gets/Sets the breakpoint factory used to create breakpoints for "ToggleMarkAt".
		/// </summary>
		public IBreakpointFactory Factory { get; set;}
		
		/// <summary>
		/// Sets the mark at the line <code>location.Line</code> if it is not set, if the
		/// line is already marked the mark is cleared.
		/// </summary>
		public void ToggleMarkAt(TextLocation location)
		{
			Breakpoint newMark;
			newMark = Factory != null ? Factory.CreateBreakpoint(document, location) :
												new Breakpoint(document, location);

			Type newMarkType = newMark.GetType();
			
			for (int i = 0; i < breakpoint.Count; ++i) {
				Breakpoint mark = breakpoint[i];

				if (mark.LineNumber != location.Line || !mark.CanToggle || mark.GetType() != newMarkType) 
					continue;
				RemoveMark(mark);
				return;
			}
			AddMark(newMark);
		}
		
		public void AddMark(Breakpoint mark)
		{
			//Adds the marker
			if (AddMarker(mark) == false)
			{
				return;
			}

			breakpoint.Add(mark);
			OnAdded(new BreakpointEventArgs(mark));
		}

		public void RemoveMark(Breakpoint mark)
		{
			RemoveMarkerHighlight(mark);
			breakpoint.Remove(mark);
			OnRemoved(new BreakpointEventArgs(mark));
		}
		
		public void RemoveMarks(Predicate<Breakpoint> predicate)
		{
			for (int i = 0; i < breakpoint.Count; ++i) {
				Breakpoint bm = breakpoint[i];
				if (!predicate(bm)) 
					continue;
				breakpoint.RemoveAt(i--);
				OnRemoved(new BreakpointEventArgs(bm));
			}
		}
		
		/// <returns>
		/// true, if a mark at mark exists, otherwise false
		/// </returns>
		public bool IsMarked(int lineNr)
		{
			for (int i = 0; i < breakpoint.Count; ++i) {
				if (breakpoint[i].LineNumber == lineNr) {
					return true;
				}
			}
			return false;
		}

		public bool AddMarker(Breakpoint mark)
		{
			TextMarker marker = HighlightBreakpointMarker(mark.LineNumber);
			if (marker == null)
			{
				return false;
			} 
			document.MarkerStrategy.AddMarker(marker);
			return true;
		}

		public void RemoveMarkerHighlight(Breakpoint mark)
		{
			int thisLineOffset = document.GetOffsetForLineNumber(mark.LineNumber);
			int nextLineOffset = document.GetOffsetForLineNumber(mark.LineNumber + 1);
		    if (nextLineOffset == -1)
		    {
		        nextLineOffset = document.TextLength;
		    }

			document.MarkerStrategy.RemoveAll(b => b.Offset >= thisLineOffset &&
                b.Offset < nextLineOffset && b is BreakpointTextMarker);

			document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, mark.LineNumber));
			document.CommitUpdate();
		}

		private TextMarker HighlightBreakpointMarker(int lineNumber)
		{
		    string line = document.GetText(document.GetLineSegment(lineNumber));
			Match match = HighlightRegex.Match(line);

			if (match.Groups.Count == 0)
			{
				return null;
			}

			Group group = match.Groups["line"];
			int start = group.Index + document.GetOffsetForLineNumber(lineNumber);
			int length = group.Length;
			return length == 0 ? null : new BreakpointTextMarker(start, length);
		}

		/// <remarks>
		/// Clears all breakpoint
		/// </remarks>
		public void Clear()
		{
			foreach (Breakpoint mark in breakpoint) {
				OnRemoved(new BreakpointEventArgs(mark));
				RemoveMarkerHighlight(mark);
			}
			breakpoint.Clear();
		}
		
		/// <value>
		/// The lowest mark, if no marks exists it returns -1
		/// </value>
		public Breakpoint GetFirstMark(Predicate<Breakpoint> predicate)
		{
			if (breakpoint.Count < 1) {
				return null;
			}
			Breakpoint first = null;
			for (int i = 0; i < breakpoint.Count; ++i) {
				if (predicate(breakpoint[i]) && breakpoint[i].IsEnabled && (first == null || breakpoint[i].LineNumber < first.LineNumber)) {
					first = breakpoint[i];
				}
			}
			return first;
		}
		
		/// <value>
		/// The highest mark, if no marks exists it returns -1
		/// </value>
		public Breakpoint GetLastMark(Predicate<Breakpoint> predicate)
		{
			if (breakpoint.Count < 1) {
				return null;
			}
			Breakpoint last = null;
			for (int i = 0; i < breakpoint.Count; ++i) {
				if (predicate(breakpoint[i]) && breakpoint[i].IsEnabled && (last == null || breakpoint[i].LineNumber > last.LineNumber)) {
					last = breakpoint[i];
				}
			}
			return last;
		}
		bool AcceptAnyMarkPredicate(Breakpoint mark)
		{
			return true;
		}
		public Breakpoint GetNextMark(int curLineNr)
		{
			return GetNextMark(curLineNr, AcceptAnyMarkPredicate);
		}
		
		/// <remarks>
		/// returns first mark higher than <code>lineNr</code>
		/// </remarks>
		/// <returns>
		/// returns the next mark > cur, if it not exists it returns FirstMark()
		/// </returns>
		public Breakpoint GetNextMark(int curLineNr, Predicate<Breakpoint> predicate)
		{
			if (breakpoint.Count == 0) {
				return null;
			}
			
			Breakpoint next = GetFirstMark(predicate);
			foreach (Breakpoint mark in breakpoint) {
				if (predicate(mark) && mark.IsEnabled && mark.LineNumber > curLineNr) {
					if (mark.LineNumber < next.LineNumber || next.LineNumber <= curLineNr) {
						next = mark;
					}
				}
			}
			return next;
		}
		
		public Breakpoint GetPrevMark(int curLineNr)
		{
			return GetPrevMark(curLineNr, AcceptAnyMarkPredicate);
		}
		/// <remarks>
		/// returns first mark lower than <code>lineNr</code>
		/// </remarks>
		/// <returns>
		/// returns the next mark lower than cur, if it not exists it returns LastMark()
		/// </returns>
		public Breakpoint GetPrevMark(int curLineNr, Predicate<Breakpoint> predicate)
		{
			if (breakpoint.Count == 0) {
				return null;
			}
			
			Breakpoint prev = GetLastMark(predicate);
			
			foreach (Breakpoint mark in breakpoint) {
				if (predicate(mark) && mark.IsEnabled && mark.LineNumber < curLineNr) {
					if (mark.LineNumber > prev.LineNumber || prev.LineNumber >= curLineNr) {
						prev = mark;
					}
				}
			}
			return prev;
		}
		
		protected virtual void OnRemoved(BreakpointEventArgs e)
		{
			if (Removed != null) {
				Removed(this, e);
			}
		}
		
		protected virtual void OnAdded(BreakpointEventArgs e)
		{
			if (Added != null) {
				Added(this, e);
			}
		}
		
		public event BreakpointEventHandler Removed;
		public event BreakpointEventHandler Added;
	}
}
