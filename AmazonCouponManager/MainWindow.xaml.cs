using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using UnManaged;

namespace AmazonCouponManager
{
	/// <summary>
	/// Logique d'interaction pour MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		HotKey _hotKey;
		private XDocument _theXML;
		public XDocument xml
		{
			get => _theXML;
			set => _theXML = value;
		}

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;
			_hotKey = new HotKey(Key.F9, KeyModifier.Shift | KeyModifier.Win, OnHotKeyHandler);
			xml = XDocument.Load(@"XML\coupons.xml");
			tvXml.DataContext = xml;
			tvXml.UpdateLayout();
		}
		private void OnHotKeyHandler(HotKey hotKey)
		{
			txtUrl.Text = GetActiveTabUrl();
		}

		public void GetChromeActiveUrl()
		{
			// there are always multiple chrome processes, so we have to loop through all of them to find the
			// process with a Window Handle and an automation element of name "Address and search bar"
			Process[] procsChrome = Process.GetProcessesByName("chrome");
			foreach (Process chrome in procsChrome)
			{
				// the chrome process must have a window
				if (chrome.MainWindowHandle == IntPtr.Zero)
				{
					continue;
				}

				// find the automation element
				AutomationElement elm = AutomationElement.FromHandle(chrome.MainWindowHandle);

				// manually walk through the tree, searching using TreeScope.Descendants is too slow (even if it's more reliable)
				AutomationElement elmUrlBar = null;
				try
				{
					// walking path found using inspect.exe (Windows SDK) for Chrome 31.0.1650.63 m (currently the latest stable)
					var elm1 = elm.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "Google Chrome"));
					if (elm1 == null) { continue; } // not the right chrome.exe
													// here, you can optionally check if Incognito is enabled:
													//bool bIncognito = TreeWalker.RawViewWalker.GetFirstChild(TreeWalker.RawViewWalker.GetFirstChild(elm1)) != null;
					var elm2 = TreeWalker.RawViewWalker.GetLastChild(elm1); // I don't know a Condition for this for finding :(
					var elm3 = elm2.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, ""));
					var elm4 = elm3.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ToolBar));
					elmUrlBar = elm4.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Custom));
				}
				catch
				{
					// Chrome has probably changed something, and above walking needs to be modified. :(
					// put an assertion here or something to make sure you don't miss it
					continue;
				}

				// make sure it's valid
				if (elmUrlBar == null)
				{
					// it's not..
					continue;
				}

				// elmUrlBar is now the URL bar element. we have to make sure that it's out of keyboard focus if we want to get a valid URL
				if ((bool)elmUrlBar.GetCurrentPropertyValue(AutomationElement.HasKeyboardFocusProperty))
				{
					continue;
				}

				// there might not be a valid pattern to use, so we have to make sure we have one
				AutomationPattern[] patterns = elmUrlBar.GetSupportedPatterns();
				if (patterns.Length == 1)
				{
					string ret = "";
					try
					{
						ret = ((ValuePattern)elmUrlBar.GetCurrentPattern(patterns[0])).Current.Value;
					}
					catch { }
					if (ret != "")
					{
						// must match a domain name (and possibly "https://" in front)
						if (Regex.IsMatch(ret, @"^(https:\/\/)?[a-zA-Z0-9\-\.]+(\.[a-zA-Z]{2,4}).*$"))
						{
							// prepend http:// to the url, because Chrome hides it if it's not SSL
							if (!ret.StartsWith("http"))
							{
								ret = "http://" + ret;
							}
							Console.WriteLine("Open Chrome URL found: '" + ret + "'");
						}
					}
					continue;
				}
			}
		}

		public string GetActiveTabUrl()
		{
			Process[] procsChrome = Process.GetProcessesByName("chrome");

			if (procsChrome.Length <= 0)
				return null;

			foreach (Process proc in procsChrome)
			{
				// the chrome process must have a window 
				if (proc.MainWindowHandle == IntPtr.Zero)
					continue;

				// to find the tabs we first need to locate something reliable - the 'New Tab' button 
				AutomationElement root = AutomationElement.FromHandle(proc.MainWindowHandle);
				var list = root.FindAll(TreeScope.Descendants, new NotCondition(new PropertyCondition( AutomationElement.NameProperty, "")));
				foreach (AutomationElement item in list)
				{
					var s = item.Current.Name;
				}
				var tabName = root.FindFirst(TreeScope.Descendants, new NotCondition(new PropertyCondition( AutomationElement.NameProperty, "")));
				if (tabName != null)
					//txtName.Text = (string)tabName.GetCurrentPropertyValue(AutomationElement.NameProperty);
					txtName.Text = tabName.Current.Name;
				var SearchBar = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Barre d'adresse et de recherche"));
				if (SearchBar != null)
					return (string)SearchBar.GetCurrentPropertyValue(ValuePatternIdentifiers.ValueProperty);
			}

			return null;
		}

		private void btnGetUrl_Click(object sender, RoutedEventArgs e)
		{
			txtUrl.Text = GetActiveTabUrl();
		}

		private void btnAdd_Click(object sender, RoutedEventArgs e)
		{
			if (xml.Descendants(txtName.Text).FirstOrDefault() == null)
				xml.Root.Add(new XElement(txtName.Text, new XAttribute("url", txtUrl.Text)));
			xml.Descendants(txtName.Text).First().Add(new XElement("coupon", new XAttribute("date_fin", datePicker.Text), new XAttribute("value", txtVal.Text)));
			xml.Save("XML\\coupons.xml");
			tvXml.UpdateLayout();
		}
	}
}
