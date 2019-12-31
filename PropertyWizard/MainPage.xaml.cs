using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PropertyWizard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>


    public sealed partial class MainPage : Page
    {
        ObservableCollection<PropertyModel> PropertyList = new ObservableCollection<PropertyModel>();
        public static readonly DependencyProperty SelectedPropertyProperty = DependencyProperty.Register("SelectedProperty", typeof(PropertyModel), typeof(MainPage), new PropertyMetadata(null));
        public static readonly DependencyProperty PropertiesAsTextProperty = DependencyProperty.Register("PropertiesAsText", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty AllPropertiesAsTextProperty = DependencyProperty.Register("AllPropertiesAsText", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty SetAllChoiceProperty = DependencyProperty.Register("SetAllChoice", typeof(string), typeof(MainPage), new PropertyMetadata("", SetAllChoiceChanged));
        public string SetAllChoice
        {
            get => (string)GetValue(SetAllChoiceProperty);
            set => SetValue(SetAllChoiceProperty, value);
        }
        private static void SetAllChoiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var depPropClass = d as MainPage;
            var depPropValue = (string)e.NewValue;
            depPropClass?.SetSetAllChoice(depPropValue);

        }
        private void SetSetAllChoice(string value)
        {
            Debug.WriteLine($"Selected Choice: {value}");
        }

        public string AllPropertiesAsText
        {
            get => (string)GetValue(AllPropertiesAsTextProperty);
            set => SetValue(AllPropertiesAsTextProperty, value);
        }
        public string PropertiesAsText
        {
            get => (string)GetValue(PropertiesAsTextProperty);
            set => SetValue(PropertiesAsTextProperty, value);
        }
        public PropertyModel SelectedProperty
        {
            get => (PropertyModel)GetValue(SelectedPropertyProperty);
            set => SetValue(SelectedPropertyProperty, value);
        }
        public MainPage()
        {
            this.InitializeComponent();


        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("TextBox_GotFocus");
            ((TextBox)sender).SelectAll();
            var lv = FindVisualParent<ListViewItem>(sender as TextBox);
            lv.IsSelected = true;
        }
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("TextBox_LostFocus");
            if (this.SelectedProperty == null)
            {
                Debug.WriteLine("SelectedProperty is null");
                return;
            }

            PropertiesAsText = GenerateProperty(this.SelectedProperty);
            GenerateAllProperties();
        }

        private void ListView_ItemClicked(object sender, ItemClickEventArgs e)
        {
            PropertyModel model = e.ClickedItem as PropertyModel;
            Debug.WriteLine("ListView_ItemClicked");
            GenerateProperty(model);
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("ListView_SelectionChanged");
            if (e?.AddedItems.Count > 0)
            {
                PropertyModel model = e.AddedItems[0] as PropertyModel;
                PropertiesAsText = GenerateProperty(model);
                GenerateAllProperties();
            }
        }

        private void GenerateAllProperties()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var prop in PropertyList)
            {
                sb.Append(GenerateProperty(prop));
            }

            AllPropertiesAsText = sb.ToString();
        }

        private string GenerateProperty(PropertyModel model)
        {
            var json = JsonConvert.SerializeObject(model, Formatting.Indented);
            Debug.WriteLine(json);


            StringBuilder sb;
            if (model.DependencyProperty)
            {
                sb = new StringBuilder(Templates.DependencProperty);
                sb.Replace("__CLASS__", model.ClassType);

            }
            else
            {
                sb = new StringBuilder(Templates.RegularProperty);
            }

            sb.Replace("__TYPE__", model.PropertyType);
            sb.Replace("__DEFAULT__", model.Default);
            sb.Replace("__PROPERTYNAME__", model.PropertyName);
            sb.Replace("__FIELDNAME__", model.FieldName);
            if (model.ChangeNotification)
            {
                if (model.DependencyProperty)
                {
                    string changeNotificationFunction = ", " + model.PropertyName + "Changed";
                    sb.Replace("__DEPENDENCY_PROP_NOTIFY__", changeNotificationFunction);
                    string depNotify = Templates.DependencyNotify.Replace("__PROPERTYNAME__", model.PropertyName);
                    depNotify = depNotify.Replace("__TYPE__", model.PropertyType);
                    depNotify = depNotify.Replace("__CLASS__", model.ClassType);
                    depNotify = depNotify.Replace("__USER_CODE__", model.UserCode);

                    sb.Append(depNotify);
                }
                else
                {
                    sb.Replace("__NOTIFY__", Templates.Notify);
                }
            }
            else
            {
                if (model.DependencyProperty)
                {
                    sb.Replace("__DEPENDENCY_PROP_NOTIFY__", "");
                }
                else
                {
                    sb.Replace("__NOTIFY__", "");
                }
            }

            return sb.ToString();

        }



        public static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element; while (parent != null)
            {
                T correctlyTyped = parent as T; if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }
                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }

        private void Button_AddNew(object sender, RoutedEventArgs e)
        {
            var model = new PropertyModel();
            PropertyList.Add(model);
            SelectedProperty = model;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (this.SelectedProperty == null)
            {
                Debug.WriteLine("SelectedProperty is null");
                return;
            }
            CheckBox cb = e.OriginalSource as CheckBox;
            if (cb.Name == "ChangeNotification")
                SelectedProperty.ChangeNotification = (bool)cb.IsChecked; // databinding happens *after* this call.  wtf??
            else
                SelectedProperty.DependencyProperty = (bool)cb.IsChecked;
            PropertiesAsText = GenerateProperty(this.SelectedProperty);
            GenerateAllProperties();

        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.SelectedProperty == null)
            {
                Debug.WriteLine("SelectedProperty is null");
                return;
            }
            CheckBox cb = e.OriginalSource as CheckBox;
            if (cb.Name == "ChangeNotification")
                SelectedProperty.ChangeNotification = (bool)cb.IsChecked; // databinding happens *after* this call.  wtf??
            else
                SelectedProperty.DependencyProperty = (bool)cb.IsChecked;
            PropertiesAsText = GenerateProperty(this.SelectedProperty);
            GenerateAllProperties();
        }

        ///
        /// Assumptions:
        ///    1. the only thing public in the parsed text is the property name (e.g. fields can be private or unspecified, but not public)
        ///    2. all properties have a get, but they don't need a set (but we always generate a set)
        ///    3. the code compiles!
        ///    4. this code does not handle comments -- any commented out property will also be parsed/added
        ///    5. User code in dependency property change notification functions is preserved
        ///    6. No user code in get functions!
        ///    
        /// NOTE:  if you paste this file into the AllPropertiesAsText TextBox, it will correctly parse out the properties...
        /// 
        private void OnParse(object sender, RoutedEventArgs e)
        {
            PropertyList.Clear();
            string toParse = AllPropertiesAsText.Replace("\t", ""); // no tabs
            toParse = toParse.Replace("  ", ""); // no double spaces
            toParse = toParse.Replace("\n", "");

            var properties = toParse.Split("public static readonly DependencyProperty", StringSplitOptions.RemoveEmptyEntries);
            if (properties.Length > 0)
            {
                //
                //  we have at least one dependency property
                foreach (var property in properties)
                {
                    string propString = property.Trim().Replace(" ", "");
                    if (propString == "") continue; // an errant /r
                    string depPropDeclaration = GetStringBetween(propString, "DependencyProperty.Register(", ";");
                    if (depPropDeclaration == "") continue;

                    var tokens = depPropDeclaration.Split(",", StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length < 4) continue;
                    var parsedModel = new PropertyModel()
                    {
                        DependencyProperty = true
                    };

                    parsedModel.PropertyName = GetStringBetween(tokens[0], "\"", "\"");
                    parsedModel.PropertyType = GetStringBetween(tokens[1], "(", ")");
                    parsedModel.ClassType = GetStringBetween(tokens[2], "(", ")");
                    if (tokens.Length == 4)
                    {
                        parsedModel.Default = GetStringBetween(tokens[3], "(", ")");

                    }
                    else if (tokens.Length == 5)
                    {
                        parsedModel.Default = tokens[3].Substring("newPropertyMetadata(".Length);
                        parsedModel.ChangeNotification = true;
                        parsedModel.UserCode = GetUserCode(toParse, parsedModel.PropertyName);
                    }
                    else
                    {
                        Debug.WriteLine($"Unexepected token length in dependency property declartion: {depPropDeclaration}");
                    }

                    PropertyList.Add(parsedModel);
                }
            }

            properties = toParse.Split("public", StringSplitOptions.RemoveEmptyEntries);
            foreach (var property in properties)
            {
                var getLoc = property.IndexOf("get");
                if (getLoc == -1) continue; // not a property -- some other code in the file.  maybe declaration of the field name plus the default, but we'll get those below
                if (property.IndexOf("GetValue") != -1) continue; // this means it is a dependency property and we parsed it above
                var lines = property.Split("\r", StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0) continue; // empty new lines in file




                //
                //  yes, this is a property
                PropertyModel parsedModel = new PropertyModel();


                var tokens = lines[0].Split(" ", StringSplitOptions.RemoveEmptyEntries);
                //  the property is in the form public __TYPE__ __PROPERTYNAME__, but "public" has been stripped                    
                if (tokens.Length != 2)
                {
                    Debug.WriteLine($"Unknown format at line {lines[0]}");
                    continue;
                }

                parsedModel.PropertyType = tokens[0].Trim();
                parsedModel.PropertyName = tokens[1].Trim();

                for (int i = 1; i < lines.Length; i++)
                {
                    if (lines[i] == "{" || lines[i] == "}") continue;

                    string line = lines[i];

                    getLoc = line.IndexOf("get");
                    if (getLoc != -1)
                    {
                        tokens = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                        var next = lines[i].Trim() + lines[i + 1].Trim();

                        if (next == "get{")
                        {
                            i += 2;
                            tokens = lines[i].Split(" ", StringSplitOptions.RemoveEmptyEntries);
                            if (tokens.Length != 2)
                            {
                                Debug.WriteLine($"Unknown format at line {lines[i]}");
                                continue;
                            }
                            if (tokens[0] != "return")
                            {
                                Debug.WriteLine($"Unknown format at line {lines[i]}");
                                continue;
                            }

                            parsedModel.FieldName = tokens[1].Substring(0, tokens[1].Length - 1).Trim(); // strip ";"

                        }
                        else
                        {

                            //
                            //  look for alternate form
                            tokens = line.Split("=>", StringSplitOptions.RemoveEmptyEntries);
                            if (tokens.Length == 2)
                            {

                                parsedModel.FieldName = tokens[1].Substring(0, tokens[1].Length - 1).Trim(); // strip the ";"
                            }
                            else
                            {
                                Debug.WriteLine($"Unknown format at line {line}");
                                continue;
                            }
                        }
                    }

                    if (line.IndexOf("NotifyPropertyChanged()") != -1)
                    {
                        parsedModel.ChangeNotification = true;

                    }
                }
                PropertyList.Add(parsedModel);
            }

            //
            //  now we need to find the defaults
            toParse = toParse.Replace(" ", ""); // no spaces!
            foreach (var prop in PropertyList)
            {
                //
                //  looking for __TYPE__ __FIELDNAME__ = __DEFAULT__;
                //
                if (prop.DependencyProperty) continue;
                var index = toParse.IndexOf(prop.FieldName + "=");
                if (index != -1)
                {
                    var eol = toParse.IndexOf("\r", index);
                    var fnLength = prop.FieldName.Length;
                    var defValue = toParse.Substring(index + fnLength + 1, eol - index - fnLength - 2);
                    if (defValue != "value") // we pick up   __FIELDNAME__ = value;
                    {
                        prop.Default = defValue;
                    }

                }
            }
            if (PropertyList.Count > 0)
            {
                SelectedProperty = PropertyList[0];
            }
        }

        private string GetUserCode(string parseString, string propertyName)
        {
            string functionName = "Set" + propertyName;
            int idx = parseString.IndexOf(functionName);
            // find first { after function name
            int firstCurly = parseString.IndexOf("{", idx + 1);
            var charArray = parseString.ToCharArray(firstCurly + 1, parseString.Length - firstCurly - 1);
            int braceCount = 1;
            string userCode = "";
            for (int i = 0; i < charArray.Length; i++)
            {
                userCode += charArray[i].ToString();
                if (charArray[i] == '{')
                    braceCount++;
                if (charArray[i] == '}')
                    braceCount--;

                if (braceCount == 0) break;
            }
            //
            //  we end with the trailing brace, which we don't want
            return userCode.Substring(0, userCode.Length - 1).Trim();
        }

        private string GetStringBetween(string toParse, string start, string end)
        {
            int startPos = toParse.IndexOf(start);
            if (startPos == -1) return "";
            int endPos = toParse.IndexOf(end, startPos + 1);
            if (endPos == -1) return "";
            var ret = toParse.Substring(startPos + start.Length, endPos - end.Length - startPos - start.Length + 1);
            return ret;
        }
        private void OnDeleteCurrent(object sender, RoutedEventArgs e)
        {
            if (SelectedProperty != null)
            {
                PropertyList.Remove(SelectedProperty);
            }
        }


        public async Task<string> GetUserString(string title, string defaultText)
        {
            var inputTextBox = new TextBox { AcceptsReturn = false, Text = defaultText };
            inputTextBox.SelectAll();
            (inputTextBox as FrameworkElement).VerticalAlignment = VerticalAlignment.Bottom;
            var dialog = new ContentDialog
            {
                Content = inputTextBox,
                Title = title,
                IsSecondaryButtonEnabled = true,
                PrimaryButtonText = "Ok",
                SecondaryButtonText = "Cancel"
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                return inputTextBox.Text;
            else
                return "";
        }

        private async void Choice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            string choice = ((ComboBoxItem)_cmbChoice.SelectedItem).Content as string;
            switch (choice)
            {
                case "Dependency Property":
                    {
                        foreach (var prop in PropertyList)
                        {
                            prop.DependencyProperty = true;
                        }
                    }
                    break;
                case "Regular Property":
                    {
                        foreach (var prop in PropertyList)
                        {
                            prop.DependencyProperty = false;
                        }
                    }
                    break;

                case "Default Field Names":
                    {
                        foreach (var prop in PropertyList)
                        {
                            prop.FieldName = "_" + prop.PropertyName;
                        }
                    }

                    break;
                case "Set Default Class Type":

                    {
                        var classType = await GetUserString("Property Wizard", "Enter the default class type here");
                        foreach (var prop in PropertyList)
                        {
                            prop.ClassType = classType;
                        }
                    }
                    break;

                default: break;

            }
            GenerateAllProperties();
        }
    }
}
