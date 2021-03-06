﻿using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WitchOS
{
    [CreateAssetMenu(menuName = "WitchOS/Filesystem", fileName = "NewFilesystem.asset")]
    public class Filesystem : InitializableSO
    {
        public Directory RootDirectory => SaveData.Value;
        public string RootPath => GetPathOfFile(RootDirectory);

        public string PathSeparator;
        public List<FileSOBase> InitialFiles;

        public SaveManager SaveManager;
        public DirectorySaveData SaveData;

        Dictionary<FileBase, Directory> parentCache;

        public override void Initialize ()
        {
            SaveManager.Register(SaveData);

            if (RootDirectory.Data == null)
            {
                RootDirectory.Data = InitialFiles.Select(so => so.File).ToList();
            }

            buildParentCache();
        }

        public bool FileExistsInFilesystem (FileBase file)
        {
            return parentCache.ContainsKey(file);
        }

        public FileBase GetFileAtPath (string path)
        {
            validatePath(path);

            if (path == RootDirectory.Name || path == RootDirectory.Name + PathSeparator)
            {
                return RootDirectory;
            }

            Directory currentDirectory = RootDirectory;
            var pathParts = splitPath(path);

            // don't include the root or the final filename
            for (int i = 1; i < pathParts.Length - 1; i++)
            {
                currentDirectory = currentDirectory.Data
                    .SingleOrDefault(f => f.Name == pathParts[i])
                    as Directory;

                if (currentDirectory == null) return null;
            }

            var file = currentDirectory.Data.SingleOrDefault(f => f.Name == pathParts.Last());

            if (path.EndsWith(PathSeparator))
            {
                return file as Directory;
            }

            return file;
        }

        public Directory GetDirectoryAtPath (string path)
        {
            return GetFileAtPath(path) as Directory;
        }

        public bool FileExistsAtPath (string path)
        {
            return GetFileAtPath(path) != null;
        }

        public string GetPathOfFile (FileBase file)
        {
            if (!FileExistsInFilesystem(file))
            {
                throw new FileDoesNotExistException($"file {file.Name} does not exist in this filesystem");
            }

            var ancestorNames = traverseAncestors(file).Reverse().Select(f => f.Name);
            var pathSuffix = file is Directory ? PathSeparator : "";

            return string.Join(PathSeparator, ancestorNames) + pathSuffix;
        }

        public void RenameFile (string filePath, string name)
        {
            RenameFile(GetFileAtPath(filePath), name);
        }

        public void RenameFile (FileBase file, string name)
        {
            validateFileName(name);

            if (!FileExistsInFilesystem(file))
            {
                throw new FileDoesNotExistException("cannot rename file that does not exist");
            }

            if (file.Name == name) return;

            if (file != RootDirectory && parentCache[file].Data.Any(f => f != file && f.Name == name))
            {
                throw new PathAlreadyExistsException($"cannot rename {file.Name} to {name} because its parent directory ({parentCache[file].Name}{PathSeparator}) already contains a file with that name");
            }

            file.Name = name;
        }

        // deep parameter acts like the 'p' flag in mkdir. if it's set and you want to add path '/a/b/c' and directory 'a' or 'b' do not exist, they will be created
        public void AddFile (FileBase file, string parentDirectoryPath, bool deep = false)
        {
            validateFileToBeAdded(file);
            validatePath(parentDirectoryPath);

            Directory parent = GetDirectoryAtPath(parentDirectoryPath);

            if (parent == null)
            {
                if (deep)
                {
                    parent = convertDeepPathToDirectories(parentDirectoryPath);
                }
                else
                {
                    throw new PathDoesNotExistException($"one or more directories on the path {parentDirectoryPath} do not exist. either add those directories, or set the deep flag and try again");
                }
            }

            AddFile(file, parent);
        }

        public void AddFile (FileBase file, Directory parent)
        {
            throwIfWouldCreateCircularStructure(file, parent);
            validateFileToBeAdded(file);

            if (!FileExistsInFilesystem(parent))
            {
                throw new FileDoesNotExistException($"cannot add file {file.Name} to directory {parent.Name} because that directory does not exist in the filesystem");
            }

            if (parent.Data.Any(f => f.Name == file.Name))
            {
                throw new PathAlreadyExistsException($"cannot add file {file.Name} to directory {parent.Name} because it already contains a file with that name");
            }

            parent.Data.Add(file);
            trackFileAndAnyChildren(file, parent);
        }

        public void MoveFile (string fromPath, string parentDirectoryPath, bool deep = false)
        {
            var file = GetFileAtPath(fromPath);
            MoveFile(file, parentDirectoryPath, deep);
        }

        public void MoveFile (FileBase file, string parentDirectoryPath, bool deep = false)
        {
            if (GetFileAtPath(parentDirectoryPath) is Directory dir)
            {
                throwIfWouldCreateCircularStructure(file, dir);
            }

            RemoveFile(file);
            AddFile(file, parentDirectoryPath, deep);
        }

        public void MoveFile (string fromPath, Directory newParent)
        {
            var file = GetFileAtPath(fromPath);
            MoveFile(file, newParent);
        }

        public void MoveFile (FileBase file, Directory newParent)
        {
            throwIfWouldCreateCircularStructure(file, newParent);
            RemoveFile(file);
            AddFile(file, newParent);
        }

        // returns whether file existed
        public bool RemoveFile (string path)
        {
            var file = GetFileAtPath(path);

            if (file == null)
            {
                return false;
            }

            RemoveFile(file);

            return true;
        }

        // assumes file existed to begin with
        public void RemoveFile (FileBase file)
        {
            if (!FileExistsInFilesystem(file))
            {
                throw new FileDoesNotExistException($"file {file.Name} does not exist in this filesystem");
            }

            if (file == RootDirectory)
            {
                throw new AttemptedRootDirectoryDeletionException("cannot delete the root directory");
            }

            parentCache[file].Data.Remove(file);
            untrackFileAndAnyChildren(file);
        }

        void buildParentCache ()
        {
            parentCache = new Dictionary<FileBase, Directory>();
            trackFileAndAnyChildren(SaveData.Value, null);
        }

        void trackFileAndAnyChildren (FileBase file, Directory parent)
        {
            parentCache[file] = parent;

            if (file is Directory dir)
            {
                foreach (var child in dir.Data)
                {
                    trackFileAndAnyChildren(child, dir);
                }
            }
        }

        void untrackFileAndAnyChildren (FileBase file)
        {
            parentCache.Remove(file);

            if (file is Directory dir)
            {
                foreach (var child in dir.Data)
                {
                    untrackFileAndAnyChildren(child);
                }
            }
        }

        // assumes file is in filesystem
        IEnumerable<FileBase> traverseAncestors (FileBase file)
        {
            FileBase currentFile = file;

            while (currentFile != RootDirectory)
            {
                yield return currentFile;
                currentFile = parentCache[currentFile];
            }

            yield return currentFile; // this should always be the root
        }

        string[] splitPath (string path)
        {
            validatePath(path);

            // gah why can't we just call the thing directly
            string[] split = path.Split(new string[] { PathSeparator }, StringSplitOptions.None);
            
            // last element is empty if there's a trailing slash
            if (split.Last() == "")
            {
                split = split.Take(split.Length - 1).ToArray();
            }

            return split;
        }

        void validateFileToBeAdded (FileBase file)
        {
            if (FileExistsInFilesystem(file))
            {
                throw new FileAlreadyExistsException($"cannot add file {file.Name} because it already exists in this filesystem");
            }

            validateFileName(file.Name);
        }

        void validateFileName (string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidPathException("file names cannot be null or empty");
            }

            if (name.Contains(PathSeparator))
            {
                throw new InvalidPathException($"filename {name} is invalid because it contains the path separator ({PathSeparator})");
            }
        }

        void validatePath (string path)
        {
            if (path == null || (path == "" && RootDirectory.Name != ""))
            {
                throw new InvalidPathException("paths cannot be null or empty");
            }

            int pathSepLength = PathSeparator.Length;

            for (int i = 0; i < path.Length - pathSepLength * 2; i++)
            {
                if (path.Substring(i, pathSepLength) == PathSeparator && path.Substring(i + pathSepLength, pathSepLength) == PathSeparator)
                {
                    throw new InvalidPathException($"path {path} contains two or more adjacent path separators ({PathSeparator})");
                }
            }
        }

        void throwIfWouldCreateCircularStructure (FileBase file, Directory targetParent)
        {
            // wish I could use pattern matching here but Unity won't let me :(
            var directory = file as Directory;
            if (directory == null) return;

            if (directory == targetParent)
            {
                throw new CircularDirectoryStructureException($"cannot add directory {directory.Name} to itself");
            }

            if (traverseAncestors(targetParent).Contains(directory))
            {
                throw new CircularDirectoryStructureException($"cannot add {directory.Name} to directory {targetParent.Name} because {targetParent.Name} is a descendant of {directory.Name}");
            }
        }

        // takes a path consisting of valid directory names and converts it to a directory structure.
        // returns the final directory of that structure
        Directory convertDeepPathToDirectories (string pathToCreate)
        {
            string[] directoryNames = splitPath(pathToCreate);
            string[] fullDirectoryPaths = new string[directoryNames.Length];
            fullDirectoryPaths[0] = "";

            var parent = RootDirectory;

            for (int i = 1; i < directoryNames.Length; i++)
            {
                fullDirectoryPaths[i] = fullDirectoryPaths[i - 1] + PathSeparator + directoryNames[i];

                var currentFileThere = GetFileAtPath(fullDirectoryPaths[i]);

                if (currentFileThere is Directory currentDirectoryThere)
                {
                    parent = currentDirectoryThere;
                    continue;
                }
                else if (currentFileThere != null)
                {
                    throw new PathAlreadyExistsException($"unable to create path {pathToCreate} because a non-directory file already exists at {fullDirectoryPaths[i]}");
                }

                var newDirectory = new Directory(directoryNames[i]);
                AddFile(newDirectory, parent);

                parent = newDirectory;
            }

            return parent;
        }
    }
}
