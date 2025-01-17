﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonglistGenerator
{
    public class Songlist
    {
        private Logger logger;

        public IEnumerable<Chapter> Chapters { get; private set; }

        public Songlist(Logger logger)
        {
            this.logger = logger;
        }

        public void CreateListOfChapters(string[] folders)
        {
            var chapters = new List<Chapter>();
            foreach (var folder in folders)
            {
                if (!File.Exists(Path.Combine(folder, Program.ChapterMasterFile)))
                {
                    logger.WriteLine($"Folder {folder} does not cotain {Program.ChapterMasterFile}, ignoring");
                    continue;
                }

                var chapter = new Chapter(folder);
                chapters.Add(chapter);
            }

            this.Chapters = chapters.OrderBy(x => x.ChapterName);
            logger.WriteLine($"Found {chapters.Count} chapters.");
        }

        public void CreateListOfSongs()
        {
            foreach (var chapter in this.Chapters)
            {
                var latexFilesInsideChapter = Directory.GetFiles(chapter.FilePath, Program.LatexFileFilter);

                foreach (var latexFilePath in latexFilesInsideChapter)
                {
                    if (Path.GetFileName(latexFilePath) == Program.ChapterMasterFile)
                    {
                        // Ignore chapter master file
                        continue;
                    }

                    var song = new Song(latexFilePath);
                    chapter.Songs.Add(song);
                }

                logger.WriteLine($"Found {chapter.Songs.Count} songs in chapter {chapter.FilePath})");
            }
        }

        public void Initialize()
        {
            foreach (var chapter in this.Chapters)
            {
                chapter.Initialize();
                logger.WriteLine($"   Chapter \"{chapter.ChapterName}\" is located in folder \"{chapter.FolderName}\", UseArtists: {chapter.UseArtists}, contains {chapter.Songs.Count} songs");
                foreach (var song in chapter.Songs)
                {
                    song.Initialize();
                    logger.WriteLine($"      Song \"{song.Title}\", author \"{song.Author}\", artist \"{song.Artist}\"");
                }
            }
        }

        public string NewMainFile()
        {
            var listOfChapters = new List<string>();
            foreach (var chapter in this.Chapters)
            {
                var masterFile = chapter.FolderName != string.Empty ? $"{chapter.FolderName}/master" : "master";
                listOfChapters.Add($"\\include{{{masterFile}}}");
            }

            return string.Join(Environment.NewLine, listOfChapters);
        }

        public void CreateOutputFile(string songRepositoryFolder, string outputPath)
        {
            var fileCreator = new OutputFileCreator(songRepositoryFolder);
            fileCreator.ReplaceMainFile(this.NewMainFile());
            foreach (var chapter in this.Chapters)
            {
                fileCreator.ReplaceMasterFile(chapter.FolderName, chapter.NewMasterFile());
            }

            fileCreator.SaveZipArchive(outputPath);
        }

        public void ConsolidateChapters()
        {
            var newChaptersList = new List<Chapter>();
            newChaptersList.AddRange(this.Chapters.Where(x => x.Songs.Count > 1));
            var othersChapter = new Chapter(null)
            {
                ChapterName = "Pozostałe",
                UseArtists = true,
                FolderName = string.Empty,
            };
            var songsToOthersChapter = new List<Song>();
            var singleSongs = this.Chapters.Where(x => x.Songs.Count == 1).Select(x => x.Songs.Single()).OrderBy(x=>x.Title);
            othersChapter.Songs.AddRange(singleSongs);
            newChaptersList.Add(othersChapter);
            this.Chapters = newChaptersList;
        }

        public void WrapCarets()
        {
            var songFiles = new List<string>();
            var listsOfSongs = this.Chapters.Select(x => x.Songs);
            foreach (var list in listsOfSongs)
            {
                songFiles.AddRange(list.Select(x => x.FilePath));
            }

            foreach (var file in songFiles)
            {
                var content = File.ReadAllText(file);
                content = CaretsWrapper.WrapCarets(content);
                File.WriteAllText(file, content);
            }
        }
    }
}
