// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using UnityEngine;
using System.IO;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility.AssetInjection;
using System.Text;
using DaggerfallWorkshop.Game.Utility.ModSupport;

namespace DaggerfallWorkshop.Game
{
    /// <summary>
    /// Represents data for localized book format.
    /// Can import classic books and export localized file.
    /// </summary>
    public class LocalizedBook
    {
        const string localizedFilenameSuffix = "-LOC";
        const string fileExtension = ".txt";
        const string textFolderName = "Text";
        const string booksFolderName = "Books";

        // Formatting markup
        const string markupJustifyLeft = "[/left]";
        const string markupJustifyCenter = "[/center]";
        const string markupFont = "[/font={0}]";

        // Keywords for save/load localized book format
        const string titleKeyword = "Title:";
        const string authorKeyword = "Author:";
        const string isNaughtyKeyword = "IsNaughty:";
        const string priceKeyword = "Price:";
        const string isUniqueKeyword = "IsUnique:";
        const string whenVarSetKeyword = "WhenVarSet:";
        const string contentKeyword = "Content:";

        // Book data fields
        public string Title;        // Title of book
        public string Author;       // Author's name
        public bool IsNaughty;      // Flagged for adult content
        public int Price;           // Base price for merchants
        public bool IsUnique;       // Book only available to mods and will not appear in loot tables or shelves
        public string WhenVarSet;   // Book only available in loot tables once this GlobalVar is set
        public StringBuilder Content = new(); // Text content of book

        /// <summary>
        /// Opens either localized or classic book file.
        /// Seeks localized books first then classic books.
        /// </summary>
        /// <param name="filename">Filename of classic book file, e.g. "BOK00042.txt".</param>
        /// <returns>True if successful.</returns>

        public bool OpenBookFile(string filename)
        {
            return OpenLocalizedBookFile(filename) || OpenClassicBookFile(filename);
        }

        /// <summary>
        /// Checks if a localized book file exists using ID.
        /// </summary>
        /// <param name="id">ID of book.</param>
        /// <returns>True if localized book file exists for this ID.</returns>
        public static bool Exists(int id)
        {
            return Exists(DaggerfallUnity.Instance.ItemHelper.GetBookFileName(id));
        }

        /// <summary>
        /// Checks if a localized book file exists using book filename.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>True if localized book file exists for this filename.</returns>
        public static bool Exists(string filename)
        {
            // Append -LOC if missing from filename
            string fileNoExt = Path.GetFileNameWithoutExtension(filename);
            if (!fileNoExt.EndsWith(localizedFilenameSuffix))
                filename = fileNoExt + localizedFilenameSuffix + fileExtension;

            // Seek localized book file from mods
            if (ModManager.Instance != null && ModManager.Instance.TryGetAsset(filename, false, out TextAsset _))
            {
                return true;
            }

            // Get path to localized book file and check it exists
            string path = Path.Combine(Paths.StreamingAssetsPath, textFolderName, booksFolderName, filename);
            return File.Exists(path);
        }

        /// <summary>
        /// Opens a classic book file.
        /// Seeks classic book files from ARENA2 and mods.
        /// Formatting of classic books is unreliable and often broken in classic data.
        /// Opening a localized book cleaned up by a human is always preferred.
        /// </summary>
        /// <param name="filename">Filename of classic book file, e.g. "BOK00042.txt".</param>
        /// <returns>True if successful.</returns>
        public bool OpenClassicBookFile(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return false;

            BookFile bookFile = new BookFile();
            if (!BookReplacement.TryImportBook(filename, bookFile) &&
                !bookFile.OpenBook(DaggerfallUnity.Instance.Arena2Path, filename))
                return false;

            Title = bookFile.Title;
            Author = bookFile.Author;
            IsNaughty = bookFile.IsNaughty;
            Price = bookFile.Price;
            IsUnique = false;
            WhenVarSet = string.Empty;
            Content.Clear();

            for (int page = 0; page < bookFile.PageCount; page++)
            {
                TextFile.Token[] tokens = bookFile.GetPageTokens(page);
                ConvertTokensToString(tokens, Content);
            }

            return true;
        }
        public bool CreateDummyBook()
        {
            // Set default values for the book properties
            Title = "Dummy Book";
            Author = "John Doe";
            IsNaughty = false;
            Price = 10;
            IsUnique = false;
            WhenVarSet = "";
            Content.Clear().Append("This is a dummy book content.\nIt contains multiple lines.\nEnjoy reading!");

            // Return true to indicate successful creation of the dummy book
            return true;
        }
        /// <summary>
        /// Opens a localized book file from StreamingAssets/Text/Books.
        /// As classic books are also .txt files (despite not being true plain text) the localized files append "-LOC" to book filename.
        /// This is to ensure load process can seek correct version of book later when loading localized book files from mods.
        /// </summary>
        /// <param name="filename">Filename of classic or localized book file, e.g. "BOK00042-LOC.txt" or "BOK00042.txt". -LOC is added to filename automatically if missing.</param>
        /// <returns>True if successfull.</returns>

