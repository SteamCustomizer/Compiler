using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

using SteamKeyValue;
using FreeImageAPI;

using Compiler.Schema;

namespace Compiler
{

    static class Core
    {
        private static string defaultSkinBaseName = "default";
        private static string defaultSkinFolderName = "skins";

        public static bool backupEnabled = true;
        public static bool debugMode = false;
        public static bool activateSkin = true;

        static public void Compile(FileInfo skinFileInfo, DirectoryInfo steamDirectoryInfo, DirectoryInfo baseDirectoryInfo = null)
        {
            if (!FreeImage.IsAvailable()) throw new Exception("FreeImage.dll not found");
            if (!skinFileInfo.Exists) throw new Exception("Definition file doesn't exist");
            if (!steamDirectoryInfo.Exists) throw new Exception("Steam directory doesn't exist");

            Dictionary<string, KeyValue> skinKeyValueList = new Dictionary<string, KeyValue>();

            JSchemaGenerator schemaGenerator = new JSchemaGenerator();

            JsonTextReader reader = new JsonTextReader(new StringReader(File.ReadAllText(skinFileInfo.FullName)));

            JSchemaValidatingReader validatingReader = new JSchemaValidatingReader(reader);
            validatingReader.Schema = schemaGenerator.Generate(typeof(SkinFile));
            SkinFile skinFile = new JsonSerializer().Deserialize<SkinFile>(validatingReader);

            string skinSourcePath;
            if (skinFile.metadata.template.skinBase == defaultSkinBaseName)
                skinSourcePath = steamDirectoryInfo.FullName + "/";
            else
            {
                string baseDirectory;
                if (baseDirectoryInfo != null)
                    baseDirectory = baseDirectoryInfo.FullName + "/" + skinFile.metadata.template.skinBase;
                else
                    baseDirectory = steamDirectoryInfo.FullName + "/" + defaultSkinFolderName + "/" + skinFile.metadata.template.skinBase;

                skinSourcePath = baseDirectory + "/";
            }

            /// TODO: Make copy of 3rd party skin and use it as a base

            if (!Directory.Exists(skinSourcePath)) throw new Exception("Skin source '" + skinSourcePath + "' directory doesn't exist");

            if (skinFile.files != null)
            {
                // iterate through files
                foreach (KeyValuePair<string, SkinFile.File> f in skinFile.files)
                {
                    string path = skinSourcePath + f.Key;
                    if (!File.Exists(path))
                    {
                        StreamWriter writer = File.CreateText(path);
                        writer.WriteLine('"' + f.Key + '"');
                        writer.WriteLine('{');
                        writer.WriteLine('}');
                        writer.Close();
                    }
                    KeyValue kv = KeyValue.LoadFromString(KeyValue.NormalizeFileContent(path));
                    if (f.Value.remove is JArray)
                    {
                        foreach (JToken node in f.Value.remove.Children())
                            RemoveNode(kv, node);
                    }
                    if (f.Value.add is JObject)
                    {
                        foreach (JProperty node in f.Value.add.Children())
                            kv.Children.Add(CreateNode(kv, node));
                    }
                    if (f.Value.change is JObject)
                    {
                        // recursively iterate through sections and change all found keys
                        ChangeNode(kv, new JProperty(f.Key, f.Value.change), false);
                    }
                    skinKeyValueList.Add(f.Key, kv);
                }
            }

            //if (skinFile.metadata.folderName == null) throw new Exception("Undefined skin folder name");
            string folderName = skinFile.metadata.template.name;
            if (skinFile.metadata.skin.name != null && skinFile.metadata.skin.author != null)
                folderName = skinFile.metadata.skin.name + " #" + skinFile.metadata.skin.id;

            string destinationPath = steamDirectoryInfo.FullName + "/" + defaultSkinFolderName + "/" + folderName;

            try
            {
                if (Directory.Exists(destinationPath))
                {
                    if (backupEnabled)
                    {
                        string backupDirectoryName = destinationPath + " - Backup (" + DateTime.Now.ToString("yyyyMMddHHmmss") + ")";
                        if (Directory.Exists(backupDirectoryName)) Directory.Delete(backupDirectoryName, true);
                        Directory.Move(destinationPath, backupDirectoryName);
                    }
                    else Directory.Delete(destinationPath, true);
                }

                Directory.CreateDirectory(destinationPath);

                /// NOTE: Copy base directory prior to writing modified files

                if (skinFile.metadata.template.skinBase != defaultSkinBaseName)
                    DirectoryCopy(skinSourcePath, destinationPath, true);

                foreach (KeyValuePair<string, KeyValue> kv in skinKeyValueList)
                {
                    if (Directory.CreateDirectory(destinationPath + "/" + Path.GetDirectoryName(kv.Key)).Exists)
                        kv.Value.SaveToFile(destinationPath + "/" + kv.Key, false);
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            if (skinFile.attachments != null)
            {
                foreach (SkinFile.Attachment attachment in skinFile.attachments)
                {
                    string type = (attachment.type != null) ? attachment.type : "image";

                    switch (type.ToLower())
                    {
                        case "image":
                            {
                                using (Base64Image image = new Base64Image(attachment.data))
                                {
                                    string graphicsDirPath = destinationPath + "/" + Path.GetDirectoryName(attachment.path);
                                    string extension = Path.GetExtension(attachment.path);
                                    if (extension.Length == 0)
                                        extension = "tga";
                                    else
                                        extension = extension.Substring(1);
                                    if (!Directory.Exists(graphicsDirPath)) Directory.CreateDirectory(graphicsDirPath);

                                    if (attachment.filters != null)
                                        image.ApplyFilters(attachment.filters);

                                    if (attachment.spritesheet == null)
                                    {
                                        if (attachment.transform != null)
                                            image.Transform(attachment.transform);
                                        image.Save(graphicsDirPath + "/" + Path.GetFileNameWithoutExtension(attachment.path) + "." + extension);
                                    }
                                    else // has defined spritesheet
                                    {
                                        string spritePath, finalPath = null;
                                        foreach (KeyValuePair<int, int[]> spriteDefinition in attachment.spritesheet)
                                        {
                                            if (spriteDefinition.Value.Length == 4)
                                            {
                                                if (attachment.spritesheetFiles.TryGetValue(spriteDefinition.Key, out spritePath))
                                                {
                                                    string dir = destinationPath + "/" + Path.GetDirectoryName(spritePath);
                                                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                                                    finalPath = dir + "/" + Path.GetFileNameWithoutExtension(spritePath) + "." + extension;
                                                }
                                                else
                                                    finalPath = graphicsDirPath + "/" + Path.GetFileNameWithoutExtension(attachment.path) + spriteDefinition.Key + "." + extension;

                                                if (finalPath != null)
                                                    image.SaveSprite(finalPath, spriteDefinition.Value);
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                    }
                }
            }

            // write metadata
            //string iniTemplateSection = "Template",
            //       iniSkinSection = "Skin";
            Ini.IniFile metadataIni = new Ini.IniFile(destinationPath + "/metadata.ini");
            foreach (PropertyInfo metadata in skinFile.metadata.GetType().GetProperties())
            {
                char[] arr = metadata.Name.ToCharArray();
                arr[0] = char.ToUpperInvariant(arr[0]);
                string sectionName = new string(arr);

                PropertyInfo sectionInfo = skinFile.metadata.GetType().GetProperty(metadata.Name);
                if (sectionInfo != null)
                {
                    object section = sectionInfo.GetValue(skinFile.metadata, null);
                    foreach (PropertyInfo property in section.GetType().GetProperties())
                    {
                        arr = property.Name.ToCharArray();
                        arr[0] = char.ToUpperInvariant(arr[0]);
                        string propertyName = new string(arr);

                        object val = property.GetValue(section, null);
                        string propertyValue = (val == null) ? "" : property.GetValue(section, null).ToString();
                        switch (property.Name)
                        {
                            case "revision":
                                {
                                    if (Convert.ToInt32(propertyValue) > 0)
                                        metadataIni.IniWriteValue(sectionName, propertyName, propertyValue);
                                    break;
                                }
                            case "primaryColor":
                            case "primaryTextColor":
                            case "accentColor":
                            case "accentTextColor":
                                {
                                    if (propertyValue.Length > 0)
                                        metadataIni.IniWriteValue(sectionName, propertyName, "0x" + propertyValue);
                                    break;
                                }
                            case "thumbnail":
                                {
                                    try
                                    {
                                        string fileName = "thumb.jpg";
                                        using (Base64Image image = new Base64Image(propertyValue))
                                        {
                                            if (image.Save(destinationPath + "/" + fileName, FREE_IMAGE_FORMAT.FIF_JPEG, FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYSUPERB))
                                                metadataIni.IniWriteValue(sectionName, propertyName, fileName);
                                        }
                                    }
                                    catch { }
                                    break;
                                }
                            default:
                                {
                                    if (propertyValue.Length > 0)
                                        metadataIni.IniWriteValue(sectionName, propertyName, propertyValue);
                                    break;
                                }
                        }
                    }
                }
                //Console.WriteLine(skinFile.metadata.GetType().GetProperty(metadata.Name).Name);
                /*
                foreach (PropertyInfo field in skinFile.metadata.GetType().GetField(metadata.Name).GetType().GetProperties())
                {
                    Console.WriteLine(field);
                }
                */
            }
            /*
            metadataIni.IniWriteValue(iniTemplateSection, "Version", skinFile.metadata.template);
            metadataIni.IniWriteValue(iniTemplateSection, "Name", skinFile.metadata.name);
            metadataIni.IniWriteValue(iniTemplateSection, "Author", skinFile.metadata.author);
            metadataIni.IniWriteValue(iniTemplateSection, "AuthorUrl", skinFile.metadata.authorUrl != null ? skinFile.metadata.authorUrl : "");
            metadataIni.IniWriteValue(iniTemplateSection, "SkinURL", skinFile.metadata.skinURL != null ? skinFile.metadata.skinURL : "");
            metadataIni.IniWriteValue(iniTemplateSection, "Description", skinFile.metadata.description != null ? skinFile.metadata.description : "");
            metadataIni.IniWriteValue(iniTemplateSection, "Color", skinFile.metadata.color != null ? skinFile.metadata.color : "0x1E1E1E");
            */
            // activate skin
            File.Delete(steamDirectoryInfo.FullName + "/" + defaultSkinFolderName + "/.active");
            if (activateSkin)
                File.WriteAllText(steamDirectoryInfo.FullName + "/" + defaultSkinFolderName + "/.active", folderName);

            // print debug
            if (debugMode)
            {
                string buffer = "";
                buffer += "Steam Customizer compiler debug log @ " + DateTime.Now.ToString() + "\r\n";
                buffer += "Schema list:\r\n";
                foreach (Type t in new Type[] { typeof(SkinFile) })
                {
                    buffer += "\r\n" + t.ToString() + ":\r\n";
                    buffer += schemaGenerator.Generate(t).ToString();
                    buffer += "\r\n";
                }
                File.WriteAllText("debug.log", buffer);
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private static KeyValue SearchNode(KeyValue containerNode, string node)
        {
            //Console.WriteLine(node);
            string keyName = node;
            int keyIndex = 0;
            int indexStart = node.IndexOf('[');
            int indexEnd = node.IndexOf(']');
            if (indexStart > -1 && (indexEnd - indexStart) > 1)
            {
                keyName = node.Substring(0, indexStart);
                keyIndex = int.Parse(node.Substring(indexStart + 1, indexEnd - indexStart - 1));
            }
            return containerNode[keyName, keyIndex];
        }

        private static void ChangeNode(KeyValue containerNode, JProperty node, bool setKV = true)
        {
            if (node.HasValues)
            {
                if (node.Value is JValue)
                {
                    KeyValue foundNode = SearchNode(containerNode, node.Name);
                    if (foundNode != null) foundNode.Value = node.Value.ToString();
                    // as steam skins are prone to structure error (especially after update) just silently ignore the error
                }
                else
                {
                    if (setKV) containerNode = containerNode[node.Name];
                    foreach (JProperty p in node.Value.Children())
                    {
                        ChangeNode(containerNode, p);
                    }
                }
            }
        }

        private static KeyValue CreateNode(KeyValue containerNode, JProperty node)
        {
            KeyValue container = containerNode.Children.Find((KeyValue kv) => kv.Name == node.Name);
            KeyValue newNode = container == null ? new KeyValue(node.Name) : container;
            if (node.HasValues)
            {
                if (node.Value is JValue)
                {
                    newNode.Value = node.Value.ToString();
                }
                else
                {
                    foreach (JToken token in node.Value.Children())
                    {
                        if (token is JProperty)
                        {
                            JProperty property = token as JProperty;
                            if (property.Value.Type == JTokenType.Array)
                            {
                                foreach (JToken item in property.Value.Children<JToken>())
                                {
                                    newNode.Children.Add(CreateNode(new KeyValue(property.Name), new JProperty(property.Name, item)));
                                }
                            }
                            else {
                                newNode.Children.Add(CreateNode(newNode, property));
                            }
                        }
                    }
                }
            }
            return newNode;
        }

        private static void RemoveNode(KeyValue containerNode, JToken token)
        {
            bool escaping = false;
            string buffer = "";
            List<string> nodeNameList = new List<string>();
            foreach (char c in token.ToString())
            {
                if (c == '\\' && !escaping)
                {
                    escaping = true;
                    continue;
                }
                if (c == '/' && !escaping)
                {
                    if (buffer.Length > 0)
                        nodeNameList.Add(buffer);
                    buffer = "";
                    continue;
                }
                escaping = false;
                buffer += c;
            }
            nodeNameList.Add(buffer);

            KeyValue currentNode = containerNode;
            int nodeCount = 0;

            foreach (string node in nodeNameList)
            {
                KeyValue foundNode = SearchNode(currentNode, node);
                if (foundNode == null)
                    break; // silent stop
                KeyValue matchNode = currentNode.Children.Find((KeyValue childNode) => childNode.Name == foundNode.Name);
                if (matchNode != null)
                {
                    if (nodeNameList.Count == ++nodeCount)
                    {
                        currentNode.Children.Remove(matchNode);
                        break;
                    }
                    currentNode = matchNode;
                }
            }
        }
    }
}
