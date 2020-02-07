using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace PropertyWizard
{
    public class PropertyModel : INotifyPropertyChanged
    {
        string _PropertyName = "";
        string _FieldName = "";
        string _PropertyType = "";
        string _ClassType = "";
        string _Default = "";
        bool _isDependencyProperty = false;
        bool _ChangeNotification = false;
        bool _HasSetter = true;
        string _Comment = "";
        public string Comment
        {
            get
            {
                return _Comment;
            }
            set
            {
                if (_Comment != value)
                {
                    _Comment = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool HasSetter
        {
            get
            {
                return _HasSetter;
            }
            set
            {
                if (_HasSetter != value)
                {
                    _HasSetter = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string FieldName
        {
            get
            {
                return _FieldName;
            }
            set
            {
                if (_FieldName != value)
                {
                    _FieldName = StripSpacesAndTrailingSemi(value);
                    NotifyPropertyChanged();
                }
            }
        }
        public bool ChangeNotification
        {
            get
            {
                return _ChangeNotification;
            }
            set
            {
                if (_ChangeNotification != value)
                {
                    _ChangeNotification = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool IsDependencyProperty
        {
            get
            {
                return _isDependencyProperty;
            }
            set
            {
                if (_isDependencyProperty != value)
                {
                    _isDependencyProperty = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Default
        {
            get
            {
                return _Default;
            }
            set
            {
                if (_Default != value)
                {
                    _Default = StripSpacesAndTrailingSemi(value);
                    NotifyPropertyChanged();
                }
            }
        }
        public string ClassType
        {
            get
            {
                return _ClassType;
            }
            set
            {
                if (_ClassType != value)
                {
                    _ClassType = StripSpacesAndTrailingSemi(value);
                    NotifyPropertyChanged();
                }
            }
        }
        public string PropertyType
        {
            get
            {
                return _PropertyType;
            }
            set
            {
                if (_PropertyType != value)
                {
                    _PropertyType = StripSpacesAndTrailingSemi(value);
                    NotifyPropertyChanged();
                }
            }
        }
        public string PropertyName
        {
            get
            {
                return _PropertyName;
            }
            set
            {
                if (_PropertyName != value)
                {
                    _PropertyName = StripSpacesAndTrailingSemi(value);
                    if (FieldName == "" && _PropertyName.Length > 0)
                    {
                        FieldName = "_" + char.ToLower(_PropertyName[0]) + _PropertyName.Substring(1);
                    }
                    NotifyPropertyChanged();
                }
            }
        }
        string _UserSetCode = "";
        public string UserSetCode
        {
            get
            {
                return _UserSetCode;
            }
            set
            {
                if (_UserSetCode != value)
                {
                    _UserSetCode = StripSpacesAndTrailingSemi(value);
                    NotifyPropertyChanged();
                }
            }
        }

        string _ChangeNotificationFunction = "";
        public string ChangeNotificationFunction
        {
            get
            {
                return _ChangeNotificationFunction;
            }
            set
            {
                if (_ChangeNotificationFunction != value)
                {
                    _ChangeNotificationFunction = StripSpacesAndTrailingSemi(value);
                    NotifyPropertyChanged();
                }
            }
        }
        string _UserGetCode = "";
        public string UserGetCode
        {
            get
            {
                return _UserGetCode;
            }
            set
            {
                if (_UserGetCode != value)
                {
                    _UserGetCode = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string StripSpacesAndTrailingSemi(string prop)
        {
            var strip = prop.Trim(); // no spaces
            while (strip.EndsWith(';')) // don't end in a ; because the template adds one
            {
                strip = strip.Substring(0, strip.Length - 1);
            }
            return strip;
        }

        public string FieldDeclareLine { get; set; } = "";

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            //  Debug.WriteLine($"NotifyPropertyChanged: [{propertyName}={this.GetType().GetProperty(propertyName).GetValue(this)}]");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