        public bool OpenLocalizedBookFile(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return false;

            string fileNoExt = Path.GetFileNameWithoutExtension(filename);
            if (!fileNoExt.EndsWith(localizedFilenameSuffix))
                filename = fileNoExt + localizedFilenameSuffix + fileExtension;

            string[] lines = null;

            if (ModManager.Instance != null && ModManager.Instance.TryGetAsset(filename, false, out TextAsset textAsset))
            {
                if (!string.IsNullOrWhiteSpace(textAsset.text))
                {
                    lines = textAsset.text.Split('\n');
                }
            }

            if (lines == null)
            {
                string path = Path.Combine(Paths.StreamingAssetsPath, textFolderName, booksFolderName, filename);
                if (!File.Exists(path))
                    return false;

                lines = File.ReadAllLines(path);
                if (lines == null || lines.Length == 0)
                    return false;   
            }

            bool readingContent = false;
            Content.Clear();
            for (int l = 0; l < lines.Length; l++)
            {
                string line = lines[l].Trim();
                if (!readingContent)
                {
                    if (line.StartsWith(titleKeyword, System.StringComparison.InvariantCultureIgnoreCase))
                        Title = line.Substring(titleKeyword.Length).Trim();
                    else if (line.StartsWith(authorKeyword, System.StringComparison.InvariantCultureIgnoreCase))
                        Author = line.Substring(authorKeyword.Length).Trim();
                    else if (line.StartsWith(isNaughtyKeyword, System.StringComparison.InvariantCultureIgnoreCase))
                        bool.TryParse(line.Substring(isNaughtyKeyword.Length).Trim(), out IsNaughty);
                    else if (line.StartsWith(priceKeyword, System.StringComparison.InvariantCultureIgnoreCase))
                        int.TryParse(line.Substring(priceKeyword.Length).Trim(), out Price);
                    else if (line.StartsWith(isUniqueKeyword, System.StringComparison.InvariantCultureIgnoreCase))
                        bool.TryParse(line.Substring(isUniqueKeyword.Length).Trim(), out IsUnique);
                    else if (line.StartsWith(whenVarSetKeyword, System.StringComparison.InvariantCultureIgnoreCase))
                        WhenVarSet = line.Substring(whenVarSetKeyword.Length).Trim();
                    else if (line.StartsWith(contentKeyword, System.StringComparison.InvariantCultureIgnoreCase))
                        readingContent = true;
                }
                else
                {
                    Content.AppendLine(lines[l]);
                }
            }

            return true;
        }

        /// <summary>
        /// Saves localized book file.
        /// </summary>
        /// <param name="filename">Filename of localized book file.</param>
        /// <returns>True if successfull.</returns>
        public void SaveLocalizedBook(string filename)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} {1}\n", titleKeyword, Title)
                   .AppendFormat("{0} {1}\n", authorKeyword, Author)
                   .AppendFormat("{0} {1}\n", isNaughtyKeyword, IsNaughty)
                   .AppendFormat("{0} {1}\n", priceKeyword, Price)
                   .AppendFormat("{0} {1}\n", isUniqueKeyword, IsUnique)
                   .AppendFormat("{0} {1}\n", whenVarSetKeyword, WhenVarSet)
                   .AppendFormat("{0}\n{1}", contentKeyword, Content);
            File.WriteAllText(filename, builder.ToString());
        }

        /// <summary>
        /// Convert book tokens to markup.
        /// This is a simplified version of RSC markup converter.
        /// </summary>
        /// <param name="tokens">Input tokens.</param>
        /// <returns>String result.</returns>
        void ConvertTokensToString(TextFile.Token[] tokens, StringBuilder builder)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                switch (tokens[i].formatting)
                {
                    case TextFile.Formatting.Text:
                        builder.Append(tokens[i].text);
                        break;
                    case TextFile.Formatting.NewLine:
                        builder.Append('\n');
                        break;
                    case TextFile.Formatting.JustifyLeft:
                        builder.Append(markupJustifyLeft);
                        break;
                    case TextFile.Formatting.JustifyCenter:
                        builder.Append(markupJustifyCenter);
                        break;
                    case TextFile.Formatting.FontPrefix:
                        builder.AppendFormat(markupFont, tokens[i].x);
                        break;
                    case TextFile.Formatting.PositionPrefix:
                    case TextFile.Formatting.SameLineOffset:
                        // Unused
                        break;
                    default:
                        Debug.LogFormat("Unknown book formatting token '{0}'", tokens[i].formatting);
                        break;
                }
            }
        }
    }
}