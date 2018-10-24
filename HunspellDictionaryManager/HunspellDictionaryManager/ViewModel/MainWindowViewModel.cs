﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Sdl.Community.HunspellDictionaryManager.Commands;
using Sdl.Community.HunspellDictionaryManager.Helpers;
using Sdl.Community.HunspellDictionaryManager.Model;
using Sdl.Community.HunspellDictionaryManager.Ui;
using Sdl.Core.Globalization;

namespace Sdl.Community.HunspellDictionaryManager.ViewModel
{
	public class MainWindowViewModel : ViewModelBase
	{
		#region Private Fields
		private MainWindow _mainWindow;
		private ObservableCollection<HunspellLangDictionaryModel> _dictionaryLanguages = new ObservableCollection<HunspellLangDictionaryModel>();
		private ObservableCollection<LanguageModel> _languages = new ObservableCollection<LanguageModel>();
		private ObservableCollection<HunspellLangDictionaryModel> _undoDictionaries = new ObservableCollection<HunspellLangDictionaryModel>();
		private HunspellLangDictionaryModel _selectedDictionaryLanguage;
		private HunspellLangDictionaryModel _selectedUndoDictionary;
		private HunspellLangDictionaryModel _deletedDictionaryLanguage;
		private Language[] _studioLanguages = Language.GetAllLanguages().OrderBy(s => s.DisplayName).ToArray();
		private List<string> _backupFiles;
		private LanguageModel _newDictionaryLanguage;
		private string _resultMessageColor;
		private string _labelVisibility = Constants.Hidden;
		private string _undoLabelVisibility = Constants.Hidden;
		private string _undoDictVisibility = Constants.Hidden;
		private string _resultMessage;
		private ICommand _createHunspellDictionaryCommand;
		private ICommand _closeCommand;
		private ICommand _deleteCommand;
		private ICommand _refreshCommand;
		private ICommand _helpCommand;
		private ICommand _undoCommand;
		private static string _hunspellDictionariesFolderPath;
		private static string _backupFolderPath;
		private static string _undoDictFolderPath = Path.Combine(Path.GetTempPath(), Constants.UndoHunspellDictionaries);
		#endregion

		#region Constructors
		public MainWindowViewModel(MainWindow mainWindow)
		{
			BackupHunspellDictionaries();

			_mainWindow = mainWindow;
			LoadDictionariesLanguages();
			LoadStudioLanguages();
			LoadUndoDictionaries();
		}
		#endregion

		#region Public Properties
		public HunspellLangDictionaryModel SelectedDictionaryLanguage
		{
			get => _selectedDictionaryLanguage;
			set
			{
				_selectedDictionaryLanguage = value;
				OnPropertyChanged();
			}
		}

