/*
 * Copyright (c) 2014, Matthew Morgan
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer. 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 * The views and conclusions contained in the software and documentation are those
 * of the authors and should not be interpreted as representing official policies, 
 * either expressed or implied, of the FreeBSD Project.
 */
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Xml;
using Ionic.Zip;

namespace firefox_extension_killer
{
	/// <summary>
	/// Handles the searching for and deleting of firefox extensions
	/// </summary>
	public class FirefoxExtensionController
	{
		public System.Collections.Generic.Dictionary<string,FirefoxExtension> extensionList;
		private string userFirefoxProfileRoot = "";
		
		public FirefoxExtensionController()
		{
			extensionList = new System.Collections.Generic.Dictionary<string, FirefoxExtension>();
			LoadExtensions();
		}
		
		public bool DeleteExtension(string name) {
			bool success = false;
			
			if(!this.extensionList.ContainsKey(name)) {
				throw(new Exception("Nonexistent extension specified"));
			}
			
			FirefoxExtension extension = this.extensionList[name];
			
			switch(extension.type) {
				case FirefoxExtension.ExtensionType.EXT_DIR:
					success = DeleteDirExtension(extension);
					break;
				case FirefoxExtension.ExtensionType.EXT_REG:
					success = DeleteRegExtension(extension);
					break;
				case FirefoxExtension.ExtensionType.EXT_XPI:
					success = DeleteXPIExtension(extension);
					break;
				default:
					throw(new Exception("Invalid extension type"));				      
			}
			
			if(success) {
				this.extensionList.Remove(extension.name);
			}
			return success;
		}
		
		private bool DeleteDirExtension(FirefoxExtension extension) {
			try {
				MakeDirectoryContentsRW(extension.path);
				Directory.Delete(extension.path, true);
			} catch(Exception e) {
				return false;
			}
			
			return true;
		}
		
		private bool DeleteRegExtension(FirefoxExtension extension) {
			string[] regPathElements = ExplodeRegValuePath(extension.path);
			string hiveName = regPathElements[0];
			string intermedPath = regPathElements[1];
			string valueName = regPathElements[2];
			
			RegistryKey hive;
			if(hiveName == "HKEY_LOCAL_MACHINE") {
				hive = Registry.LocalMachine;
			} else if(hiveName == "HKEY_CURRENT_USER") {
				hive = Registry.CurrentUser;
			} else {
				throw(new Exception("Invalid registry path"));
			}
			
			try {
				RegistryKey extensionKey = hive.OpenSubKey(intermedPath, true);
				extensionKey.DeleteValue(valueName);
			} catch(Exception e) {
				return false;
			}
			
			return true;
		}
		
		private bool DeleteXPIExtension(FirefoxExtension extension) {
			try {
				File.Delete(extension.path);
			} catch(Exception e) {
				return false;
			}
			
			return true;
		}
		
		private void MakeDirectoryContentsRW(string path) {
			string[] subDirs = Directory.GetDirectories(path);
			foreach(string subDir in subDirs) {
				FileInfo thisFileInfo = new FileInfo(subDir);
				thisFileInfo.IsReadOnly = false;
				thisFileInfo.Refresh();
				MakeDirectoryContentsRW(subDir);
			}
			
			string[] childFiles = Directory.GetFiles(path);
			foreach(string childFile in childFiles) {
				FileInfo thisFileInfo = new FileInfo(childFile);
				thisFileInfo.IsReadOnly = false;
				thisFileInfo.Refresh();
			}
		}
		
		private string GetUserFirefoxProfileRoot() {
			if(this.userFirefoxProfileRoot == "") {
				string appDataPath = System.Environment.GetEnvironmentVariable("APPDATA");
				this.userFirefoxProfileRoot = appDataPath + @"\mozilla";
			}
			
			return this.userFirefoxProfileRoot;
		}
		
		private void LoadExtensions() {
			LoadCurrentUserGlobalDirExtensions();
  			LoadCurrentUserProfileDirExtensions();
			LoadMachineDirExtensions();
			LoadMachineRegExtensions();
			LoadCurrentUserRegExtensions();
			LoadMachineWOWRegExtensions();
		}
		
		private void LoadCurrentUserGlobalDirExtensions() {
			string firefoxProfileGlobalExtensionDir = GetUserFirefoxProfileRoot() + @"\extensions";
			LoadExtensionsFromPath(firefoxProfileGlobalExtensionDir);
		}
		
		private void LoadCurrentUserProfileDirExtensions() {
			string[] firefoxUserProfileDirs = GetUserProfileDirs();
			foreach(string profileDirPath in firefoxUserProfileDirs) {
				string profileDirExtensionPath = profileDirPath + @"\extensions";
				LoadExtensionsFromPath(profileDirExtensionPath);
			}
		}
		
		private void LoadMachineDirExtensions() {
			string programFilesPath = System.Environment.GetEnvironmentVariable("PROGRAMFILES(X86)");

			if(programFilesPath == null) {
				programFilesPath = System.Environment.GetEnvironmentVariable("PROGRAMFILES");
			}
			
			string programFilesExtensionPath = programFilesPath + @"\Mozilla Firefox\browser\extensions";
			LoadExtensionsFromPath(programFilesExtensionPath);
		}
		
		private void LoadExtensionsFromPath(string path) {
			LoadDirTypeExtensionsFromPath(path);
			LoadXPITypeExtensionsFromPath(path);
		}
		
