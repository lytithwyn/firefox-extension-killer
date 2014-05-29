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
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace firefox_extension_killer
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		Panel scrollPanel = new Panel();
		GroupBox checksGroup = new GroupBox();
		Button removeButton = new Button();
		FirefoxExtensionController ffExtController = new FirefoxExtensionController();
		List<CheckBox> extCheckBoxes = new List<CheckBox>();

        public MainForm ()
		{
			this.SuspendLayout ();

			// Initialize your components here
			this.MinimumSize = new Size(200, 200);

			this.checksGroup.Text = "Extensions Found: ";
			this.checksGroup.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			this.Controls.Add (checksGroup);

			this.scrollPanel.AutoScroll = true;
			this.scrollPanel.Dock = DockStyle.Fill;
			this.checksGroup.Controls.Add (this.scrollPanel);

			this.removeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			this.removeButton.Text = "&Remove";
			this.removeButton.Click += this.RemoveExtensions;
			this.Controls.Add (removeButton);

            this.ResumeLayout();
            this.Name = "MainForm Name.";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Firefox Extension Killer";

			// set up event listeners
			this.Load += new EventHandler(this.onLoad);
        }

		private void onLoad(object Sender, EventArgs Args) {
			this.checksGroup.Width = this.ClientRectangle.Width;
			this.checksGroup.Height = this.ClientRectangle.Height - this.removeButton.Height - 10;
			this.removeButton.Top = this.checksGroup.Height + 5;
			this.removeButton.Left = this.ClientRectangle.Width - this.removeButton.Width - 5;
			
			LoadExtensionsToGUI();
		}
        
        private void LoadExtensionsToGUI() {
			foreach(string extName in this.ffExtController.extensionList.Keys) {
				CheckBox thisCheckBox = new CheckBox();
				thisCheckBox.Top = (thisCheckBox.Height) * this.extCheckBoxes.Count + 5;
				thisCheckBox.Text = extName;
				thisCheckBox.Left += 10;
				thisCheckBox.AutoSize = true;
				this.scrollPanel.Controls.Add (thisCheckBox);
				this.extCheckBoxes.Add(thisCheckBox);
			}
        }
        
        private void RemoveExtensions(object Sender, EventArgs Args) {
        	foreach(CheckBox thisCheckBox in this.extCheckBoxes) {
        		if(thisCheckBox.Checked) {
        			string name = thisCheckBox.Text;
        			bool success = ffExtController.DeleteExtension(name);
        			if(!success) {
        				string message = string.Format("Failed to delete extension: {0}", name);
        				MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        			}
        		}
        	}
        	
        	ReloadExtensions();
        }
        
        private void ReloadExtensions() {
        	int extCount = this.extCheckBoxes.Count;
        	int lastExt = extCount - 1;
        	for(int i = lastExt; i >= 0; i--) {
        		CheckBox thisCheckBox = this.extCheckBoxes[i];
        		int controlIndex = this.scrollPanel.Controls.IndexOf(thisCheckBox);
        		this.scrollPanel.Controls.RemoveAt(controlIndex);
        		extCheckBoxes.RemoveAt(i);
        		
        	}
        	
        	LoadExtensionsToGUI();
        }
	}
}