		public HunspellLangDictionaryModel DeletedDictionaryLanguage
		{
			get => _deletedDictionaryLanguage;
			set
			{
				_deletedDictionaryLanguage = value;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<HunspellLangDictionaryModel> DictionaryLanguages
		{
			get => _dictionaryLanguages;
			set
			{
				_dictionaryLanguages = value;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<HunspellLangDictionaryModel> UndoDictionaries
		{
			get => _undoDictionaries;
			set
			{
				_undoDictionaries = value;
				OnPropertyChanged();
			}
		}

		public HunspellLangDictionaryModel SelectedUndoDictionary
		{
			get => _selectedUndoDictionary;
			set
			{
				_selectedUndoDictionary = value;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<LanguageModel> Languages
		{
			get => _languages;
			set
			{
				_languages = value;
				OnPropertyChanged();
			}
		}

		public LanguageModel NewDictionaryLanguage
		{
			get => _newDictionaryLanguage;
			set
			{
				_newDictionaryLanguage = value;
				OnPropertyChanged();
			}
		}

		public string LabelVisibility
		{
			get => _labelVisibility;
			set
			{
				_labelVisibility = value;
				OnPropertyChanged();
			}
		}

		public string UndoLabelVisibility
		{
			get => _undoLabelVisibility;
			set
			{
				_undoLabelVisibility = value;
				OnPropertyChanged();
			}
		}

		public string UndoDictVisibility
		{
			get => _undoDictVisibility;
			set
			{
				_undoDictVisibility = value;
				OnPropertyChanged();
			}
		}

		public string ResultMessage
		{
			get => _resultMessage;
			set
			{
				_resultMessage = value;
				OnPropertyChanged();
			}
		}

		public string ResultMessageColor
		{
			get => _resultMessageColor;
			set
			{
				_resultMessageColor = value;
				OnPropertyChanged();
			}
		}
		#endregion

		#region Commands
		public ICommand CreateHunspellDictionaryCommand => _createHunspellDictionaryCommand ?? (_createHunspellDictionaryCommand = new CommandHandler(CreateHunspellDictionaryAction, true));
		public ICommand CloseCommand => _closeCommand ?? (_closeCommand = new CommandHandler(CloseAction, true));
		public ICommand DeleteCommand => _deleteCommand ?? (_deleteCommand = new CommandHandler(DeleteAction, true));
		public ICommand RefreshCommand => _refreshCommand ?? (_refreshCommand = new CommandHandler(RefreshAction, true));
		public ICommand HelpCommand => _helpCommand ?? (_helpCommand = new CommandHandler(HelpAction, true));
		public ICommand UndoCommand => _undoCommand ?? (_undoCommand = new CommandHandler(UndoAction, true));
		#endregion

		#region Actions
		private void CreateHunspellDictionaryAction()
		{
			LabelVisibility = Constants.Hidden;
			if (NewDictionaryLanguage != null)
			{
				CopyFiles();
			}
		}

		private void CloseAction()
		{
			if (_mainWindow.IsLoaded)
			{
				_mainWindow.Close();
			}
		}

		private void DeleteAction()
		{
			LabelVisibility = Constants.Hidden;
			if (DeletedDictionaryLanguage != null)
			{
				// add dictionaries which are deleted in a temp folder (used for Undo action)
				AddUndoDictionaries();
				DeleteSelectedFies();
				RemoveConfigLanguageNode();

				UndoLabelVisibility = Constants.Visible;
				UndoDictVisibility = Constants.Visible;
			}
		}

		private void RefreshAction()
		{
			NewDictionaryLanguage = new LanguageModel();
			DeletedDictionaryLanguage = new HunspellLangDictionaryModel();
			SelectedDictionaryLanguage = new HunspellLangDictionaryModel();
			DictionaryLanguages = new ObservableCollection<HunspellLangDictionaryModel>();
			Languages = new ObservableCollection<LanguageModel>();
			UndoDictionaries = new ObservableCollection<HunspellLangDictionaryModel>();

			LoadDictionariesLanguages();
			LoadStudioLanguages();
			LoadUndoDictionaries();
			LabelVisibility = Constants.Hidden;
			if (!UndoDictionaries.Any())
			{
				UndoLabelVisibility = Constants.Hidden;
				UndoDictVisibility = Constants.Hidden;
			}
		}

		/// <summary>
		/// Undo the deletion of dictionaries by moving back to HunspellDictionaries original folder the selected dictionary
		/// </summary>
		private void UndoAction()
		{
			// Selected dictionary and .aff files for Undo action
			var dictionaryFilePath = Path.Combine(_hunspellDictionariesFolderPath, Path.GetFileName(SelectedUndoDictionary.DictionaryFile));
			var affFilePath = Path.Combine(_hunspellDictionariesFolderPath, Path.GetFileName(SelectedUndoDictionary.AffFile));

			var undoHunspellDictionaryModel = new HunspellLangDictionaryModel
			{
				DictionaryFile = SelectedUndoDictionary.DictionaryFile,
				AffFile = SelectedUndoDictionary.AffFile
			};

			// Copy files in the original Studio HunspellDictionaries folder
			var isoCode = Path.GetFileNameWithoutExtension(SelectedUndoDictionary.AffFile);
			CopyLanguageDictionary(undoHunspellDictionaryModel, dictionaryFilePath, affFilePath, isoCode);

			RemoveDictFromDeleteFolder();
			RefreshAction();
			ResultMessage = Constants.UndoSuccessMessage;
			LabelVisibility = Constants.Visible;
		}

		private void HelpAction()
		{
			System.Diagnostics.Process.Start(Constants.HelpLink);
		}
		#endregion

		#region Private Methods

		/// <summary>
		/// Load .dic and .aff files from the installed Studio location -> HunspellDictionaries folder
		/// and display the dictionary language name which is compatible with Studio 
		/// </summary>
		private void LoadDictionariesLanguages()
		{
			if (!string.IsNullOrEmpty(_hunspellDictionariesFolderPath))
			{
				FormatDictionaryFiles(_hunspellDictionariesFolderPath, DictionaryLanguages);
			}
			else
			{
				_mainWindow.Close();
			}
		}

		/// <summary>
		/// Copy selected (.dic and .aff) files and rename them using the specified dictionary language
		/// If dictionary files already exists in folder, allow user the possibility to override or not the file
		/// </summary>
		private void CopyFiles()
		{
			var newDictionaryFilePath = Path.Combine(_hunspellDictionariesFolderPath, $"{NewDictionaryLanguage.IsoCode.Replace('-', '_')}.dic");
			var newAffFilePath = Path.Combine(_hunspellDictionariesFolderPath, $"{NewDictionaryLanguage.IsoCode.Replace('-', '_')}.aff");

			if (DictionaryLanguages.Any(d => d.DictionaryFile.Equals(newDictionaryFilePath)))
			{
				var result = MessageBox.Show(Constants.DictionaryAlreadyExists, Constants.InformativeMessage, MessageBoxButton.YesNo, MessageBoxImage.Warning);
				if (result.Equals(MessageBoxResult.Yes))
				{
					CopyLanguageDictionary(SelectedDictionaryLanguage, newDictionaryFilePath, newAffFilePath, NewDictionaryLanguage.IsoCode);
				}
			}
			else
			{
				CopyLanguageDictionary(SelectedDictionaryLanguage, newDictionaryFilePath, newAffFilePath, NewDictionaryLanguage.IsoCode);
			}
		}

		/// <summary>
		/// Update spellcheckmanager_config.xml file by adding a new node which contains the new dictionary language
		/// </summary>
		private void UpdateConfigFile(string isoCode)
		{
			// load xml config file
			var configFilePath = Path.Combine(_hunspellDictionariesFolderPath, Constants.ConfigFileName);
			var xmlDoc = XDocument.Load(configFilePath);

			// add new language dictionary if doesn't already exists in the config file
			var languageElem = (string)xmlDoc.Root.Elements("language").FirstOrDefault(x => (string)x.Element("isoCode") == isoCode);
			if (string.IsNullOrEmpty(languageElem))
			{
				var node = new XElement("language",
					new XElement("isoCode", isoCode), new XElement("dict", isoCode.Replace('-', '_')));

				xmlDoc.Element("config").Add(node);
				xmlDoc.Save(configFilePath);

				LoadDictionariesLanguages();
			}
			LabelVisibility = Constants.Visible;
			SetGridSettings(Constants.SuccessfullCreateMessage, Constants.GreenColor);
		}

		/// <summary>
		/// Delete from HunspellDictionaries folder the .aff and .dic files based on user selection
		/// </summary>
		private void DeleteSelectedFies()
		{
			if (File.Exists(DeletedDictionaryLanguage.DictionaryFile))
			{
				File.Delete(DeletedDictionaryLanguage.DictionaryFile);
			}
			if (File.Exists(DeletedDictionaryLanguage.AffFile))
			{
				File.Delete(DeletedDictionaryLanguage.AffFile);
			}
		}

		/// <summary>
		/// Remove corresponding nodes from the xml config file
		/// </summary>
		private void RemoveConfigLanguageNode()
		{
			// load xml config file
			var configFilePath = Path.Combine(_hunspellDictionariesFolderPath, Constants.ConfigFileName);
			var xmlDoc = XDocument.Load(configFilePath);
			var dictionaryLanguage = Path.GetFileNameWithoutExtension(DeletedDictionaryLanguage.DictionaryFile);

			// remove the language dictionary from the config
			var languageElem = xmlDoc.Root.Elements("language").FirstOrDefault(x => (string)x.Element("dict") == dictionaryLanguage);

			if (languageElem != null)
			{
				languageElem.Remove();
				xmlDoc.Save(configFilePath);
				SetGridSettings(Constants.SuccessfullDeleteMessage, Constants.GreenColor);
			}
			else
			{
				SetGridSettings(Constants.NoLanguageDictionaryFound, Constants.RedColor);
			}
			RefreshAction();
			LabelVisibility = Constants.Visible;
		}

		private void SetGridSettings(string resultMessage, string resultMessageColor)
		{
			ResultMessage = resultMessage;
			ResultMessageColor = resultMessageColor;
		}

		/// <summary>
		/// Set dictionary language display name based on the studio languages
		/// </summary>
		/// <param name="hunspellDictionaryName">hunspell dictionary name</param>
		/// <param name="studioLanguages">all Studio languages</param>
		/// <returns>Dictionary display language name</returns>
		private string SetDisplayLanguageName(string hunspellDictionaryName)
		{
			hunspellDictionaryName = hunspellDictionaryName.Replace('_', '-');
			//search for Language based on IsoAbbreviation
			var displayLanguageName = _studioLanguages.Where(a => a.IsoAbbreviation.Equals(hunspellDictionaryName)).FirstOrDefault();
			if (displayLanguageName != null)
			{
				return displayLanguageName.DisplayName;
			}
			else
			{
				// search for Language based on TwoLetterISOLanguageName
				displayLanguageName = _studioLanguages.Where(a => a.CultureInfo.TwoLetterISOLanguageName.Equals(hunspellDictionaryName)).FirstOrDefault();
				if (displayLanguageName != null)
				{
					return displayLanguageName.DisplayName;
				}
			}
			return string.Empty;
		}

		/// <summary>
		/// Load languages which are compatible in Studio
		/// </summary>
		private void LoadStudioLanguages()
		{
			foreach (var language in _studioLanguages)
			{
				var languageModel = new LanguageModel
				{
					LanguageName = language.DisplayName,
					IsoCode = language.IsoAbbreviation,
					TwoLetterISOLanguageName = language.CultureInfo.TwoLetterISOLanguageName
				};
				Languages.Add(languageModel);
			}
		}

		/// <summary>
		/// Copy dictionaries files to the specific location and also update the spellcheckmanager_config file based on the isoCode
		/// </summary>
		private void CopyLanguageDictionary(HunspellLangDictionaryModel selectedDictionaryLanguage, string newDictionaryFilePath, string newAffFilePath, string isoCode)
		{
			File.Copy(selectedDictionaryLanguage.DictionaryFile, newDictionaryFilePath, true);
			File.Copy(selectedDictionaryLanguage.AffFile, newAffFilePath, true);
			UpdateConfigFile(isoCode);
		}

		/// <summary>
		/// Backup Hunspell Dictionaries from Studio location to a nwe temporary folder (used when user deletes dictionary and wants to undo the action)
		/// </summary>
		private void BackupHunspellDictionaries()
		{
			var studioPath = Utils.GetInstalledStudioPath();
			_backupFolderPath = Path.Combine(Path.GetTempPath(), Constants.HunspellDictionaries);

			var task = System.Threading.Tasks.Task.Factory.StartNew(() =>
			{
				if (Directory.Exists(_backupFolderPath))
				{
					Directory.Delete(_backupFolderPath, true);
				}
			});
			task.Wait();

			var createFolder = System.Threading.Tasks.Task.Factory.StartNew(() =>
			{
				Directory.CreateDirectory(_backupFolderPath);
			});
			createFolder.Wait();
			if (!string.IsNullOrEmpty(studioPath))
			{
				_hunspellDictionariesFolderPath = Path.Combine(Path.GetDirectoryName(studioPath), Constants.HunspellDictionaries);
				_backupFiles = Directory.EnumerateFiles(_hunspellDictionariesFolderPath).ToList();
				foreach (var file in _backupFiles)
				{
					File.Copy(file, Path.Combine(_backupFolderPath, Path.GetFileName(file)));
				}
			}
		}

		/// <summary>
		/// Add dictionaries which were deleted in a temporary folder (used for the Undo action)
		/// </summary>
		private void AddUndoDictionaries()
		{
			if (!Directory.Exists(_undoDictFolderPath))
			{
				Directory.CreateDirectory(_undoDictFolderPath);
			}
			UndoDictionaries.Add(DeletedDictionaryLanguage);
			var dictFileName = Path.GetFileName(DeletedDictionaryLanguage.DictionaryFile);
			var affFileName = Path.GetFileName(DeletedDictionaryLanguage.AffFile);

			File.Copy(DeletedDictionaryLanguage.DictionaryFile, Path.Combine(_undoDictFolderPath, dictFileName), true);
			File.Copy(DeletedDictionaryLanguage.AffFile, Path.Combine(_undoDictFolderPath, affFileName), true);
		}

		/// <summary>
		/// Load dictionaries which were deleted and can be undo
		/// </summary>
		private void LoadUndoDictionaries()
		{
			if (Directory.Exists(_undoDictFolderPath))
			{
				var undoDictionaries = FormatDictionaryFiles(_undoDictFolderPath, UndoDictionaries);
				if (undoDictionaries.Any())
				{
					UndoLabelVisibility = Constants.Visible;
					UndoDictVisibility = Constants.Visible;
				}
				else
				{
					UndoLabelVisibility = Constants.Hidden;
					UndoDictVisibility = Constants.Hidden;
				}
			}
		}

		/// <summary>
		/// Remove dictionary from the Undo temp folder which was copied back to the original Studio HunspellDictionaries folder
		/// </summary>
		private void RemoveDictFromDeleteFolder()
		{
			if (SelectedUndoDictionary != null)
			{
				var deletedFiles = Directory.GetFiles(_undoDictFolderPath);
				var recoveredDictFile = deletedFiles.Where(a => Path.GetFileName(a).Equals(Path.GetFileName(SelectedUndoDictionary.DictionaryFile))).FirstOrDefault();
				var recoveredAffFile = deletedFiles.Where(a => Path.GetFileName(a).Equals(Path.GetFileName(SelectedUndoDictionary.AffFile))).FirstOrDefault();

				if (!string.IsNullOrEmpty(recoveredDictFile))
				{
					File.Delete(recoveredDictFile);
				}
				if (!string.IsNullOrEmpty(recoveredAffFile))
				{
					File.Delete(recoveredAffFile);
				}
			}
		}

		/// <summary>
		/// Get dictionaries .dict and .aff files from the Studio/temp location and add them to the observable collection.
		/// Used to display dictionaries in the Main Window.
		/// </summary>
		/// <param name="dictionaryFolderPath">dictionary folder path</param>
		/// <param name="dictionaries">dictionaries</param>
		/// <returns></returns>
		private ObservableCollection<HunspellLangDictionaryModel> FormatDictionaryFiles(string dictionaryFolderPath, ObservableCollection<HunspellLangDictionaryModel> dictionaries)
		{
			// get .dic files from Studio HunspellDictionaries folder
			var dictionaryFiles = Directory.GetFiles(dictionaryFolderPath, "*.dic").ToList();
			foreach (var hunspellDictionary in dictionaryFiles)
			{
				var hunspellLangDictionaryModel = new HunspellLangDictionaryModel()
				{
					DictionaryFile = hunspellDictionary,
					ShortLanguageName = Path.GetFileNameWithoutExtension(hunspellDictionary),
					DisplayLanguageName = SetDisplayLanguageName(Path.GetFileNameWithoutExtension(hunspellDictionary))
				};
				// add to dropdown list only dictionaries that has language correspondence in Studio
				if (!string.IsNullOrEmpty(hunspellLangDictionaryModel.DisplayLanguageName))
				{
					dictionaries.Add(hunspellLangDictionaryModel);
				}
			}

			// get .aff files from Studio HunspellDictionaries folder
			var affFiles = Directory.GetFiles(dictionaryFolderPath, "*.aff").ToList();
			foreach (var dicFile in dictionaries)
			{
				var affFile = affFiles.Where(d => d.Contains($"{dicFile.ShortLanguageName}.aff")).FirstOrDefault();
				if (affFile != null)
				{
					dicFile.AffFile = affFile;
				}
			}
			return new ObservableCollection<HunspellLangDictionaryModel>(dictionaries.OrderBy(d => d.DisplayLanguageName));
		}

		#endregion
	}
}