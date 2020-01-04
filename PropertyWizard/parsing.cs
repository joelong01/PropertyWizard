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
    public enum FunctionType { DependencyProperty, Function, OneLiner, RegularProperty, Unknown };
    public class FunctionInfo
    {
        public FunctionType FunctionType { get; set; } = FunctionType.Unknown;
        public string UserSetCode { get; set; } = "";
        public string UserGetCode { get; set; } = "";
        public string ReturnType { get; set; } = "";
        public string AccessModifier { get; set; } = "";
        public string Name { get; set; } = "";
        public string Code { get; internal set; }
        public int OldLocation { get; internal set; }
        public int NewLocation { get; internal set; }
    }

    public class ParsedFileInfo
    {
        public List<PropertyModel> PropertyList { get; set; } = new List<PropertyModel>();
        public string NoProperties { get; set; } = "";

    }


    public static class PropertyParser
    {
        ///
        /// Assumptions:
        ///    1. property declaration is on one line
        ///    2. all properties have a get, but they don't need a set (but we always generate a set)
        ///    3. the code compiles!
        ///    4. this code does not handle comments -- any commented out property will also be parsed/added
        ///    5. User code in dependency property change notification functions is preserved
        ///    6. No user code in get functions!
        ///    7. properties are either public or private
        ///    
        ///    
        /// NOTE:  if you paste this file into the AllPropertiesAsText TextBox, it will correctly parse out the properties...
        /// 

        public static ParsedFileInfo ParseFile(string toParse)
        {

            ParsedFileInfo parsedInfo = new ParsedFileInfo();
            StringBuilder nonPropertyCode = new StringBuilder();

            //
            //  firt get all the lines
            // var lines = AllPropertiesAsText.Split("\r", StringSplitOptions.RemoveEmptyEntries);
            //for (int i = 0; i < lines.Length; i++)
            int currentLocation = 0;
            int oldLoc;
            string nl = "\r";
            PropertyModel model;
            HashSet<string> dependencyPropertySetFunctions = new HashSet<string>();
            FunctionInfo functionInfo;
            bool foundFirstClass = false;
            while (true)
            {

                (oldLoc, currentLocation) = GetLine(toParse, currentLocation, out string line);
                Debug.WriteLine($"{currentLocation}: {line}");
                if (line.Contains("_parsing"))
                {
                    Debug.WriteLine("here)");
                }
                if (currentLocation == -1)
                    break;
                if (line == "")
                {
                    nonPropertyCode.Append(nl);
                    continue;
                }
                if (currentLocation >= toParse.Length) break;
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("public") == false && trimmedLine.StartsWith("private") == false)
                {
                    nonPropertyCode.Append(line + nl);
                    continue;
                }
                int commentStartPos = InsideComment(toParse, currentLocation);
                if (commentStartPos != -1)
                {
                    /* this means we have something like this:

                        public Foo()
                        {
                        }

                    */

                    int endCommentPos = toParse.IndexOf("*/", currentLocation);
                    string restOfComment = toParse.Substring(currentLocation, endCommentPos - currentLocation + 2);
                    nonPropertyCode.Append(line + nl);
                    nonPropertyCode.Append(restOfComment);
                    currentLocation = endCommentPos + 2;
                    continue;
                }

                /**
                 * 
                 *  this is a long comment!
                 * 
                 * 
                 */

                if (line.Contains("get;") && line.Contains("{") && line.Contains("}"))
                {
                    // then it starts with public or private and has a get in it -- assume one liner of the form
                    // public __TYPE__ __PROPERTYNAME__ { get;set; } = __DEFAULT__ where set is optional.
                    Debug.WriteLine($"{line}");
                    model = ParseOneLiner(line);
                    if (model != null)
                    {
                        parsedInfo.PropertyList.Add(model);
                    }
                    else
                    {
                        nonPropertyCode.Append(line + nl);
                    }
                    continue;
                }

                if (line.Contains("("))
                {
                    //   we know we have some kind of function declaration 

                    if (line.Contains("DependencyProperty.Register"))
                    {
                        // dependency property declaration                        
                        model = ParseDependencyProperty(toParse, line);
                        if (model != null)
                        {
                            parsedInfo.PropertyList.Add(model);
                            dependencyPropertySetFunctions.Add($"Set{model.PropertyName}"); // assumption!!
                            dependencyPropertySetFunctions.Add($"{model.PropertyName}Changed"); // assumption!!
                        }
                        else
                        {
                            Debug.Assert(false, $"how did we get here? {line}");
                            nonPropertyCode.Append(line + nl);
                        }

                        continue;

                    }
                    else
                    {
                        //
                        //  user code of some kind
                        functionInfo = GetFunction(toParse, oldLoc); // pass in oldLoc because you want the function name in the string
                        currentLocation = functionInfo.NewLocation;
                        if (dependencyPropertySetFunctions.Contains(functionInfo.Name) == false) // this is a generated function
                        {
                            nonPropertyCode.Append(functionInfo.Code);
                        }
                        else
                        {
                            Debug.WriteLine($"threw away {line}");
                        }
                        continue;

                    }
                }
                else if (line.Contains("class"))
                {
                    if (foundFirstClass)
                    {
                        break; // stop if we find a second class

                    }
                    nonPropertyCode.Append(line + nl);
                    foundFirstClass = true;
                    continue;
                }
                else if (line.Contains(";"))
                {
                    // this is a simple declartion like "bool _parsing = true;" or "bool _parsing;" ==> properties cannnot have a ";" at the end of their declations
                    nonPropertyCode.Append(line);
                    continue;
                }
                //
                // if we get here we have a property

                functionInfo = GetFunction(toParse, oldLoc);
                currentLocation = functionInfo.NewLocation;
                // Debug.WriteLine($"{property}");
                if (functionInfo.Code.Contains("GetValue"))
                {
                    // this  is a dependency property that we don't need -- throw it away.
                    continue;
                }
                Debug.WriteLine($"it is a normal property named {functionInfo.Name} type is: {functionInfo.FunctionType}");
                model = ParseProperty(functionInfo, toParse);
                if (model != null)
                {
                    parsedInfo.PropertyList.Add(model);
                    continue;
                }

                Debug.WriteLine($"have unparsed code! {line}");

            }
            parsedInfo.NoProperties = nonPropertyCode.ToString();
            return parsedInfo;
        }
        private static (int oldLoc, int newLoc) GetLine(string toParse, int start, out string line)
        {
            line = "";
            if (start >= toParse.Length) return (-1, -1);

            string nl = "\r";

            int loc = toParse.IndexOf(nl, start);
            if (loc == -1)
                return (-1, -1);

            line = toParse.Substring(start, loc - start);
            return (start, loc + nl.Length);

        }
        //
        //  is the code at location inside toParse part of a comment?
        private static int InsideComment(string toParse, int location)
        {
            int index = location + 1; // this will guarantee that we have two non-empty chars to start with...
            string line = "";
            while (toParse[index] != '\r' && index > 0)
            {
                line = toParse[index].ToString() + line;
                index--;  // go to previous line break
            }

            line = line.TrimStart();
            // Debug.WriteLine($"Line={line}");



            if (line.Length > 2 && line.Substring(0, 2) == "//")
                return index;
            /*
             *
             * 
              if you have something like this get then you are in a comment
              
             * 
             */
            line = "";
            for (; ; )
            {
                if ((toParse[index] == '/' && toParse[index + 1] == '*')) return index;
                if (toParse[index] == '*' && toParse[index + 1] == '/') return -1;
                if (index == 0) return -1;

                line = toParse[index].ToString() + line;
                index--;  // go to previous line break
            }

        }


        // public __TYPE__ __PROPERTYNAME__ { get;set; } = __DEFAULT__ where set is optional.
        private static PropertyModel ParseOneLiner(string line)
        {
            var tokens = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 4)
            {
                return null;
            }

            var model = new PropertyModel();
            model.HasSetter = line.Contains("set;");
            model.PropertyType = tokens[1];
            model.PropertyName = tokens[2];
            tokens = line.Split("=", StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 2)
            {
                model.Default = tokens[1];
            }
            return model;
        }

        //  parse something that looks like:  
        // public static readonly DependencyProperty TwelvePercentProperty = DependencyProperty.Register("TwelvePercent", typeof(string), typeof(MainPage), new PropertyMetadata("0 (0%)"));
        //      -- or --
        // public static readonly DependencyProperty TwelvePercentProperty = DependencyProperty.Register("TwelvePercent", typeof(string), typeof(MainPage), new PropertyMetadata("0 ,(0%)", OnChagePercent));
        //
        // (oldLoc, currentLocation) = ParseDependencyProperty(line, currentLocation, out model);
        private static PropertyModel ParseDependencyProperty(string allText, string line)
        {
            //
            //  line looks like "        public static readonly DependencyProperty PropertiesAsTextProperty = DependencyProperty.Register(\"PropertiesAsText\", typeof(string), typeof(MainPage), new PropertyMetadata(\"\"));"
            var model = new PropertyModel
            {
                IsDependencyProperty = true
            };
            //
            //  get the text passed into .Register            
            int pos1 = line.IndexOf("(");
            int pos2 = line.LastIndexOf(")");
            string parameters = line.Substring(pos1, pos2 - pos1);
            //
            //  parameters looks something like "(\"PropertiesAsText\", typeof(string), typeof(MainPage), new PropertyMetadata(\"\")"
            string setterFunction = "";
            //
            //  pick out the first parameter finding text between the first two quotes
            pos1 = parameters.IndexOf("\"");
            pos2 = parameters.IndexOf("\"", pos1 + 1);
            model.PropertyName = parameters.Substring(pos1 + 1, pos2 - pos1 - 1);

            //
            //  type is inside the typeof() parens
            pos1 = parameters.IndexOf("(", pos2);
            pos2 = parameters.IndexOf(")", pos1 + 1);
            model.PropertyType = parameters.Substring(pos1 + 1, pos2 - pos1 - 1);

            //
            //  class type is insde the next typeof()
            pos1 = parameters.IndexOf("(", pos2 + 1);
            pos2 = parameters.IndexOf(")", pos1 + 1);
            model.ClassType = parameters.Substring(pos1 + 1, pos2 - pos1 - 1);

            // get what is passed into new PropertyMetaData
            pos1 = parameters.IndexOf("(", pos2 + 1);
            pos2 = parameters.LastIndexOf(")");
            var pmd = parameters.Substring(pos1 + 1, pos2 - pos1 - 1);
            //
            //  pmd is the *inside* of the parens -- so something like "\"\""
            //
            //  this code is because there may be a "," inside the default, so I scan forward
            int index = 0;
            int quoteCount = 0;
            while (index != pmd.Length)
            {
                if (pmd[index] == '\\') // ignore next char
                    index++;
                if (pmd[index] == '"')
                {

                    quoteCount++;
                }
                if ((pmd[index] == ',' && quoteCount % 2 == 0) || pmd[index] == ')')
                    break;

                model.Default += pmd[index].ToString();

                index++;
            }
            if (pmd.Length <= index || pmd[index] != ',')
            {
                model.ChangeNotification = false;
                return model;
            }



            pos1 = pmd.LastIndexOf(",");

            model.ChangeNotification = true;
            model.ChangeNotificationFunction = pmd.Substring(pos1 + 1, pmd.Length - pos1 - 1);
            /*
             *   this pulls the name from the DependencyProperty.Register line.  the function will look like:
             *  
                private static void TestChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
                {
                    var depPropClass = d as MainPage;
                    var depPropValue = (string)e.NewValue;
                    depPropClass?.SetTest(depPropValue);
                }
                private void SetTest(string value)
                {

                }

              * 
              */
            var magic = $"depPropClass?.Set{model.PropertyName}(depPropValue);";
            int userSetLoc = -1;
            do
            {
                userSetLoc = allText.IndexOf(magic, userSetLoc + 1); // but what if this is in a comment like what we have above???

            }
            while (InsideComment(allText, userSetLoc) != -1);


            var paren = allText.IndexOf("(", userSetLoc);
            int len = "depPropClass?".Length;
            setterFunction = allText.Substring(userSetLoc + len + 1, paren - len - userSetLoc - 1);
            //
            //  we now have the name of the function -- find the the set code

            magic = "private void " + setterFunction;
            userSetLoc = -1;
            do
            {
                userSetLoc = allText.IndexOf(magic, userSetLoc + 1); // but what if this is in a comment like what we have above???

            }
            while (InsideComment(allText, userSetLoc) != -1);


            var functionInfo = GetFunction(allText, userSetLoc);
            model.UserSetCode = functionInfo.Code;




            return model;
        }

        /// <summary>
        ///     given an offset into the file, it will find the function and populate the FunctionInfo object
        /// </summary>
        /// <param name="toParse"></param>
        /// <param name="fileLoc"></param>        
        /// <returns></returns>
        private static FunctionInfo GetFunction(string toParse, int fileLoc)
        {
            int curlyCount = 1;
            _ = GetLine(toParse, fileLoc, out string line);
            var functionInfo = ParseFunctionDeclartion(line);

            int pos = toParse.IndexOf("{", fileLoc) + 1;
            if (pos == -1)
            {
                throw new Exception("this is unexpected. should be a function here!");
            }
            while (pos < toParse.Length)
            {


                if (toParse[pos] == '{')
                {
                    curlyCount++;
                }
                else if (toParse[pos] == '}')
                {
                    curlyCount--;
                }


                pos++;

                if (curlyCount == 0) break;
            }

            functionInfo.Code = toParse.Substring(fileLoc, pos - fileLoc);
            functionInfo.OldLocation = fileLoc;
            functionInfo.NewLocation = pos;
            return functionInfo;

        }


        // private static void SetAllChoiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        // private (int oldLoc, int newLoc, string functionName) GetFunction(string toParse, int fileLoc, out string func)
        // public MainPage()
        // private string GetFunctionName(string line)
        // SetSetAllChoice(depPropValue);
        // public bool HasSetter
        // public bool HasSetter // comment
        private static FunctionInfo ParseFunctionDeclartion(string line)
        {
            FunctionInfo functionInfo = new FunctionInfo();
            int lastParenIndex = line.LastIndexOf(")");
            if (lastParenIndex == -1)
            {
                //
                //  property?
                var tokens = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length >= 3)
                {
                    functionInfo.FunctionType = FunctionType.RegularProperty;
                    functionInfo.AccessModifier = tokens[0];
                    functionInfo.ReturnType = tokens[1];
                    functionInfo.Name = tokens[2];
                    return functionInfo;
                }

                return null;
            }
            int index = lastParenIndex - 1;
            int parenCount = 1;
            do
            {
                if (line[index] == ')') parenCount++;
                if (line[index] == '(') parenCount--;

                index--;
            } while (parenCount != 0);

            // index is now at the end of the function name;
            int end = index;
            while (line[index] != ' ' && index > 0) index--;
            if (index > 0) index++;
            functionInfo.Name = line.Substring(index, end - index + 1);
            functionInfo.FunctionType = FunctionType.Function;
            return functionInfo; // we don't care about any other data for a function

        }



        private static string GetUserCode(string parseString, string propertyName)
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

        /// <summary>
        ///     assumes
        ///     1. set comes after get
        ///     2. code compiles
        ///     3. the change Notification function is "tNotifyPropertyChanged()"
        ///     4. the declare line looks exactly like $"{model.PropertyType} {model.FieldName} = model.{default}";
        ///     5. all properties have a get
        ///     6. set is optional
        /// </summary>
        /// <param name="propName"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        private static PropertyModel ParseProperty(FunctionInfo functionInfo, string toParse)
        {
            if (functionInfo == null)
                return null;




            int startOfGet = functionInfo.Code.IndexOf("get");
            if (startOfGet == -1)
                return null;


            var model = new PropertyModel()
            {
                IsDependencyProperty = false,
                PropertyName = functionInfo.Name,
                PropertyType = functionInfo.ReturnType,


            };



            int endOfGet = functionInfo.Code.IndexOf("set");
            model.HasSetter = (endOfGet != -1);
            if (!model.HasSetter) endOfGet = functionInfo.Code.Length; // if there is no setter, it is all get code

            //
            //  i'm using "set".Length and "get".Length here so that I don't have to remember why there is magic "+3" or "-3" sprinkled in this code...
            model.UserGetCode = functionInfo.Code.Substring(startOfGet, endOfGet - startOfGet).Trim();

            int returnPos = model.UserGetCode.IndexOf("return");
            int eolPos = model.UserGetCode.IndexOf(";", returnPos);
            model.FieldName = model.UserGetCode.Substring(returnPos + "return".Length, eolPos - returnPos - "return".Length).Trim();

            string declareLine = $"{model.PropertyType} {model.FieldName} = ";
            int fieldDeclarePos = toParse.IndexOf(declareLine);
            Debug.Assert(fieldDeclarePos != -1, "field names must be declared!");
            if (fieldDeclarePos != -1)
            {
                int semiPos = toParse.IndexOf(";", fieldDeclarePos);
                model.Default = toParse.Substring(fieldDeclarePos + declareLine.Length, semiPos - declareLine.Length - fieldDeclarePos).Trim();
            }
            if (model.HasSetter)
            {
                model.UserSetCode = functionInfo.Code.Substring(endOfGet, functionInfo.Code.Length - endOfGet).Trim();
                model.ChangeNotification = model.UserSetCode.Contains("NotifyPropertyChanged");
            }



            return model;

        }





    }
}