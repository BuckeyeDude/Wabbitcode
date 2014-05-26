﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@Revsoft.net"/>
//     <version>$Revision: 2659 $</version>
// </file>

using System;
using System.Collections.Generic;

namespace Revsoft.TextEditor.Document
{
	/// <summary>
	/// Manages the list of markers and provides ways to retrieve markers for specific positions.
	/// </summary>
	public sealed class MarkerStrategy
	{
		List<TextMarker> textMarker = new List<TextMarker>();
		IDocument document;
		
		public IDocument Document {
			get {
				return document;
			}
		}
		
		public IEnumerable<TextMarker> TextMarker {
			get {
				return textMarker.AsReadOnly();
			}
		}
		
		public void AddMarker(TextMarker item)
		{
			markersTable.Clear();
            int startIndex = textMarker.BinarySearch(item);
            if (startIndex < 0)
            {
                startIndex = ~startIndex;
            }

			textMarker.Insert(startIndex, item);
		}
		
		public void RemoveMarker(TextMarker item)
		{
			markersTable.Clear();
            textMarker.Remove(item);
		}
		
		public void RemoveAll(Predicate<TextMarker> match)
		{
			markersTable.Clear();
			textMarker.RemoveAll(match);
		}
		
		public MarkerStrategy(IDocument document)
		{
			this.document = document;
			document.DocumentChanged += DocumentChanged;
		}
		
		Dictionary<int, List<TextMarker>> markersTable = new Dictionary<int, List<TextMarker>>();
		
		public List<TextMarker> GetMarkers(int offset)
		{
			if (!markersTable.ContainsKey(offset)) {
				List<TextMarker> markers = new List<TextMarker>();
				for (int i = 0; i < textMarker.Count; ++i) {
					TextMarker marker = textMarker[i];
					if (marker.Offset <= offset && offset <= marker.EndOffset) {
						markers.Add(marker);
					}
				}
				markersTable[offset] = markers;
			}
			return markersTable[offset];
		}
		
		public List<TextMarker> GetMarkers(int offset, int length)
		{
			int endOffset = offset + length - 1;
			List<TextMarker> markers = new List<TextMarker>();
		    if (textMarker.Count == 0)
		    {
		        return markers;
		    }

		    int startIndex = textMarker.BinarySearch(new TextMarker(offset, 1, TextMarkerType.SolidBlock),
                new StartMarkerComparer());
		    if (startIndex < 0)
		    {
		        startIndex = ~startIndex;
		    }

            int endIndex = textMarker.BinarySearch(new TextMarker(endOffset, 1, TextMarkerType.SolidBlock),
                new EndMarkerComparer());
		    if (endIndex < 0)
		    {
		        endIndex = ~endIndex;
		    }

			for (int i = startIndex; i < endIndex; ++i) {
				TextMarker marker = textMarker[i];
				if (// start in marker region
				    marker.Offset <= offset && offset <= marker.EndOffset ||
				    // end in marker region
				    marker.Offset <= endOffset && endOffset <= marker.EndOffset ||
				    // marker start in region
				    offset <= marker.Offset && marker.Offset <= endOffset ||
				    // marker end in region
				    offset <= marker.EndOffset && marker.EndOffset <= endOffset
				   )
				{
					markers.Add(marker);
				}
			}
			return markers;
		}
		
		public List<TextMarker> GetMarkers(TextLocation position)
		{
			if (position.Y >= document.TotalNumberOfLines || position.Y < 0) {
				return new List<TextMarker>();
			}
			LineSegment segment = document.GetLineSegment(position.Y);
			return GetMarkers(segment.Offset + position.X);
		}
		
		void DocumentChanged(object sender, DocumentEventArgs e)
		{
			// reset markers table
			markersTable.Clear();
			document.UpdateSegmentListOnDocumentChange(textMarker, e);
		}
	}

    public class StartMarkerComparer : IComparer<TextMarker>
    {
        public int Compare(TextMarker x, TextMarker y)
        {
            if (y.Offset >= x.Offset && y.Offset <= x.EndOffset)
            {
                return 0;
            }

            if (y.Offset < x.Offset)
            {
                return 1;
            }
            
            return -1;
        }
    }

    public class EndMarkerComparer : IComparer<TextMarker>
    {
        public int Compare(TextMarker x, TextMarker y)
        {
            if (y.EndOffset >= x.Offset)
            {
                return -1;
            }

            return 1;
        }
    }
}
