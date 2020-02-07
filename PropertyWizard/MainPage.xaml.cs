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
        private ObservableCollection<PropertyModel> PropertyList { get; } = new ObservableCollection<PropertyModel>();
        private string _defaultClassType = "";
        private bool _parsing = false;

        public static readonly DependencyProperty PropertiesAsTextProperty = DependencyProperty.Register("PropertiesAsText", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty AllPropertiesAsTextProperty = DependencyProperty.Register("AllPropertiesAsText", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty SetAllChoiceProperty = DependencyProperty.Register("SetAllChoice", typeof(string), typeof(MainPage), new PropertyMetadata("", SetAllChoiceChanged));
        public static readonly DependencyProperty SelectedPropertyProperty = DependencyProperty.Register("SelectedProperty", typeof(PropertyModel), typeof(MainPage), new PropertyMetadata(null, SelectedPropertyChanged));
        public static readonly DependencyProperty AllCodeProperty = DependencyProperty.Register("AllCode", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty CodeNoPropertiesProperty = DependencyProperty.Register("CodeNoProperties", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty ParseCodeProperty = DependencyProperty.Register("ParseCode", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty AllFieldsTogetherProperty = DependencyProperty.Register("AllFieldsTogether", typeof(bool), typeof(MainPage), new PropertyMetadata(true));
        public static readonly DependencyProperty DefaultToRegularPropertyProperty = DependencyProperty.Register("DefaultToRegularProperty", typeof(bool), typeof(MainPage), new PropertyMetadata(true));
        public bool DefaultToRegularProperty
        {
            get => (bool)GetValue(DefaultToRegularPropertyProperty);
            set => SetValue(DefaultToRegularPropertyProperty, value);
        }
        public bool AllFieldsTogether
        {
            get => (bool)GetValue(AllFieldsTogetherProperty);
            set => SetValue(AllFieldsTogetherProperty, value);
        }
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
            PropertiesAsText = BuildTemplateAndReplaceStrings(model);
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
            PropertiesAsText = BuildTemplateAndReplaceStrings(model);
        }

        private StringBuilder GetDeclareTemplate(PropertyModel prop)
        {
            StringBuilder sb = new StringBuilder();
            if (prop.IsDependencyProperty)
            {
                if (prop.ChangeNotification)
                {
                    sb = sb.Append(Templates.DependencyDeclareWidthNotification);
                }
                else
                {
                    sb.Append(Templates.DependencyDeclareNoNotify);
                }

            }
            else
            {

                sb.Append(Templates.FieldDeclare);
            }

            return sb;

        }

        private void GenerateAllProperties()
        {
            _cmbChoice.IsDropDownOpen = false;

            if (_parsing) return;
            StringBuilder sb = new StringBuilder();
            StringBuilder sbTemp;
            if (AllFieldsTogether)
            {
                foreach (var property in PropertyList)
                {
                    sbTemp = new StringBuilder();
                    sbTemp.Append(GetDeclareTemplate(property));
                    sbTemp.Append("\r");
                    sb = sb.Append(ReplaceMagicStrings(sbTemp, property));

                }

                sb.Append("\r");
            }

            foreach (var property in PropertyList)
            {
                sbTemp = new StringBuilder();
                if (!AllFieldsTogether)
                {
                    sbTemp = sbTemp.Append(GetDeclareTemplate(property));

                }
                BuildBodyTemplate(ref sbTemp, property);
                sb = sb.Append(ReplaceMagicStrings(sbTemp, property));
            }

            AllPropertiesAsText = sb.ToString();
        }
        private string ReplaceMagicStrings(StringBuilder sbin, PropertyModel model)
        {
            StringBuilder sb = new StringBuilder(sbin.ToString());
            sb = sb.Replace("__TYPE__", model.PropertyType);
            sb = sb.Replace("__DEFAULT__", model.Default);
            sb = sb.Replace("__PROPERTYNAME__", model.PropertyName);
            sb = sb.Replace("__FIELDNAME__", model.FieldName);
            sb = sb.Replace("__DEPENDENCY_PROP_NOTIFY__", model.ChangeNotificationFunction);
            sb = sb.Replace("__CLASS__", model.ClassType);
            sb = sb.Replace("__USER_CODE__", model.UserSetCode);
            return sb.ToString();

        }

        private void BuildBodyTemplate(ref StringBuilder sb, PropertyModel model)
        {
            if (model.IsDependencyProperty)
            {
                if (model.ChangeNotification)
                {
                    sb = sb.Append(Templates.DependencyBodyNotify);
                }
                else
                {
                    sb = sb.Append(Templates.DependencyBodyNoNotify);
                }
            }
            else if (model.ChangeNotification)
            {
                sb = sb.Append(Templates.RegularPropertyWithNotify);
            }
            else
            {
                sb = sb.Append(Templates.RegularPropertyNoNotify);
            }

        }

        private string BuildTemplateAndReplaceStrings(PropertyModel model)
        {

            StringBuilder sb = GetDeclareTemplate(model);
            BuildBodyTemplate(ref sb, model);
            return ReplaceMagicStrings(sb, model);

        }





        private void Button_AddNew(object sender, RoutedEventArgs e)
        {
            var model = new PropertyModel()
            {
                ClassType = _defaultClassType,
                IsDependencyProperty = !DefaultToRegularProperty,

            };
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
            PropertiesAsText = BuildTemplateAndReplaceStrings(model);

            GenerateAllProperties();
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

        private void OnMakeAllDependencyProperty(object sender, RoutedEventArgs e)
        {
            foreach (var prop in PropertyList)
            {
                prop.IsDependencyProperty = true;
            }
           
            GenerateAllProperties();
        }

        private void OnDefaultFieldNames(object sender, RoutedEventArgs e)
        {
            foreach (var prop in PropertyList)
            {
                if (prop.PropertyName.Length < 2) continue;
                prop.FieldName = "_" + char.ToLower(prop.PropertyName[0]) + prop.PropertyName.Substring(1); ;
            }
            GenerateAllProperties();
        }

        private void OnMakeAllRegularProperty(object sender, RoutedEventArgs e)
        {
            foreach (var prop in PropertyList)
            {
                prop.IsDependencyProperty = false;
            }
           
            GenerateAllProperties();
        }

        private async void OnSetDefaultClass(object sender, RoutedEventArgs e)
        {
            _defaultClassType = await GetUserString("Property Wizard", "Enter the default class type here");
            foreach (var prop in PropertyList)
            {
                prop.ClassType = _defaultClassType;
            }
            GenerateAllProperties();
        }

        private void OnComboxBoxCancel(object sender, RoutedEventArgs e)
        {
            _cmbChoice.IsDropDownOpen = false;

        }

        private void OnMakeAll(object sender, RoutedEventArgs e)
        {
            _cmbChoice.IsDropDownOpen = !_cmbChoice.IsDropDownOpen;


        }
    }


}