		private void LoadDirTypeExtensionsFromPath(string path) {
			string[] subDirsInPath = GetSubDirs(path);
			foreach(string subDir in subDirsInPath) {
				string name = GetNameFromDirExtension(subDir);
				name = MakeExtNameUnique(name);
				this.extensionList.Add(name, new FirefoxExtension (
					name,
					subDir,
					FirefoxExtension.ExtensionType.EXT_DIR
				));
			}
		}
		
		private void LoadXPITypeExtensionsFromPath(string path) {
			string[] xpiFilesInPath = GetXPIFiles(path);
			foreach(string xpiFile in xpiFilesInPath) {
				string name = GetNameFromXPIExtension(xpiFile);
				name = MakeExtNameUnique(name);
				this.extensionList.Add(name, new FirefoxExtension (
					name,
					xpiFile,
					FirefoxExtension.ExtensionType.EXT_XPI
				));
			}
		}
		
		private void LoadMachineRegExtensions() {
			RegistryKey lmExtensionsKey = Registry.LocalMachine.OpenSubKey(@"Software\mozilla\firefox\extensions");
			if(lmExtensionsKey == null) {
				return;
			}

			LoadExtensionsFromRegKey(lmExtensionsKey);
		}
		
		private void LoadMachineWOWRegExtensions() {
			RegistryKey lmWOWExtensionsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Mozilla\Firefox\Extensions");
			if(lmWOWExtensionsKey == null) {
				return;
			}
			
			LoadExtensionsFromRegKey(lmWOWExtensionsKey);
		}
		
		private void LoadCurrentUserRegExtensions() {
			RegistryKey cuExtensionsKey = Registry.CurrentUser.OpenSubKey(@"Software\mozilla\firefox\extensions");
			if(cuExtensionsKey == null) {
				return;
			}

			LoadExtensionsFromRegKey(cuExtensionsKey);
		}
		
		private void LoadExtensionsFromRegKey(RegistryKey regKey) {
			string[] valueNames = regKey.GetValueNames();
			foreach(string valueName in valueNames) {
				string name = MakeExtNameUnique(valueName);
				this.extensionList.Add(name, new FirefoxExtension (
					name,
					regKey.Name + @"\" + valueName,
					FirefoxExtension.ExtensionType.EXT_REG
				));
			}
		}
		
		private string GetNameFromDirExtension(string path) {
			string name = Path.GetFileName(path);
			string[] installRDFS = Directory.GetFiles(path, "install.rdf");
			
			if(installRDFS.Length == 1) {
				string installRDFName = GetNameFromInstallRDF(installRDFS[0]);
				if(installRDFName != null) {
					name = installRDFName;
				}
			}
			
			return name;
		}
		
		private string GetNameFromXPIExtension(string path) {
			string name = Path.GetFileNameWithoutExtension(path);
			string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempDirectory);
			
			using (ZipFile xpiFile = ZipFile.Read(path)) {
				if(xpiFile.ContainsEntry("install.rdf")) {
					ZipEntry installRDFEntry = xpiFile["install.rdf"];
					installRDFEntry.Extract(tempDirectory, ExtractExistingFileAction.OverwriteSilently);
					string installRDFName = GetNameFromInstallRDF(Path.Combine(tempDirectory, "install.rdf"));
					if(installRDFName != null) {
						name = installRDFName;
					}
					MakeDirectoryContentsRW(tempDirectory);
					Directory.Delete(tempDirectory, true);
				}
			}
			
			return name;
		}
		
		private string GetNameFromInstallRDF(string installRDFPath) {
			string name = null;
			string installRDFText = File.ReadAllText(installRDFPath);
			int nameOpenTagIndex = installRDFText.IndexOf("<em:name>");
			int nameCloseTagIndex = installRDFText.IndexOf("</em:name>");
			if(nameOpenTagIndex >=0 && nameCloseTagIndex >=0) {
				name = installRDFText.Substring(nameOpenTagIndex+9, nameCloseTagIndex - (nameOpenTagIndex + 9));
			}
			
			return name;
		}
		
		private string[] ExplodeRegValuePath(string path) {
			string[] hiveAndPath = path.Split(new char[]{'\\'}, 2);
			if(hiveAndPath.Length != 2) {
				throw(new Exception("Invalid registry value path specified!"));
			}
			string hive = hiveAndPath[0];
			
			int lastSlashIndex = hiveAndPath[1].LastIndexOf('\\');
			string intermedPath = hiveAndPath[1].Substring(0, lastSlashIndex);
			string valueName = hiveAndPath[1].Substring(lastSlashIndex + 1);
			
			return new string[]{ hive, intermedPath, valueName };
		}
		
		private string[] GetSubDirs(string path) {
			if(Directory.Exists(path)) {
				string[] subDirsInPath = Directory.GetDirectories(path);
				return subDirsInPath;
			} else {
				return new string[0];
			}
		}
		
		private string[] GetXPIFiles(string path) {
			if(Directory.Exists(path)) {
				string[] xpiFilesInPath = Directory.GetFiles(path, "*.xpi");
				return xpiFilesInPath;
			} else {
				return new string[0];
			}
		}
		
		private string MakeExtNameUnique(string suggestedName) {
			while(this.extensionList.ContainsKey(suggestedName)) {
				suggestedName += "1";
			}
			
			return suggestedName;
		}
		
		private string[] GetUserProfileDirs() {
			string firefoxProfileContainerDir = GetUserFirefoxProfileRoot() + @"\Firefox\Profiles";
			return GetSubDirs(firefoxProfileContainerDir);
		}
	}
}
