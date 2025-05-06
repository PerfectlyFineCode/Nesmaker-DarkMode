using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NesMakerPluginBase;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace NesmakerDarkmode
{
	[ExportMetadata("ID", "DarkMode")]
	[Export(typeof(INesMakerPlugin))]
	public class MyPlugin : INesMakerPlugin
	{
		private readonly HashSet<Form> _formsSet = [];

		[Import]
		public INesMaker NesMaker { get; set; }
		
		public static readonly bool IsDebugMode = Environment.GetEnvironmentVariable("DEV") == "1";

		/// <inheritdoc />
		public void Init(INesMaker app)
		{
			NesMaker = app;

			if (IsDebugMode)
			{
				// Allocate console using the utility class
				ConsoleUtility.InitializeConsole();
			}

			// Delay applying dark mode until the application is idle
			Application.Idle += OnApplicationIdle;
		}

		/// <inheritdoc />
		public int GetNodeCount()
		{
			return 1;
		}

		/// <inheritdoc />
		public string GetNodeName(int offset)
		{
			return "Dark Mode";
		}

		public Image GetNodeIcon(int offset)
		{
			return null;
		}

		public UserControl GetControl(int offset)
		{
			return new UserControl();
		}

		/// <inheritdoc />
		public ICollection<string> GetExportFilenames()
		{
			return new List<string>();
		}

		/// <inheritdoc />
		public string Export(string filename)
		{
			return "";
		}

		/// <inheritdoc />
		public void Persist(BinaryWriter bw)
		{
		}

		/// <inheritdoc />
		public void Recall(BinaryReader br)
		{
		}

		private void OnApplicationIdle(object sender, EventArgs e)
		{
			if (IsDebugMode)
			{
				Console.WriteLine("OnApplicationIdle called");
			}
			// Iterate through all open forms
			foreach (Form form in Application.OpenForms)
			{
				// Check if handle exists, is created, and not already subclassed
				if (form is not { IsHandleCreated: true } || _formsSet.Contains(form)) continue;
				// IntPtr handle = form.Handle;

				form.Invalidated -= OnFormOnInvalidated; // Unsubscribe to avoid multiple subscriptions;
				form.Invalidated += OnFormOnInvalidated;
					
				EnableMica(form);
				FindTreeView(form);
				ApplyDarkMode(form);
					
				_formsSet.Add(form); // Mark as subclassed
				continue;

				// Apply visual effects
				void OnFormOnInvalidated(object s, InvalidateEventArgs args) => ApplyDarkMode(form);
			}
		}

		private static void EnableMica(Form form)
		{
			IntPtr hwnd = form.Handle;
			
			if (hwnd == IntPtr.Zero && IsDebugMode)
			{
				Console.WriteLine("Form handle is not valid.");
				return;
			}

			// Set the window attribute to enable Mica
			var dwmwaSystembackdropType = 38;
			var dwmsbtMainwindow = 2;              // Mica Alt
			var dwmwaUseImmersiveDarkMode = 20; // For dark mode non-client area

			DwmSetWindowAttribute(hwnd, dwmwaSystembackdropType, ref dwmsbtMainwindow, sizeof(int));

			// Set dark mode for the non-client area
			var useDarkMode = 1; // TRUE
			DwmSetWindowAttribute(hwnd, dwmwaUseImmersiveDarkMode, ref useDarkMode, sizeof(int));

			// Extend the frame into the client area
			Margins margins = new Margins { CxLeftWidth = -1 };
			DwmExtendFrameIntoClientArea(hwnd, ref margins);
		}

		[DllImport("dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

		[DllImport("dwmapi.dll")]
		private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

		private static void FindTreeView(Control control)
		{
			control.ForEach(static x =>
			{
				if (x is TreeView treeView)
				{
					ApplyTreeViewTheme(treeView);
				}
				else
				{
					ApplyDarkMode(x);
				}
			});
		}

		private static void ApplyTreeViewTheme(TreeView treeView)
		{
			treeView.BackColor = Color.Black;

			treeView.ForeColor = Color.White;
			if (IsDebugMode)
			{
				Console.WriteLine($"TreeView: [{treeView.Name}] {treeView.GetType().Name}");
			}

			treeView.AfterSelect -= OnTreeViewOnAfterSelect; // Unsubscribe to avoid multiple subscriptions
			treeView.AfterSelect += OnTreeViewOnAfterSelect;
			return;

			// Set dark mode colors for TreeView
			void OnTreeViewOnAfterSelect(object s, TreeViewEventArgs e)
			{
				// Get form handle
				Form parentForm = treeView.FindForm();
				if (parentForm != null)
				{
					ApplyDarkMode(parentForm);
				}
					
				// treeView.BackColor = Color.FromArgb(25, 25, 25);
				treeView.BackColor = Color.Black;
				treeView.ForeColor = Color.White;
				if (IsDebugMode)
				{
					Console.WriteLine($"TreeView AfterSelect: [{treeView.Name}] {treeView.GetType().Name}");
				}
			}
		}
		
		private static void ApplyDarkMode(Control control)
		{
			// Recursively apply to child controls
			control.ForEach(ApplyTheme);
		}

		private static void ApplyTheme(Control control)
		{
			switch (control)
			{
				// Set dark mode colors based on control type if needed
				case TextBox _:
					// Specific color for TextBox
					control.BackColor = Color.Black;
					control.ForeColor = Color.Gray;
					break;
				default:
					// Default dark mode for other controls
					control.BackColor = Color.Black;
					break;
			}

			control.ForeColor = Color.White;

			if (IsDebugMode)
			{
				Console.WriteLine($"Control: [{control.Name}] {control.GetType().Name}, BackColor: {control.BackColor}, ForeColor: {control.ForeColor}");
			}
		}

		private struct Margins
		{
			public int CxLeftWidth;
			public int CxRightWidth;
			public int CyTopHeight;
			public int CyBottomHeight;
		}
	}
}