/* this means we have something like this:
                     
                        public Foo()
                        {
                        }

                    */
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
        //  in a comment!
        //private void SetTest(string value)
        //{

        //}
        /*
         * in a comment!!
        private void SetTest(string value)
        {

        }
        */
        private ObservableCollection<PropertyModel> PropertyList { get; } = new ObservableCollection<PropertyModel>();
        public static readonly DependencyProperty TestProperty = DependencyProperty.Register("Test", typeof(string), typeof(MainPage), new PropertyMetadata(",", TestChanged));
        public string Test
        {
            get => (string)GetValue(TestProperty);
            set => SetValue(TestProperty, value);
        }
        private static void TestChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var depPropClass = d as MainPage;
            var depPropValue = (string)e.NewValue;
            depPropClass?.SetTest(depPropValue);
        }
        //  in a comment!
        //private void SetTest(string value)
        //{

        //}
        /*
         * in a comment!!
        private void SetTest(string value)
        {

        }
        */
        private void SetTest(string value)
        {
            Debug.Print($"Test changed to: {value}");
        }

        private bool _parsing = false;

        public static readonly DependencyProperty PropertiesAsTextProperty = DependencyProperty.Register("PropertiesAsText", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty AllPropertiesAsTextProperty = DependencyProperty.Register("AllPropertiesAsText", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty SetAllChoiceProperty = DependencyProperty.Register("SetAllChoice", typeof(string), typeof(MainPage), new PropertyMetadata("", SetAllChoiceChanged));
        public static readonly DependencyProperty SelectedPropertyProperty = DependencyProperty.Register("SelectedProperty", typeof(PropertyModel), typeof(MainPage), new PropertyMetadata(null, SelectedPropertyChanged));
        public static readonly DependencyProperty AllCodeProperty = DependencyProperty.Register("AllCode", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty CodeNoPropertiesProperty = DependencyProperty.Register("CodeNoProperties", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty ParseCodeProperty = DependencyProperty.Register("ParseCode", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public string ParseCode
        {
            get => (string)GetValue(ParseCodeProperty);
            set => SetValue(ParseCodeProperty, value);
        }
        public string CodeNoProperties
        {
            get => (string)GetValue(CodeNoPropertiesProperty);
            set => SetValue(CodeNoPropertiesProperty, value);
        }
        public string AllCode
        {
            get => (string)GetValue(AllCodeProperty);
            set => SetValue(AllCodeProperty, value);
        }

        public PropertyModel SelectedProperty
        {
            get => (PropertyModel)GetValue(SelectedPropertyProperty);
            set => SetValue(SelectedPropertyProperty, value);
        }
        private static void SelectedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var depPropClass = d as MainPage;
            var depPropValue = (PropertyModel)e.NewValue;
            depPropClass?.SetSelectedProperty(depPropValue);
        }
        private void SetSelectedProperty(PropertyModel model)
        {
            if (model == null) return;
            PropertiesAsText = GenerateProperty(model);
            // do not call GenerateAllProperties() as all we did is change the list selection and it will already have the full prop list
        }

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

        public MainPage()
        {
            this.InitializeComponent();
            PropertyList.CollectionChanged += PropertyList_CollectionChanged;

        }

        private void PropertyList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                string defaultClassType = "";
                if (PropertyList.Count > 0)
                {
                    defaultClassType = PropertyList[0].ClassType;
                }
                foreach (PropertyModel model in e.NewItems)
                {
                    model.PropertyChanged += Model_PropertyChanged;
                    if (model.ClassType == "" && model.IsDependencyProperty)
                    {
                        model.ClassType = defaultClassType;
                    }
                }
            }
            if (e.OldItems != null)
            {
                foreach (PropertyModel model in e.OldItems)
                {
                    model.PropertyChanged -= Model_PropertyChanged;
                }
            }

            GenerateAllProperties();
        }


        //
        //  make sure if you click inside a textbox that the list item gets selected



        private void ListView_ItemClicked(object sender, ItemClickEventArgs e)
        {
            PropertyModel model = e.ClickedItem as PropertyModel;
            Debug.WriteLine("ListView_ItemClicked");
            PropertiesAsText = GenerateProperty(model);
        }



        private void GenerateAllProperties()
        {
            if (_parsing) return; 
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
            // Debug.WriteLine(json);


            StringBuilder sb;
            if (model.IsDependencyProperty)
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
                if (model.IsDependencyProperty)
                {
                    string changeNotificationFunction = ", " + model.PropertyName + "Changed";
                    sb.Replace("__DEPENDENCY_PROP_NOTIFY__", changeNotificationFunction);
                    string depNotify = Templates.DependencyNotify.Replace("__PROPERTYNAME__", model.PropertyName);
                    depNotify = depNotify.Replace("__TYPE__", model.PropertyType);
                    depNotify = depNotify.Replace("__CLASS__", model.ClassType);
                    depNotify = depNotify.Replace("__USER_CODE__", model.UserSetCode);

                    sb.Append(depNotify);
                }
                else
                {
                    sb.Replace("__NOTIFY__", Templates.Notify);
                }
            }
            else
            {
                if (model.IsDependencyProperty)
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





        private void Button_AddNew(object sender, RoutedEventArgs e)
        {
            var model = new PropertyModel();
            PropertyList.Add(model);
            SelectedProperty = model;
        }
        private void OnDeleteCurrent(object sender, RoutedEventArgs e)
        {
            if (SelectedProperty != null)
            {
                PropertyList.Remove(SelectedProperty);
            }
        }


        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            PropertyModel model = sender as PropertyModel;
            if (model == null) return;

            if (e.PropertyName == "IsDependencyProperty")
            {
                if (model.IsDependencyProperty && model.ClassType == "" && PropertyList[0].ClassType != "")
                {
                    model.ClassType = PropertyList[0].ClassType;
                }
            }
            PropertiesAsText = GenerateProperty(model);

            GenerateAllProperties();
        }





        /*
         * Looks like:
         * 
         * public __TYPE__ __PROPERTYNAME__
            {
                get
                {
                    return __FIELDNAME__;
                }
                set
                {
                    if (value != __FIELDNAME__)
                    {
                        __FIELDNAME__ = value;
                        __NOTIFY__
                    }
                }
            }";
         * 
         * 
         */



        private string GetStringBetween(string toParse, string start, string end)
        {
            int startPos = toParse.IndexOf(start);
            if (startPos == -1) return "";
            int endPos = toParse.IndexOf(end, startPos + 1);
            if (endPos == -1) return "";
            var ret = toParse.Substring(startPos + start.Length, endPos - end.Length - startPos - start.Length + 1);
            return ret;
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
                            prop.IsDependencyProperty = true;
                        }
                    }
                    break;
                case "Regular Property":
                    {
                        foreach (var prop in PropertyList)
                        {
                            prop.IsDependencyProperty = false;
                        }
                    }
                    break;

                case "Default Field Names":
                    {

                        foreach (var prop in PropertyList)
                        {
                            prop.FieldName = "_" + char.ToLower(prop.PropertyName[0]) + prop.PropertyName.Substring(1); ;
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

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                tb.SelectAll();
            }
        }

        private void OnParse(object sender, RoutedEventArgs e)
        {
            try
            {
                _parsing = true;
                var pfi = PropertyParser.ParseFile(ParseCode);
                PropertyList.Clear();
                foreach (var prop in pfi.PropertyList)
                {
                    PropertyList.Add(prop);
                }

                CodeNoProperties = pfi.NoProperties;
                if (PropertyList.Count > 0)
                {
                    SelectedProperty = PropertyList[0];
                }
            }
            finally
            {
                _parsing = false;
                GenerateAllProperties();
            }
        }
    }


}
