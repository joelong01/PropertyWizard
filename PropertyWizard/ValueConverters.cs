using System;
using Windows.UI.Xaml.Data;

//
//  DON'T FORGET: add your converter class to app.xaml as a resource...
namespace PropertyWizard
{
    //
    //  inverts true to false and false to true
    public class NotBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !((bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return !((bool)value);
        }
    }
    public class NullableBoolToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((bool)value);
        }
    }
}
