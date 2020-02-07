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
        public int StartIndex { get; internal set; }
        public int EndIndex { get; internal set; }
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
        ///    2. all properties have a get, but they don't need a set
        ///    3. the code compiles!
        ///    4. comments above a function/property belong to the function/property
        ///    5. User code is preserved
        ///    6. properties are either public or private
        ///    7. there are no strings that have a "(" in them.
        ///    8. Dependency Property functions look like Templates.DependencProperty
        ///    
        ///    
        /// NOTE:  if you paste  mainpage.xaml.cs into the AllPropertiesAsText TextBox, it will correctly parse out the properties...
        /// 

        public static ParsedFileInfo ParseFile(string toParse)
        {

            ParsedFileInfo parsedInfo = new ParsedFileInfo();
            List<string> nonPropertyCode = new List<string>();

            //
            //  firt get all the lines
            var lines = toParse.Split("\r");
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Trim();
            }

            string nl = "\r";
            PropertyModel model;
            HashSet<string> dependencyPropertySetFunctions = new HashSet<string>();
            FunctionInfo functionInfo;
            bool foundFirstClass = false;
            string currentComment = "";
            for (int currentLineIndex = 0; currentLineIndex < lines.Length; currentLineIndex++)
            {
                string line = lines[currentLineIndex];
                Debug.WriteLine(line);
                if (line == "")
                {
                    nonPropertyCode.Add(nl);
                    continue;
                }
                if (line.Contains("delegate"))
                {
                    continue;
                }
                if (line.Contains("=>"))
                {
                    Debug.WriteLine($"line");
                }


                if (line.StartsWith("//"))
                {
                    if (currentComment != "") nonPropertyCode.Add(currentComment);
                    nonPropertyCode.Add(line);
                    currentComment = "";
                    continue;
                }

                if (line.StartsWith("/*"))
                {
                    StringBuilder sb = new StringBuilder();
                    while (true)
                    {
                        sb.Append(lines[currentLineIndex] + nl);
                        if (lines[currentLineIndex].EndsWith("*/")) break;
                        currentLineIndex++;
                    } // if we go over the index, the code would not compile, violating one constraint

                    currentComment = sb.ToString();
                    continue;
                }

                if (line.StartsWith("public") == false && line.StartsWith("private") == false)
                {
                    if (currentComment != "") nonPropertyCode.Add(currentComment);
                    nonPropertyCode.Add(line);
                    currentComment = "";
                    continue;
                }


                if (line.Contains("get;") && line.Contains("{") && line.Contains("}"))
                {
                    // then it starts with public or private and has a get in it -- assume one liner of the form
                    // public __TYPE__ __PROPERTYNAME__ { get;set; } = __DEFAULT__ where set is optional.
                    model = ParseOneLiner(line);
                    if (model != null)
                    {
                        model.Comment = currentComment;
                        parsedInfo.PropertyList.Add(model);
                    }
                    else
                    {
                        if (currentComment != "") nonPropertyCode.Add(currentComment);
                        nonPropertyCode.Add(line);
                    }
                    currentComment = "";
                    continue;
                }

                if (line.Contains("("))
                {
                    //   we know we have some kind of function declaration 

                    if (line.Contains("DependencyProperty.Register"))
                    {
                        // dependency property declaration                        
                        model = ParseDependencyProperty(toParse, lines, currentLineIndex);
                        if (model != null)
                        {
                            model.Comment = currentComment;
                            parsedInfo.PropertyList.Add(model);
                            dependencyPropertySetFunctions.Add($"{model.PropertyName}"); // assumption!!
                            dependencyPropertySetFunctions.Add($"Set{model.PropertyName}"); // assumption!!
                            dependencyPropertySetFunctions.Add($"{model.PropertyName}Changed"); // assumption!!

                        }
                        else
                        {
                            Debug.Assert(false, $"how did we get here? {line}");
                            if (currentComment != "") nonPropertyCode.Add(currentComment);
                            nonPropertyCode.Add(line);
                        }
                        currentComment = "";
                        continue;

                    }
                    else
                    {
                        //
                        //  user code of some kind
                        functionInfo = GetFunction(lines, currentLineIndex);
                        currentLineIndex = functionInfo.EndIndex;
                        if (dependencyPropertySetFunctions.Contains(functionInfo.Name) == false) // this is a generated function
                        {
                            if (currentComment != "") nonPropertyCode.Add(currentComment);
                            nonPropertyCode.Add(functionInfo.Code);
                        }
                        else
                        {
                            Debug.WriteLine($"threw away {line}");
                        }
                        currentComment = "";
                        continue;

                    }
                }
                else if (line.Contains("class"))
                {
                    if (foundFirstClass)
                    {
                        break; // stop if we find a second class

                    }
                    if (currentComment != "") nonPropertyCode.Add(currentComment);
                    nonPropertyCode.Add(line);
                    foundFirstClass = true;
                    currentComment = "";
                    continue;
                }
                else if (line.Contains(";"))
                {
                    if (line.Contains("=>")) // there is both a ";" and a "=>"...this is a property of the form "public int Test => _test;" -- this is a get only function
                    {
                        model = ParseExpressionBodiedProperty(line);
                        parsedInfo.PropertyList.Add(model);
                        currentComment = "";
                        continue;
                    }
                    // this is a simple declartion like "bool _parsing = true;" or "bool _parsing;" ==> properties cannnot have a ";" at the end of their declations
                    if (currentComment != "") nonPropertyCode.Add(currentComment);
                    nonPropertyCode.Add(line);
                    currentComment = "";
                    continue;
                }
                //
                // if we get here we have a property

                functionInfo = GetFunction(lines, currentLineIndex);
                currentLineIndex = functionInfo.EndIndex;
                // Debug.WriteLine($"{property}");
                if (functionInfo.Code.Contains("GetValue"))
                {
                    // this  is a dependency property that we don't need -- throw it away.
                    currentComment = "";
                    continue;
                }
                Debug.WriteLine($"it is a normal property named {functionInfo.Name} type is: {functionInfo.FunctionType}");
                model = ParseProperty(functionInfo, toParse);
                if (model != null)
                {
                    model.Comment = currentComment;
                    parsedInfo.PropertyList.Add(model);
                    currentComment = "";
                    continue;
                }

                Debug.WriteLine($"have unparsed code! {line}");

            }
            //
            //  remove all the Field declare lines
            //  
            foreach (var prop in parsedInfo.PropertyList)
            {
                if (prop.FieldDeclareLine != "")
                {
                    for (int i = 0; i < nonPropertyCode.Count; i++)
                    {
                        if (nonPropertyCode[i].IndexOf(prop.FieldDeclareLine) != -1)
                        {
                            nonPropertyCode[i] = nonPropertyCode[i].Replace(prop.FieldDeclareLine, "");
                            break;
                        }

                    }
                }
            }
            //
            //  we called .Trim() on each line -- this puts the indents back (4 spaces for a tab), but nothing else
            //
            parsedInfo.NoProperties = Beautify(nonPropertyCode);
            return parsedInfo;
        }

        private static int BeautifyOneLine(StringBuilder sb, string line, int curlyCount)
        {
            int cc = curlyCount;
            if (line.StartsWith("}")) cc--;
            sb.Append(new string(' ', 4 * cc));
            sb.Append(line);
            sb.Append('\r');
            if (line.StartsWith("{")) cc++;
            return cc;
        }

        //
        //  my list can have some single line and some multiline strings...
        private static string Beautify(List<string> lines)
        {
            int curlyCount = 0;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                var tokens = lines[i].Split("\r", StringSplitOptions.RemoveEmptyEntries);
                foreach (var l in tokens)
                {
                    curlyCount = BeautifyOneLine(sb, l, curlyCount);
                }

            }
            return sb.ToString();
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
        //
        //  look for
        //  1. current line starts with //
        //  2. previous lines have a /*
        //
        //  stop if we see a */ - this means we scan to the top of the file.  We could cache the last time we scanned and stop at that point, but doesn't seem worth it.
        //
        //  returns: 
        //              the index for the line that the comment ends
        //              a string with the full comment
        //
        //              endIndex = -1 and comment = "" when it is not inside a comment
        //
        //  ASSUMES:  
        //              1. all lines have already called Trim()
        //              2. we do not look for lines that have /* -------------- */ comments in them (yet)
        //              3. we assume that /* is the first characters in the line 
        //
        private static (int endIndex, string comment) GetComment(string[] lines, int index)
        {
            if (index < 0) throw new Exception("index into array cannot be negative!");
            if (lines[index].StartsWith("//")) return (index, lines[index]);
            string nl = "\r";
            for (int start = index; start >= 0; start--)
            {

                if (lines[start].StartsWith("/*"))
                {
                    // i is the starting line
                    for (int end = index; end < lines.Length; end++)
                    {
                        if (lines[end].Contains("*/"))
                        {
                            string comment = "";

                            for (int k = start; k <= end; k++)
                            {
                                comment += (lines[k] + nl);
                            }
                            return (end, comment);
                        }
                    }

                }
                if (lines[start].Contains("*/"))
                {
                    return (-1, "");
                }

            }

            return (-1, "");
        }

        /// <summary>
        /// Given a line in the form of
        ///  
        ///  public __TYPE__ __PROPERTYNAME__ => __DEFAULT__;
        ///     *or*
        ///  private __TYPE__ __PROPERTYNAME__ => __DEFAULT__;
        ///  
        ///     ASSUMES:
        ///         1. line starts with public or private
        ///         2. the order is as above
        ///         3. spaces are slitters
        ///         4. there are exactly the number of tokens as abovew
        /// </summary>
        private static PropertyModel ParseExpressionBodiedProperty(string line)
        {
            var tokens = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 5)
            {
                return null;
            }

            if (tokens[0] != "public" && tokens[0] != "private")
            {
                return null;
            }
            var model = new PropertyModel
            {
                HasSetter = false,
                PropertyType = tokens[1],
                PropertyName = tokens[2],
                Default = tokens[4]
            };


            return model;
        }

        //
        //  Given a line in the form of: 
        //
        //      public __TYPE__ __PROPERTYNAME__ { get;set; } = __DEFAULT__ where set is optional.
        // or
        //      private __TYPE__ __PROPERTYNAME__ { get;set; } = __DEFAULT__ where set is optional.
        //
        //      parse into a PropertyModel
        //
        //  ASSUMES
        //              1. there is no "=" except for the assignment = (e.g. we would error on "public string Foo {get;set;} = "=";
        //              2. line starts with __TYPE__, public, or private
        //
        //  RETURNS 
        //              PropertyModel
        //
        private static PropertyModel ParseOneLiner(string line)
        {
            var tokens = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 4)
            {
                return null;
            }

            int typeIndex = 1;
            if (tokens[0] != "public" && tokens[0] != "private")
            {
                typeIndex = 0;
            }

            var model = new PropertyModel
            {
                HasSetter = line.Contains("set;"),
                PropertyType = tokens[typeIndex],
                PropertyName = tokens[typeIndex + 1]
            };
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
        //
        //  ASSUMES
        //              1. we are NOT in a comment
        private static PropertyModel ParseDependencyProperty(string allText, string[] lines, int currentIndex)
        {
            //
            //  line looks like "        public static readonly DependencyProperty PropertiesAsTextProperty = DependencyProperty.Register(\"PropertiesAsText\", typeof(string), typeof(MainPage), new PropertyMetadata(\"\"));"
            var model = new PropertyModel
            {
                IsDependencyProperty = true
            };
            string line = lines[currentIndex];
            //
            //  get the text passed into .Register            
            int pos1 = line.IndexOf("(");
            int pos2 = line.LastIndexOf(")");
            string parameters = line.Substring(pos1, pos2 - pos1);

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

            //
            //  if we get here, we know that we have a change notifictation function
            //



            pos1 = pmd.LastIndexOf(",");
            Debug.Assert(pos1 != -1);



            model.ChangeNotification = true;
            model.ChangeNotificationFunction = pmd.Substring(pos1 + 1, pmd.Length - pos1 - 1);

            //
            //  find the 
            var magic = Templates.DependencyBodyNotify.Replace("__PROPERTYNAME__", model.PropertyName).Trim();
            magic = magic.Replace("__TYPE__", model.PropertyType);

            int paren = magic.IndexOf(")");
            magic = magic.Substring(0, paren + 1);

            // look for the line in the lines array
            //  the function could be before index!
            int i;
            for (i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(magic))
                    break;
            }

            if (i == lines.Length) // couldn't find it!
            {
                Debug.Assert(false, $"we shoudl have fund the {magic} line!");
                return model;
            }

            var functionInfo = GetFunction(lines, i);
            model.UserSetCode = functionInfo.Code;




            return model;
        }

        /// <summary>
        ///     given an offset into the file, it will find the function and populate the FunctionInfo object
        ///     this returns the *whole* function
        /// </summary>

        private static FunctionInfo GetFunction(string[] lines, int index)
        {
            int curlyCount = 1;
            var functionInfo = ParseFunctionDeclartion(lines[index]);
            StringBuilder linesInFunction = new StringBuilder();
            int i = index;
            while (true)
            {
                linesInFunction.Append(lines[i]);
                linesInFunction.Append("\r");
                if (lines[i].StartsWith("{")) break;
                i++;
            }
            i++;

            for (; i < lines.Length; i++)
            {
                if (lines[i].Contains("{")) curlyCount++;
                if (lines[i].Contains("}")) curlyCount--;
                if (curlyCount > 0)
                {
                    linesInFunction.Append(lines[i]);
                    linesInFunction.Append("\r");
                }
                else if (curlyCount == 0)
                {
                    linesInFunction.Append(lines[i]);
                    linesInFunction.Append("\r");
                    break;
                }


            }

            functionInfo.Code = linesInFunction.ToString();
            functionInfo.StartIndex = index;
            functionInfo.EndIndex = i;
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

            // 
            //  look for expressions of the form get => __DEFUALT__

            if (model.UserGetCode.Contains("=>"))
            {
                var tokens = model.UserGetCode.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 3)
                {
                    model.FieldName = tokens[2];
                }
            }
            else
            {

                int returnPos = model.UserGetCode.IndexOf("return");
                int eolPos = model.UserGetCode.IndexOf(";", returnPos);
                model.FieldName = model.UserGetCode.Substring(returnPos + "return".Length, eolPos - returnPos - "return".Length).Trim();
            }
            string declareLine = $"{model.PropertyType} {model.FieldName} = ";
            int fieldDeclarePos = toParse.IndexOf(declareLine);

            if (fieldDeclarePos != -1) // this might be -1 if the field is declared in a different file...
            {
                int semiPos = toParse.IndexOf(";", fieldDeclarePos);
                int lineStart = fieldDeclarePos;
                while (toParse[lineStart] != '\r')
                {
                    lineStart--;
                }
                lineStart++;
                model.Default = toParse.Substring(fieldDeclarePos + declareLine.Length, semiPos - declareLine.Length - fieldDeclarePos).Trim();
                model.FieldDeclareLine = toParse.Substring(lineStart, semiPos - lineStart + 1).Trim();
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