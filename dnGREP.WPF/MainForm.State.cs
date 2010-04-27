﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using dnGREP.Common;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Threading;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace dnGREP.WPF
{
	public class MainFormState : INotifyPropertyChanged
	{
		public static int FastBookmarkCapacity = 20;

		public MainFormState()
		{
            LoadAppSettings();
			UpdateState("Initial");
		}

		public GrepSettings settings
		{
			get { return GrepSettings.Instance; }
		}

        public void LoadAppSettings()
        {
            FileOrFolderPath = settings.Get<string>(GrepSettings.Key.SearchFolder);
            SearchFor = settings.Get<string>(GrepSettings.Key.SearchFor);
            ReplaceWith = settings.Get<string>(GrepSettings.Key.ReplaceWith);
            IncludeHidden = settings.Get<bool>(GrepSettings.Key.IncludeHidden);
			IncludeBinary = settings.Get<bool>(GrepSettings.Key.IncludeBinary);
			IncludeSubfolder = settings.Get<bool>(GrepSettings.Key.IncludeSubfolder);
            TypeOfSearch = settings.Get<SearchType>(GrepSettings.Key.TypeOfSearch);
            TypeOfFileSearch = settings.Get<FileSearchType>(GrepSettings.Key.TypeOfFileSearch);
			FilePattern = settings.Get<string>(GrepSettings.Key.FilePattern);
			FilePatternIgnore = settings.Get<string>(GrepSettings.Key.FilePatternIgnore);
            UseFileSizeFilter = settings.Get<FileSizeFilter>(GrepSettings.Key.UseFileSizeFilter);
			CaseSensitive = settings.Get<bool>(GrepSettings.Key.CaseSensitive);
			Multiline = settings.Get<bool>(GrepSettings.Key.Multiline);
			Singleline = settings.Get<bool>(GrepSettings.Key.Singleline);
			WholeWord = settings.Get<bool>(GrepSettings.Key.WholeWord);
            SizeFrom = settings.Get<int>(GrepSettings.Key.SizeFrom);
            SizeTo = settings.Get<int>(GrepSettings.Key.SizeTo);
			IsOptionsExpanded = settings.Get<bool>(GrepSettings.Key.IsOptionsExpanded);
			List<string> fsb = settings.Get<List<string>>(GrepSettings.Key.FastSearchBookmarks);
			if (fsb != null) 
			{
				foreach (string bookmark in fsb)
				{
					if (!FastSearchBookmarks.Contains(bookmark))
						FastSearchBookmarks.Add(bookmark);
				}
			}

			List<string> frb = settings.Get<List<string>>(GrepSettings.Key.FastReplaceBookmarks);
			if (frb != null)
			{
				foreach (string bookmark in frb)
				{
					if (!FastReplaceBookmarks.Contains(bookmark))
						FastReplaceBookmarks.Add(bookmark);
				}
			}

			List<string> ffmb = settings.Get<List<string>>(GrepSettings.Key.FastFileMatchBookmarks);
			if (ffmb != null)
			{
				foreach (string bookmark in ffmb)
				{
					if (!FastFileMatchBookmarks.Contains(bookmark))
						FastFileMatchBookmarks.Add(bookmark);
				}
			}

			List<string> ffnmb = settings.Get<List<string>>(GrepSettings.Key.FastFileNotMatchBookmarks);
			if (ffnmb != null)
			{
				foreach (string bookmark in ffnmb)
				{
					if (!FastFileNotMatchBookmarks.Contains(bookmark))
						FastFileNotMatchBookmarks.Add(bookmark);
				}
			}
        }

		private ObservableGrepSearchResults searchResults = new ObservableGrepSearchResults();
		public ObservableGrepSearchResults SearchResults
		{
			get {				
				return searchResults; 
			}
		}

		private ObservableCollection<string> fastSearchBookmarks = new ObservableCollection<string>();
		public ObservableCollection<string> FastSearchBookmarks
		{
			get { return fastSearchBookmarks; }
		}

		private ObservableCollection<string> fastReplaceBookmarks = new ObservableCollection<string>();
		public ObservableCollection<string> FastReplaceBookmarks
		{
			get { return fastReplaceBookmarks; }
		}

		private ObservableCollection<string> fastFileMatchBookmarks = new ObservableCollection<string>();
		public ObservableCollection<string> FastFileMatchBookmarks
		{
			get { return fastFileMatchBookmarks; }
		}

		private ObservableCollection<string> fastFileNotMatchBookmarks = new ObservableCollection<string>();
		public ObservableCollection<string> FastFileNotMatchBookmarks
		{
			get { return fastFileNotMatchBookmarks; }
		}

		private string _FileOrFolderPath = "";
		/// <summary>
		/// FileOrFolderPath property
		/// </summary>
		public string FileOrFolderPath
		{
			get { return _FileOrFolderPath; }
			set { 
				_FileOrFolderPath = value; 
				settings.Set<string>(GrepSettings.Key.SearchFolder, value);  
				UpdateState("FileOrFolderPath"); 
			}
		}

		private string _SearchFor = "";
		/// <summary>
		/// SearchFor property
		/// </summary>
		public string SearchFor
		{
			get { return _SearchFor; }
			set { 
				_SearchFor = value; 
				settings.Set<string>(GrepSettings.Key.SearchFor, value); 
				UpdateState("SearchFor"); 
			}
		}

		private string _ReplaceWith = "";
		/// <summary>
		/// ReplaceWith property
		/// </summary>
		public string ReplaceWith
		{
			get { return _ReplaceWith; }
			set { 
				_ReplaceWith = value; 
				settings.Set<string>(GrepSettings.Key.ReplaceWith, value); 
				UpdateState("ReplaceWith"); 
			}
		}

		private bool _IsOptionsExpanded = false;
		/// <summary>
		/// IsOptionsExpanded property
		/// </summary>
		public bool IsOptionsExpanded
		{
			get { return _IsOptionsExpanded; }
			set
			{
				_IsOptionsExpanded = value;
				settings.Set<bool>(GrepSettings.Key.IsOptionsExpanded, value);
				UpdateState("IsOptionsExpanded");
			}
		}
		
		private string _FilePattern = "";
		/// <summary>
		/// FilePattern property
		/// </summary>
		public string FilePattern
		{
			get { return _FilePattern; }
			set
			{
				_FilePattern = value;
				settings.Set<string>(GrepSettings.Key.FilePattern, value);
				UpdateState("FilePattern");
			}
		}

		private string _FilePatternIgnore = "";
		/// <summary>
		/// FilePatternIgnore property
		/// </summary>
		public string FilePatternIgnore
		{
			get { return _FilePatternIgnore; }
			set
			{
				_FilePatternIgnore = value;
				settings.Set<string>(GrepSettings.Key.FilePatternIgnore, value);
				UpdateState("FilePatternIgnore");
			}
		}

		private bool _IncludeSubfolder = false;
		/// <summary>
		/// IncludeSubfolder property
		/// </summary>
		public bool IncludeSubfolder
		{
			get { return _IncludeSubfolder; }
			set
			{
				_IncludeSubfolder = value;
				settings.Set<bool>(GrepSettings.Key.IncludeSubfolder, value);
				UpdateState("IncludeSubfolder");
			}
		}

		private bool _IncludeHidden = false;
		/// <summary>
		/// IncludeHidden property
		/// </summary>
		public bool IncludeHidden
		{
			get { return _IncludeHidden; }
			set
			{
				_IncludeHidden = value;
				settings.Set<bool>(GrepSettings.Key.IncludeHidden, value);
				UpdateState("IncludeHidden");
			}
		}

		private bool _IncludeBinary = false;
		/// <summary>
		/// IncludeBinary property
		/// </summary>
		public bool IncludeBinary
		{
			get { return _IncludeBinary; }
			set { 
				_IncludeBinary = value;
				settings.Set<bool>(GrepSettings.Key.IncludeBinary, value);
				UpdateState("IncludeBinary"); 
			}
		}

		private SearchType _TypeOfSearch = SearchType.PlainText;
		/// <summary>
		/// TypeOfSearch property
		/// </summary>
		public SearchType TypeOfSearch
		{
			get { return _TypeOfSearch; }
			set
			{
				_TypeOfSearch = value;
				settings.Set<SearchType>(GrepSettings.Key.TypeOfSearch, value);
				UpdateState("TypeOfSearch");
			}
		}

		private FileSearchType _TypeOfFileSearch = FileSearchType.Asterisk;
		/// <summary>
		/// TypeOfFileSearch property
		/// </summary>
		public FileSearchType TypeOfFileSearch
		{
			get { return _TypeOfFileSearch; }
			set
			{
				_TypeOfFileSearch = value;
				settings.Set<FileSearchType>(GrepSettings.Key.TypeOfFileSearch, value);
				UpdateState("TypeOfFileSearch");
			}
		}

		private FileSizeFilter _UseFileSizeFilter = FileSizeFilter.No;
		/// <summary>
		/// UseFileSizeFilter property
		/// </summary>
		public FileSizeFilter UseFileSizeFilter
		{
			get { return _UseFileSizeFilter; }
			set
			{
				_UseFileSizeFilter = value;
				settings.Set<FileSizeFilter>(GrepSettings.Key.UseFileSizeFilter, value);
				UpdateState("UseFileSizeFilter");
			}
		}

		private int sizeFrom = 0;
		/// <summary>
		/// SizeFrom property
		/// </summary>
		public int SizeFrom
		{
			get { return sizeFrom; }
			set { 
				sizeFrom = value;
				settings.Set<int>(GrepSettings.Key.SizeFrom, value);
				UpdateState("SizeFrom");
			}
		}

		private int _SizeTo = 1000;
		/// <summary>
		/// SizeTo property
		/// </summary>
		public int SizeTo
		{
			get { return _SizeTo; }
			set
			{
				_SizeTo = value;
				settings.Set<int>(GrepSettings.Key.SizeTo, value);
				UpdateState("SizeTo");
			}
		}

		private bool _CaseSensitive = true;
		/// <summary>
		/// CaseSensitive property
		/// </summary>
		public bool CaseSensitive
		{
			get { return _CaseSensitive; }
			set
			{
				_CaseSensitive = value;
				settings.Set<bool>(GrepSettings.Key.CaseSensitive, value);
				UpdateState("CaseSensitive");
			}
		}

		private bool _IsCaseSensitiveEnabled = true;
		/// <summary>
		/// IsCaseSensitiveEnabled property
		/// </summary>
		public bool IsCaseSensitiveEnabled
		{
			get { return _IsCaseSensitiveEnabled; }
			set
			{
				_IsCaseSensitiveEnabled = value;
				UpdateState("IsCaseSensitiveEnabled");
			}
		}

		private bool _Multiline = false;
		/// <summary>
		/// Multiline property
		/// </summary>
		public bool Multiline
		{
			get { return _Multiline; }
			set
			{
				_Multiline = value;
				settings.Set<bool>(GrepSettings.Key.Multiline, value);
				UpdateState("Multiline");
			}
		}

		private bool _IsMultilineEnabled = true;
		/// <summary>
		/// IsMultilineEnabled property
		/// </summary>
		public bool IsMultilineEnabled
		{
			get { return _IsMultilineEnabled; }
			set
			{
				_IsMultilineEnabled = value;
				UpdateState("IsMultilineEnabled");
			}
		}

		private bool _Singleline = false;
		/// <summary>
		/// Singleline property
		/// </summary>
		public bool Singleline
		{
			get { return _Singleline; }
			set
			{
				_Singleline = value;
				settings.Set<bool>(GrepSettings.Key.Singleline, value);
				UpdateState("Singleline");
			}
		}


		private bool _IsSinglelineEnables = true;
		/// <summary>
		/// IsSinglelineEnables property
		/// </summary>
		public bool IsSinglelineEnabled
		{
			get { return _IsSinglelineEnables; }
			set
			{
				_IsSinglelineEnables = value;
				UpdateState("IsSinglelineEnabled");
			}
		}


		private bool _WholeWord = false;
		/// <summary>
		/// WholeWord property
		/// </summary>
		public bool WholeWord
		{
			get { return _WholeWord; }
			set
			{
				_WholeWord = value;
				settings.Set<bool>(GrepSettings.Key.WholeWord, value);
				UpdateState("WholeWord");
			}
		}

		private bool _IsWholeWordEnabled = true;
		/// <summary>
		/// IsWholeWordEnabled property
		/// </summary>
		public bool IsWholeWordEnabled
		{
			get { return _IsWholeWordEnabled; }
			set { _IsWholeWordEnabled = value; UpdateState("IsWholeWordEnabled"); }
		}
		
		#region Derived properties
		private bool _IsSizeFilterSet = true;
		/// <summary>
		/// IsSizeFilterSet property
		/// </summary>
		public bool IsSizeFilterSet
		{
			get { return _IsSizeFilterSet; }
			set { _IsSizeFilterSet = value; UpdateState("IsSizeFilterSet");	}
		}
		
		private bool filesFound = false;
		public bool FilesFound
		{
			get { return filesFound; }
			set { filesFound = value; UpdateState("FilesFound"); }
		}

		private bool _CanSearch = true;
		/// <summary>
		/// CanSearch property
		/// </summary>
		public bool CanSearch
		{
			get { return _CanSearch; }
			set { _CanSearch = value; UpdateState("CanSearch"); }
		}

		private bool _CanSearchInResults = true;
		/// <summary>
		/// CanSearchInResults property
		/// </summary>
		public bool CanSearchInResults
		{
			get { return _CanSearchInResults; }
			set { _CanSearchInResults = value; UpdateState("CanSearchInResults"); }
		}

		private string _SearchButtonMode = "";
		/// <summary>
		/// SearchButtonMode property
		/// </summary>
		public string SearchButtonMode
		{
			get { return _SearchButtonMode; }
			set { _SearchButtonMode = value; UpdateState("SearchButtonMode"); }
		}

		private bool _CanReplace = false;
		/// <summary>
		/// CanReplace property
		/// </summary>
		public bool CanReplace
		{
			get { return _CanReplace; }
			set { _CanReplace = value; UpdateState("CanReplace"); }
		}

		private bool _CanCancel = false;
		/// <summary>
		/// CanCancel property
		/// </summary>
		public bool CanCancel
		{
			get { return _CanCancel; }
			set { _CanCancel = value; UpdateState("CanCancel"); }
		}

		private GrepOperation _CurrentGrepOperation = GrepOperation.None;
		/// <summary>
		/// CurrentGrepOperation property
		/// </summary>
		public GrepOperation CurrentGrepOperation
		{
			get { return _CurrentGrepOperation; }
			set { _CurrentGrepOperation = value; UpdateState("CurrentGrepOperation"); }
		}

		private string _OptionsSummary = "";
		/// <summary>
		/// OptionsSummary property
		/// </summary>
		public string OptionsSummary
		{
			get { return _OptionsSummary; }
			set { _OptionsSummary = value; UpdateState("OptionsSummary"); }
		}

		private string _TextBoxStyle = "";
		/// <summary>
		/// TextBoxStyle property
		/// </summary>
		public string TextBoxStyle
		{
			get { return _TextBoxStyle; }
			set { _TextBoxStyle = value; UpdateState("TextBoxStyle"); }
		}

		private int _CodePage = 0;
		/// <summary>
		/// CodePage property
		/// </summary>
		public int CodePage
		{
			get { return _CodePage; }
			set { _CodePage = value; UpdateState("CodePage"); }
		}

		private bool _CanUndo = false;
		/// <summary>
		/// CanUndo property
		/// </summary>
		public bool CanUndo
		{
			get { return _CanUndo; }
			set { _CanUndo = value; UpdateState("CanUndo"); }
		}

		private string _UndoFolder = "";
		/// <summary>
		/// UndoFolder property
		/// </summary>
		public string UndoFolder
		{
			get { return _UndoFolder; }
			set { _UndoFolder = value; UpdateState("UndoFolder"); }
		}

		#endregion
		public virtual void UpdateState(string name)
		{
			OnPropertyChanged(name);

			switch (name)
			{
				case "UseFileSizeFilter":
					if (UseFileSizeFilter == FileSizeFilter.Yes)
					{
						IsSizeFilterSet = true;
					}
					else
					{
						IsSizeFilterSet = false;
					}
					break;
				case "Initial":
				case "Multiline":
				case "Singleline":
				case "WholeWord":
				case "CaseSensitive":
					List<string> tempList = new List<string>();
					if (CaseSensitive)
						tempList.Add("Case sensitive");
					if (Multiline)
						tempList.Add("Multiline");
					if (WholeWord)
						tempList.Add("Whole word");
					if (Singleline)
						tempList.Add("Match dot as new line");
					OptionsSummary = "[";
					if (tempList.Count == 0)
					{
						OptionsSummary += "None";
					}
					else
					{
						for (int i = 0; i < tempList.Count; i++)
						{
							OptionsSummary += tempList[i];
							if (i < tempList.Count - 1)
								OptionsSummary += ", ";
						}
					}
					OptionsSummary += "]";

					if (Multiline)
						TextBoxStyle = "{StaticResource ExpandedTextbox}";
					else
						TextBoxStyle = "";
					
					break;
			}

            //Files found
			if (name == "FileOrFolderPath" || name == "SearchFor" || name == "FilePattern" || name == "FilePatternIgnore")
            {
                FilesFound = false;
            }

			//Can search
			if (name == "FileOrFolderPath" || name == "CurrentGrepOperation" || name == "SearchFor")
			{
				if (Utils.IsPathValid(FileOrFolderPath) && CurrentGrepOperation == GrepOperation.None &&
					(!string.IsNullOrEmpty(SearchFor) || settings.Get<bool>(GrepSettings.Key.AllowSearchingForFileNamePattern)))
				{
					CanSearch = true;
				}
				else
				{
					CanSearch = false;
				}
			}

			//btnSearch.ShowAdvance
			if (name == "CurrentGrepOperation" || name == "Initial")
			{
				if (searchResults.Count > 0)
				{
					//TODO
					CanSearchInResults = true;
					SearchButtonMode = "Split";
				}
				else
				{
					//TODO
					CanSearchInResults = false;
					SearchButtonMode = "Button";
				}
			}

			//searchResults
			searchResults.FolderPath = FileOrFolderPath;

			// btnReplace
			if (name == "FileOrFolderPath" || name == "FilesFound" || name == "CurrentGrepOperation" || name == "SearchFor")
			{
				if (Utils.IsPathValid(FileOrFolderPath) && FilesFound && CurrentGrepOperation == GrepOperation.None &&
					!string.IsNullOrEmpty(SearchFor))
				{
					CanReplace = true;
				}
				else
				{
					CanReplace = false;
				}
			}

			//btnCancel
			if (name == "CurrentGrepOperation")
			{
				if (CurrentGrepOperation != GrepOperation.None)
				{
					CanCancel = true;
				}
				else
				{
					CanCancel = false;
				}
			}

			//Search type specific options
			if (name == "TypeOfSearch")
			{
				if (TypeOfSearch == SearchType.XPath)
				{
					IsCaseSensitiveEnabled = false;
					IsMultilineEnabled = false;
					IsSinglelineEnabled = false;
					IsWholeWordEnabled = false;
					CaseSensitive = false;
					Multiline = false;
					Singleline = false;
					WholeWord = false;
				}
				else if (TypeOfSearch == SearchType.PlainText)
				{
					IsCaseSensitiveEnabled = true;
					IsMultilineEnabled = true;
					IsSinglelineEnabled = false;
					IsWholeWordEnabled = true;
					Singleline = false;
				}
				else if (TypeOfSearch == SearchType.Soundex)
				{
					IsMultilineEnabled = true;
					IsCaseSensitiveEnabled = false;
					IsSinglelineEnabled = false;
					IsWholeWordEnabled = true;
					CaseSensitive = false;
					Singleline = false;
				}
				else if (TypeOfSearch == SearchType.Regex)
				{
					IsCaseSensitiveEnabled = true;
					IsMultilineEnabled = true;
					IsSinglelineEnabled = true;
					IsWholeWordEnabled = true;
				}
			}
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		// Create the OnPropertyChanged method to raise the event
		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(name));
			}
		}

		#endregion
	}
}